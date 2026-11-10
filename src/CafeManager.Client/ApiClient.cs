using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CafeManager.Core;

namespace CafeManager.Client;

public readonly record struct Empty;

public sealed record ApiClientOptions(Uri BaseAddress, TimeSpan Timeout)
{
    public const string BaseAddressVariable = "CAFE_API_BASE_URL";
    public static ApiClientOptions FromEnvironment() => new(
        new Uri(Environment.GetEnvironmentVariable(BaseAddressVariable) ?? "https://localhost:7034"), TimeSpan.FromSeconds(15));
}

// Kết quả một lời gọi API. Unknown = true khi chưa biết máy chủ đã xử lý hay chưa (mất mạng, hết thời gian, lỗi 5xx):
// giao diện không được báo thành công hay thất bại chắc chắn và phải cho tra cứu lại trước khi gửi lại thao tác tiền.
public sealed record ApiResult<T>(bool Ok, int StatusCode, T? Value, ApiError? Error, bool Unknown)
{
    public string Message => Error?.Message ?? (Ok ? "" : "Lỗi không xác định.");
}

// Máy khách HTTP cho Desktop. Mỗi thể hiện có CookieContainer riêng (một nhân viên một phiên, không dùng chung).
// Desktop chỉ gọi API qua lớp này; không truy cập cơ sở dữ liệu.
public sealed class ApiClient : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly TimeSpan _timeout;

    public ApiClient(ApiClientOptions options, HttpMessageHandler? handler = null)
    {
        if (handler == null)
        {
            Cookies = new System.Net.CookieContainer();
            handler = new HttpClientHandler { CookieContainer = Cookies, UseCookies = true, AllowAutoRedirect = false };
        }
        _http = new HttpClient(handler, disposeHandler: true) { BaseAddress = options.BaseAddress, Timeout = Timeout.InfiniteTimeSpan };
        _timeout = options.Timeout;
    }

    // Null khi dùng handler tùy chọn (kiểm thử).
    public System.Net.CookieContainer? Cookies { get; }
    public LoginResponse? CurrentUser { get; private set; }
    // Tên header và mã chống giả mạo yêu cầu (antiforgery); máy chủ sẽ chốt cách cấp mã (xem hợp đồng API).
    public string AntiforgeryHeaderName { get; set; } = "X-CSRF-TOKEN";
    public string? AntiforgeryToken { get; set; }
    // Xảy ra khi một lời gọi (không phải đăng nhập) nhận 401: phiên đã hết hạn hoặc bị thu hồi.
    public event Action? SessionExpired;

    public async Task<ApiResult<LoginResponse>> LoginAsync(string userName, string password, CancellationToken ct = default)
    {
        var result = await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", new LoginRequest(userName, password), raiseExpired: false, ct);
        if (!result.Ok) { CurrentUser = null; return result; }
        if (result.Value is not { UserKind: UserKinds.Staff })
        {
            await LogoutAsync(ct);
            return new(false, 403, null, new ApiError(ApiErrorCodes.Forbidden, "Tài khoản này không phải tài khoản nhân viên."), false);
        }
        CurrentUser = result.Value;
        return result;
    }

    public async Task<ApiResult<Empty>> LogoutAsync(CancellationToken ct = default)
    {
        var result = await SendAsync<Empty>(HttpMethod.Post, "api/auth/logout", null, raiseExpired: false, ct);
        CurrentUser = null;
        return result;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, raiseExpired: true, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, raiseExpired: true, ct);

    public Task<ApiResult<Empty>> PostAsync(string path, object? body, CancellationToken ct = default) =>
        SendAsync<Empty>(HttpMethod.Post, path, body, raiseExpired: true, ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Put, path, body, raiseExpired: true, ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, bool raiseExpired, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_timeout);
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body != null) request.Content = JsonContent.Create(body, options: Json);
            if (AntiforgeryToken != null) request.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, AntiforgeryToken);
            request.Headers.Accept.Add(new("application/json"));
            using var response = await _http.SendAsync(request, timeout.Token);
            return await ReadAsync<T>(response, raiseExpired, timeout.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException)
        {
            return Unknown<T>("Hết thời gian chờ máy chủ. Chưa xác định thao tác đã được xử lý hay chưa; hãy kiểm tra lại trước khi gửi lại.");
        }
        catch (HttpRequestException)
        {
            return Unknown<T>("Chưa kết nối được máy chủ. Chưa xác định thao tác đã được xử lý hay chưa; hãy kiểm tra mạng và tra cứu lại.");
        }
    }

    private async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, bool raiseExpired, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        if (response.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(Empty) || response.StatusCode == HttpStatusCode.NoContent)
                return new(true, status, default, null, false);
            try { return new(true, status, await response.Content.ReadFromJsonAsync<T>(Json, ct), null, false); }
            catch (JsonException) { return Unknown<T>("Máy chủ trả dữ liệu không đọc được. Chưa xác định kết quả thao tác."); }
        }
        if (status >= 500) return Unknown<T>("Máy chủ gặp lỗi (mã " + status + "). Chưa xác định thao tác đã được xử lý hay chưa.", status);
        if (status == 401 && raiseExpired) SessionExpired?.Invoke();
        return new(false, status, default, await ReadErrorAsync(response, status, ct), false);
    }

    private static async Task<ApiError> ReadErrorAsync(HttpResponseMessage response, int status, CancellationToken ct)
    {
        // Máy chủ phải trả JSON cho Desktop; nếu nhận HTML (trang đăng nhập) thì không phân tích, dùng mã trạng thái.
        if (response.Content.Headers.ContentType?.MediaType is "application/json" or "application/problem+json")
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(Json, ct);
                if (error != null && !string.IsNullOrEmpty(error.Code)) return error;
            }
            catch (JsonException) { }
        }
        return status switch
        {
            400 => new(ApiErrorCodes.Validation, "Dữ liệu gửi lên không hợp lệ."),
            401 => new(ApiErrorCodes.Unauthorized, "Chưa đăng nhập hoặc phiên đã hết hạn."),
            403 => new(ApiErrorCodes.Forbidden, "Không đủ quyền thực hiện thao tác này."),
            404 => new(ApiErrorCodes.NotFound, "Không tìm thấy hoặc không thuộc phạm vi được truy cập."),
            409 => new(ApiErrorCodes.Conflict, "Dữ liệu vừa thay đổi; hãy tải lại rồi thử lại."),
            _ => new("HTTP_" + status, "Máy chủ trả mã " + status + "."),
        };
    }

    private static ApiResult<T> Unknown<T>(string message, int status = 0) =>
        new(false, status, default, new ApiError("UNKNOWN", message), true);

    public void Dispose() => _http.Dispose();
}
