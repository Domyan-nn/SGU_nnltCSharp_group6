using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HistoryProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(HistorySql.TR_RefundRequest_History);
            migrationBuilder.Sql(HistorySql.TR_CancellationRequest_History);
            migrationBuilder.Sql(HistorySql.TR_CancellationItem_Order);
            migrationBuilder.Sql(HistorySql.TR_AuditEvent_AppendOnly);
            migrationBuilder.Sql(HistorySql.TR_StockMovement_AppendOnly);
            migrationBuilder.AddCheckConstraint(
                name: "CK_StockCount_Approval",
                table: "StockCount",
                sql: "([Status] = N'Pending' AND [ApprovedBy] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_RefundRequest_History");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_CancellationRequest_History");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_CancellationItem_Order");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_AuditEvent_AppendOnly");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_StockMovement_AppendOnly");
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockCount_Approval",
                table: "StockCount");
        }
    }
}
