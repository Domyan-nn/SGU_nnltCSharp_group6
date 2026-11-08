using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntegrityGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(IntegritySql.TR_PriceHistory_NoOverlap);
            migrationBuilder.Sql(IntegritySql.TR_RecipeVersion_NoOverlap);
            migrationBuilder.Sql(IntegritySql.TR_PreparationUnit_Round);
            migrationBuilder.Sql(IntegritySql.TR_PreparationTicket_Round);
            migrationBuilder.Sql(IntegritySql.TR_OrderLine_Units);
            migrationBuilder.Sql(IntegritySql.TR_Payment_History);
            migrationBuilder.Sql(IntegritySql.TR_RefundTransaction_Limit);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PriceHistory_NoOverlap");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_RecipeVersion_NoOverlap");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PreparationUnit_Round");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PreparationTicket_Round");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_OrderLine_Units");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_Payment_History");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_RefundTransaction_Limit");
        }
    }
}
