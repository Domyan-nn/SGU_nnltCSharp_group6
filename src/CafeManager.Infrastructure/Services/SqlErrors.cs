using Microsoft.Data.SqlClient;

namespace CafeManager.Infrastructure.Services;

// Nhận biết lỗi tranh chấp tạm thời của SQL Server (bế tắc, hết hạn khóa) kể cả khi EF bọc ngoài InvalidOperationException/DbUpdateException,
// để trả "hệ thống đang bận, thử lại" (409) thay vì báo nhầm là sai trạng thái.
internal static class SqlErrors
{
    internal static bool IsContention(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
            if (e is SqlException sql && sql.Errors.Cast<SqlError>().Any(x => x.Number is 1205 or 51000)) return true;
        return false;
    }
}
