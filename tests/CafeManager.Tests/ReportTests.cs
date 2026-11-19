using System.Net;
using System.Text;
using System.Text.Json;
using CafeManager.Client;
using CafeManager.Core;
using CafeManager.Infrastructure.Services;

namespace CafeManager.Tests;

// Kiểm thử báo cáo doanh thu (B4) không cần SQL Server: phần tính, thang biểu đồ, CSV và lời gọi API của Desktop.
public sealed class ReportTests
{
    private static readonly DateOnly D18 = new(2026, 11, 18);
    private static readonly DateOnly D19 = new(2026, 11, 19);
    private static readonly DateOnly D20 = new(2026, 11, 20);

    [Fact]
    public void Aggregator_groups_by_vietnam_day_subtracts_refunds_and_fills_empty_days()
    {
        var payments = new List<PaymentRow>
        {
            new(1, 50_000m, new DateTime(2026, 11, 18, 16, 59, 0, DateTimeKind.Utc)), // 23:59 ngày 18 (UTC+7)
            new(2, 70_000m, new DateTime(2026, 11, 18, 17, 1, 0, DateTimeKind.Utc)),  // 00:01 ngày 19
            new(2, 30_000m, new DateTime(2026, 11, 19, 3, 0, 0, DateTimeKind.Utc)),   // cùng đơn 2, vẫn ngày 19
        };
        var refunds = new List<RefundRow> { new(20_000m, new DateTime(2026, 11, 19, 5, 0, 0, DateTimeKind.Utc)) };

        var report = RevenueAggregator.Build(D18, D20, payments, refunds, [], 10);

        Assert.Equal(3, report.Days.Count);
        Assert.Equal(new DailyRevenueDto(D18, 50_000m, 1), report.Days[0]);
        Assert.Equal(new DailyRevenueDto(D19, 80_000m, 1), report.Days[1]); // 70.000 + 30.000 - 20.000; một đơn
        Assert.Equal(new DailyRevenueDto(D20, 0m, 0), report.Days[2]);
        Assert.Equal(150_000m, report.Paid);
        Assert.Equal(20_000m, report.Refunded);
        Assert.Equal(130_000m, report.Net);
        Assert.Equal(2, report.PaidOrders);
        Assert.Equal(report.Net, report.Days.Sum(d => d.Revenue));
    }

    [Fact]
    public void Aggregator_ranks_top_products_by_quantity_then_revenue_and_includes_addons()
    {
        var lines = new List<SoldLineRow>
        {
            new("Cà phê sữa", 2, 30_000m, 5_000m),   // 70.000
            new("Cà phê sữa", 1, 30_000m, 0m),       // 30.000 -> tổng 3 ly, 100.000
            new("Trà đào", 3, 35_000m, 0m),          // 3 ly, 105.000 -> hơn Cà phê sữa về doanh thu
            new("Bạc xỉu", 1, 28_000m, 0m),
        };

        var report = RevenueAggregator.Build(D19, D19, [], [], lines, 2);

        Assert.Equal(2, report.TopProducts.Count);
        Assert.Equal(new TopProductDto("Trà đào", 3, 105_000m), report.TopProducts[0]);
        Assert.Equal(new TopProductDto("Cà phê sữa", 3, 100_000m), report.TopProducts[1]);
    }

    [Fact]
    public void Chart_scale_rounds_up_to_one_two_five_steps()
    {
        Assert.Equal(ChartScale.EmptyMax, ChartScale.NiceMax(0m));
        Assert.Equal(100_000m, ChartScale.NiceMax(87_000m));
        Assert.Equal(2_000_000m, ChartScale.NiceMax(1_234_567m));
        Assert.Equal(5_000_000m, ChartScale.NiceMax(5_000_000m));
        Assert.Equal(10_000_000m, ChartScale.NiceMax(5_000_001m));
    }

    [Fact]
    public void Csv_has_excel_separator_hint_escapes_cells_and_blocks_formulas()
    {
        var report = new RevenueReportDto(D19, D19, 100_000m, 0m, 100_000m, 1,
            [new DailyRevenueDto(D19, 100_000m, 1)],
            [new TopProductDto("=HYPERLINK(\"x\")", 2, 60_000m), new TopProductDto("Trà; \"đào\"", 1, 40_000m)]);

        var csv = ReportCsv.Build(report);

        Assert.StartsWith("sep=;\r\n", csv);
        Assert.Contains("2026-11-19;100000;1\r\n", csv);
        Assert.Contains("1;\"'=HYPERLINK(\"\"x\"\")\";2;60000\r\n", csv);
        Assert.Contains("2;\"Trà; \"\"đào\"\"\";1;40000\r\n", csv);
    }

    private sealed class RecordingHandler(object body) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task Desktop_client_calls_the_revenue_endpoint_with_iso_dates_and_reads_the_report()
    {
        var expected = new RevenueReportDto(D19, D20, 10m, 0m, 10m, 1, [new DailyRevenueDto(D19, 10m, 1)], []);
        var handler = new RecordingHandler(expected);
        using var api = new ApiClient(new ApiClientOptions(new Uri("https://api.test/"), TimeSpan.FromSeconds(5)), handler);

        var result = await new StaffApi(api).RevenueReportAsync(D19, D20, 5);

        Assert.True(result.Ok);
        Assert.Equal("/api/staff/reports/revenue", handler.LastUri!.AbsolutePath);
        Assert.Equal("?from=2026-11-19&to=2026-11-20&top=5", handler.LastUri.Query);
        Assert.Equal(D19, result.Value!.From);
        Assert.Equal(10m, result.Value.Net);
    }
}
