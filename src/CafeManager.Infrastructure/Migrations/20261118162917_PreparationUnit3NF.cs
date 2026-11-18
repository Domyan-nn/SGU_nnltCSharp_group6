using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManager.Infrastructure.Migrations
{
    /// <summary>
    /// Chuẩn hóa PreparationUnit về 3NF/BCNF: bỏ TicketId và RecipeVersionId (cùng phụ thuộc vào LineId, một phần của
    /// khóa ứng viên (LineId, UnitNumber)), chuyển RecipeVersionId lên OrderLine; phiếu của ly suy ra qua
    /// OrderLine.RoundId = PreparationTicket.RoundId. Thứ tự Up: bỏ trigger cũ → kiểm tra dữ liệu → thêm cột mới
    /// → chép dữ liệu → bỏ cột cũ → tạo trigger mới, để không mất công thức của các ly đã có. Down làm ngược lại.
    /// Làm mới từ main ngày 18/11/2026, thay cho nhánh thử PR #9.
    /// </summary>
    public partial class PreparationUnit3NF : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Bỏ trigger cũ (đang đọc hai cột sắp xóa) và kiểm tra dữ liệu cũ chuyển được.
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PreparationUnit_Round");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PreparationTicket_Round");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_OrderLine_Units");
            migrationBuilder.Sql(PreparationUnit3NFSql.GuardUp);

            // 2. Thêm OrderLine.RecipeVersionId (NULL khi dòng còn nháp) rồi chép từ các ly hiện có.
            migrationBuilder.AddColumn<long>(
                name: "RecipeVersionId",
                table: "OrderLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLine_RecipeVersionId",
                table: "OrderLine",
                column: "RecipeVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLine_RecipeVersion_RecipeVersionId",
                table: "OrderLine",
                column: "RecipeVersionId",
                principalTable: "RecipeVersion",
                principalColumn: "RecipeVersionId");

            migrationBuilder.Sql(PreparationUnit3NFSql.CopyRecipeToLine);

            // 3. Bỏ hai cột phụ thuộc riêng phần khỏi PreparationUnit.
            migrationBuilder.DropForeignKey(
                name: "FK_PreparationUnit_PreparationTicket_TicketId",
                table: "PreparationUnit");

            migrationBuilder.DropForeignKey(
                name: "FK_PreparationUnit_RecipeVersion_RecipeVersionId",
                table: "PreparationUnit");

            migrationBuilder.DropIndex(
                name: "IX_PreparationUnit_RecipeVersionId",
                table: "PreparationUnit");

            migrationBuilder.DropIndex(
                name: "IX_PreparationUnit_TicketId",
                table: "PreparationUnit");

            migrationBuilder.DropColumn(
                name: "RecipeVersionId",
                table: "PreparationUnit");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "PreparationUnit");

            // 4. Tạo trigger cho mô hình mới.
            migrationBuilder.Sql(PreparationUnit3NFSql.TR_PreparationUnit_Round);
            migrationBuilder.Sql(PreparationUnit3NFSql.TR_PreparationTicket_Round);
            migrationBuilder.Sql(PreparationUnit3NFSql.TR_OrderLine_Units);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Bỏ trigger mới, kiểm tra mọi ly tìm lại được phiếu và công thức.
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PreparationUnit_Round");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_PreparationTicket_Round");
            migrationBuilder.Sql("DROP TRIGGER dbo.TR_OrderLine_Units");
            migrationBuilder.Sql(PreparationUnit3NFSql.GuardDown);

            // 2. Thêm lại hai cột (cho phép NULL để điền), điền từ dòng và phiếu, rồi đặt NOT NULL.
            migrationBuilder.AddColumn<long>(
                name: "RecipeVersionId",
                table: "PreparationUnit",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TicketId",
                table: "PreparationUnit",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(PreparationUnit3NFSql.RestoreTicketAndRecipe);

            migrationBuilder.AlterColumn<long>(
                name: "RecipeVersionId",
                table: "PreparationUnit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TicketId",
                table: "PreparationUnit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationUnit_RecipeVersionId",
                table: "PreparationUnit",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PreparationUnit_TicketId",
                table: "PreparationUnit",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreparationUnit_PreparationTicket_TicketId",
                table: "PreparationUnit",
                column: "TicketId",
                principalTable: "PreparationTicket",
                principalColumn: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreparationUnit_RecipeVersion_RecipeVersionId",
                table: "PreparationUnit",
                column: "RecipeVersionId",
                principalTable: "RecipeVersion",
                principalColumn: "RecipeVersionId");

            // 3. Bỏ OrderLine.RecipeVersionId.
            migrationBuilder.DropForeignKey(
                name: "FK_OrderLine_RecipeVersion_RecipeVersionId",
                table: "OrderLine");

            migrationBuilder.DropIndex(
                name: "IX_OrderLine_RecipeVersionId",
                table: "OrderLine");

            migrationBuilder.DropColumn(
                name: "RecipeVersionId",
                table: "OrderLine");

            // 4. Khôi phục trigger gốc của IntegrityGuards.
            migrationBuilder.Sql(IntegritySql.TR_PreparationUnit_Round);
            migrationBuilder.Sql(IntegritySql.TR_PreparationTicket_Round);
            migrationBuilder.Sql(IntegritySql.TR_OrderLine_Units);
        }
    }
}
