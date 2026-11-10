using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SchemaGapColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "SalesOrder",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "SalesOrder",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupCode",
                table: "SalesOrder",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "SalesOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "SalesOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RejectedBy",
                table: "SalesOrder",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "PreparationUnit",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeliveredBy",
                table: "PreparationUnit",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "Payment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AllowedAddon",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    AddonVariantId = table.Column<long>(type: "bigint", nullable: false),
                    MaxPerUnit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowedAddon", x => new { x.ProductId, x.AddonVariantId });
                    table.CheckConstraint("CK_AllowedAddon_1", "[MaxPerUnit] > 0");
                    table.ForeignKey(
                        name: "FK_AllowedAddon_ProductVariant_AddonVariantId",
                        column: x => x.AddonVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                    table.ForeignKey(
                        name: "FK_AllowedAddon_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_RejectedBy",
                table: "SalesOrder",
                column: "RejectedBy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SalesOrder_3",
                table: "SalesOrder",
                sql: "([RejectedBy] IS NULL AND [RejectedAt] IS NULL AND [RejectReason] IS NULL) OR ([RejectedBy] IS NOT NULL AND [RejectedAt] IS NOT NULL AND [RejectReason] IS NOT NULL AND [Status] = N'Rejected')");

            migrationBuilder.CreateIndex(
                name: "IX_PreparationUnit_DeliveredBy",
                table: "PreparationUnit",
                column: "DeliveredBy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PreparationUnit_3",
                table: "PreparationUnit",
                sql: "([DeliveredBy] IS NULL AND [DeliveredAt] IS NULL) OR ([DeliveredBy] IS NOT NULL AND [DeliveredAt] IS NOT NULL AND [Status] = N'Delivered')");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ExternalReference",
                table: "Payment",
                column: "ExternalReference",
                unique: true,
                filter: "[ExternalReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AllowedAddon_AddonVariantId",
                table: "AllowedAddon",
                column: "AddonVariantId");

            migrationBuilder.Sql(AllowedAddonSql.TR_AllowedAddon_Type);

            migrationBuilder.AddForeignKey(
                name: "FK_PreparationUnit_StaffAccount_DeliveredBy",
                table: "PreparationUnit",
                column: "DeliveredBy",
                principalTable: "StaffAccount",
                principalColumn: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrder_StaffAccount_RejectedBy",
                table: "SalesOrder",
                column: "RejectedBy",
                principalTable: "StaffAccount",
                principalColumn: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreparationUnit_StaffAccount_DeliveredBy",
                table: "PreparationUnit");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrder_StaffAccount_RejectedBy",
                table: "SalesOrder");

            migrationBuilder.Sql("DROP TRIGGER dbo.TR_AllowedAddon_Type");

            migrationBuilder.DropTable(
                name: "AllowedAddon");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrder_RejectedBy",
                table: "SalesOrder");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SalesOrder_3",
                table: "SalesOrder");

            migrationBuilder.DropIndex(
                name: "IX_PreparationUnit_DeliveredBy",
                table: "PreparationUnit");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PreparationUnit_3",
                table: "PreparationUnit");

            migrationBuilder.DropIndex(
                name: "IX_Payment_ExternalReference",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "PickupCode",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "SalesOrder");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "PreparationUnit");

            migrationBuilder.DropColumn(
                name: "DeliveredBy",
                table: "PreparationUnit");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "Payment");
        }
    }
}
