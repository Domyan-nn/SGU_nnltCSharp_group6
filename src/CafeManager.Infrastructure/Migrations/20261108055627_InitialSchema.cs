using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeTable",
                columns: table => new
                {
                    TableId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeTable", x => x.TableId);
                    table.CheckConstraint("CK_CafeTable_1", "[Status] IN (N'Ready',N'Serving',N'NeedsCleaning')");
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    CategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Counter",
                columns: table => new
                {
                    CounterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CounterCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counter", x => x.CounterId);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResultReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.RequestId);
                });

            migrationBuilder.CreateTable(
                name: "Ingredient",
                columns: table => new
                {
                    IngredientId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngredientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredient", x => x.IngredientId);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerProfile",
                columns: table => new
                {
                    CustomerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProfile", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerProfile_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffAccount",
                columns: table => new
                {
                    StaffId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAccount", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_StaffAccount_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceSession",
                columns: table => new
                {
                    SessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrentTableId = table.Column<long>(type: "bigint", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSession", x => x.SessionId);
                    table.CheckConstraint("CK_ServiceSession_1", "[ClosedAt] IS NULL OR [ClosedAt] >= [OpenedAt]");
                    table.ForeignKey(
                        name: "FK_ServiceSession_CafeTable_CurrentTableId",
                        column: x => x.CurrentTableId,
                        principalTable: "CafeTable",
                        principalColumn: "TableId");
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Product_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "IngredientBatch",
                columns: table => new
                {
                    BatchId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngredientId = table.Column<long>(type: "bigint", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientBatch", x => x.BatchId);
                    table.CheckConstraint("CK_IngredientBatch_1", "[QuantityReceived] > 0");
                    table.ForeignKey(
                        name: "FK_IngredientBatch_Ingredient_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredient",
                        principalColumn: "IngredientId");
                });

            migrationBuilder.CreateTable(
                name: "StockBalance",
                columns: table => new
                {
                    IngredientId = table.Column<long>(type: "bigint", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalance", x => x.IngredientId);
                    table.CheckConstraint("CK_StockBalance_1", "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]");
                    table.ForeignKey(
                        name: "FK_StockBalance_Ingredient_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredient",
                        principalColumn: "IngredientId");
                });

            migrationBuilder.CreateTable(
                name: "StockMovement",
                columns: table => new
                {
                    MovementId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovementCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IngredientId = table.Column<long>(type: "bigint", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovement", x => x.MovementId);
                    table.CheckConstraint("CK_StockMovement_1", "[Quantity] > 0");
                    table.CheckConstraint("CK_StockMovement_2", "[MovementType] IN (N'Receipt',N'Consume',N'Waste',N'AdjustmentIn',N'AdjustmentOut',N'Reserve',N'Release')");
                    table.ForeignKey(
                        name: "FK_StockMovement_Ingredient_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredient",
                        principalColumn: "IngredientId");
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "PermissionId");
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvent",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffId = table.Column<long>(type: "bigint", nullable: false),
                    ActionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_AuditEvent_StaffAccount_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "Shift",
                columns: table => new
                {
                    ShiftId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CounterId = table.Column<long>(type: "bigint", nullable: false),
                    OpenedBy = table.Column<long>(type: "bigint", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CountedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ClosedBy = table.Column<long>(type: "bigint", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloseNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift", x => x.ShiftId);
                    table.CheckConstraint("CK_Shift_1", "[OpeningCash] >= 0 AND ([CountedCash] IS NULL OR [CountedCash] >= 0)");
                    table.CheckConstraint("CK_Shift_2", "([ClosedAt] IS NULL AND [ClosedBy] IS NULL AND [CountedCash] IS NULL) OR ([ClosedAt] IS NOT NULL AND [ClosedBy] IS NOT NULL AND [CountedCash] IS NOT NULL AND [ClosedAt] >= [OpenedAt])");
                    table.ForeignKey(
                        name: "FK_Shift_Counter_CounterId",
                        column: x => x.CounterId,
                        principalTable: "Counter",
                        principalColumn: "CounterId");
                    table.ForeignKey(
                        name: "FK_Shift_StaffAccount_ClosedBy",
                        column: x => x.ClosedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                    table.ForeignKey(
                        name: "FK_Shift_StaffAccount_OpenedBy",
                        column: x => x.OpenedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "StaffRole",
                columns: table => new
                {
                    StaffId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffRole", x => new { x.StaffId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_StaffRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId");
                    table.ForeignKey(
                        name: "FK_StaffRole_StaffAccount_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "StockCount",
                columns: table => new
                {
                    CountId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IngredientId = table.Column<long>(type: "bigint", nullable: false),
                    BookQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CountedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CountedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CountedBy = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCount", x => x.CountId);
                    table.CheckConstraint("CK_StockCount_1", "[BookQty] >= 0 AND [CountedQty] >= 0");
                    table.CheckConstraint("CK_StockCount_2", "[Status] IN (N'Pending',N'Approved',N'Rejected')");
                    table.CheckConstraint("CK_StockCount_3", "[ApprovedBy] IS NULL OR [ApprovedBy] <> [CountedBy]");
                    table.ForeignKey(
                        name: "FK_StockCount_Ingredient_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredient",
                        principalColumn: "IngredientId");
                    table.ForeignKey(
                        name: "FK_StockCount_StaffAccount_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                    table.ForeignKey(
                        name: "FK_StockCount_StaffAccount_CountedBy",
                        column: x => x.CountedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrder",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    SessionId = table.Column<long>(type: "bigint", nullable: true),
                    OrderType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedPickupAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrder", x => x.OrderId);
                    table.CheckConstraint("CK_SalesOrder_1", "([OrderType] = N'Web' AND [CustomerId] IS NOT NULL AND [SessionId] IS NULL) OR ([OrderType] = N'Table' AND [SessionId] IS NOT NULL)");
                    table.CheckConstraint("CK_SalesOrder_2", "[Status] IN (N'Pending',N'Serving',N'Settling',N'Closed',N'Cancelled',N'Rejected')");
                    table.ForeignKey(
                        name: "FK_SalesOrder_CustomerProfile_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerProfile",
                        principalColumn: "CustomerId");
                    table.ForeignKey(
                        name: "FK_SalesOrder_ServiceSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ServiceSession",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateTable(
                name: "TableTransferHistory",
                columns: table => new
                {
                    TransferId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    FromTableId = table.Column<long>(type: "bigint", nullable: false),
                    ToTableId = table.Column<long>(type: "bigint", nullable: false),
                    TransferAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableTransferHistory", x => x.TransferId);
                    table.CheckConstraint("CK_TableTransferHistory_1", "[FromTableId] <> [ToTableId]");
                    table.ForeignKey(
                        name: "FK_TableTransferHistory_CafeTable_FromTableId",
                        column: x => x.FromTableId,
                        principalTable: "CafeTable",
                        principalColumn: "TableId");
                    table.ForeignKey(
                        name: "FK_TableTransferHistory_CafeTable_ToTableId",
                        column: x => x.ToTableId,
                        principalTable: "CafeTable",
                        principalColumn: "TableId");
                    table.ForeignKey(
                        name: "FK_TableTransferHistory_ServiceSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ServiceSession",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateTable(
                name: "ProductVariant",
                columns: table => new
                {
                    VariantId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VariantType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariant", x => x.VariantId);
                    table.CheckConstraint("CK_ProductVariant_1", "[VariantType] IN (N'Drink',N'Addon')");
                    table.ForeignKey(
                        name: "FK_ProductVariant_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateTable(
                name: "ShiftCashEntry",
                columns: table => new
                {
                    CashEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashEntryCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ShiftId = table.Column<long>(type: "bigint", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftCashEntry", x => x.CashEntryId);
                    table.CheckConstraint("CK_ShiftCashEntry_1", "[Amount] > 0");
                    table.CheckConstraint("CK_ShiftCashEntry_2", "[EntryType] IN (N'OtherIn',N'OtherOut')");
                    table.CheckConstraint("CK_ShiftCashEntry_3", "[Status] IN (N'Posted',N'Voided')");
                    table.ForeignKey(
                        name: "FK_ShiftCashEntry_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shift",
                        principalColumn: "ShiftId");
                    table.ForeignKey(
                        name: "FK_ShiftCashEntry_StaffAccount_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "CancellationRequest",
                columns: table => new
                {
                    CancellationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedBy = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationRequest", x => x.CancellationId);
                    table.CheckConstraint("CK_CancellationRequest_1", "[Status] IN (N'Pending',N'Approved',N'Rejected')");
                    table.CheckConstraint("CK_CancellationRequest_2", "([Status] = N'Pending' AND [ApprovedBy] IS NULL AND [ApprovedAt] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL AND [ApprovedBy] <> [RequestedBy])");
                    table.ForeignKey(
                        name: "FK_CancellationRequest_SalesOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrder",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_CancellationRequest_StaffAccount_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                    table.ForeignKey(
                        name: "FK_CancellationRequest_StaffAccount_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "OrderRound",
                columns: table => new
                {
                    RoundId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRound", x => x.RoundId);
                    table.CheckConstraint("CK_OrderRound_1", "[Status] IN (N'Draft',N'Accepted',N'Cancelled')");
                    table.CheckConstraint("CK_OrderRound_2", "([Status] = N'Draft' AND [AcceptedAt] IS NULL) OR ([Status] = N'Accepted' AND [AcceptedAt] IS NOT NULL) OR [Status] = N'Cancelled'");
                    table.CheckConstraint("CK_OrderRound_3", "[RoundNumber] > 0");
                    table.ForeignKey(
                        name: "FK_OrderRound_SalesOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrder",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ShiftId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentId);
                    table.CheckConstraint("CK_Payment_1", "[Amount] > 0");
                    table.CheckConstraint("CK_Payment_2", "[Method] IN (N'Cash',N'BankTransfer')");
                    table.CheckConstraint("CK_Payment_3", "[Status] IN (N'Pending',N'Succeeded',N'Failed')");
                    table.CheckConstraint("CK_Payment_4", "[Status] <> N'Succeeded' OR [PaidAt] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Payment_IdempotencyRecord_RequestId",
                        column: x => x.RequestId,
                        principalTable: "IdempotencyRecord",
                        principalColumn: "RequestId");
                    table.ForeignKey(
                        name: "FK_Payment_SalesOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrder",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_Payment_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shift",
                        principalColumn: "ShiftId");
                    table.ForeignKey(
                        name: "FK_Payment_StaffAccount_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "SalesAdjustment",
                columns: table => new
                {
                    AdjustmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    AmountSigned = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesAdjustment", x => x.AdjustmentId);
                    table.ForeignKey(
                        name: "FK_SalesAdjustment_SalesOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesOrder",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_SalesAdjustment_StaffAccount_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    PriceHistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<long>(type: "bigint", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.PriceHistoryId);
                    table.CheckConstraint("CK_PriceHistory_1", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                    table.CheckConstraint("CK_PriceHistory_2", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_PriceHistory_ProductVariant_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                });

            migrationBuilder.CreateTable(
                name: "RecipeVersion",
                columns: table => new
                {
                    RecipeVersionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeVersion", x => x.RecipeVersionId);
                    table.CheckConstraint("CK_RecipeVersion_1", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_RecipeVersion_ProductVariant_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                });

            migrationBuilder.CreateTable(
                name: "OrderLine",
                columns: table => new
                {
                    LineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundId = table.Column<long>(type: "bigint", nullable: false),
                    VariantId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VariantNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLine", x => x.LineId);
                    table.CheckConstraint("CK_OrderLine_1", "[Quantity] > 0");
                    table.CheckConstraint("CK_OrderLine_2", "[UnitPriceSnapshot] >= 0");
                    table.ForeignKey(
                        name: "FK_OrderLine_OrderRound_RoundId",
                        column: x => x.RoundId,
                        principalTable: "OrderRound",
                        principalColumn: "RoundId");
                    table.ForeignKey(
                        name: "FK_OrderLine_ProductVariant_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                });

            migrationBuilder.CreateTable(
                name: "PreparationTicket",
                columns: table => new
                {
                    TicketId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationTicket", x => x.TicketId);
                    table.CheckConstraint("CK_PreparationTicket_1", "[Status] IN (N'Waiting',N'Preparing',N'Ready',N'Completed',N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_PreparationTicket_OrderRound_RoundId",
                        column: x => x.RoundId,
                        principalTable: "OrderRound",
                        principalColumn: "RoundId");
                });

            migrationBuilder.CreateTable(
                name: "RefundRequest",
                columns: table => new
                {
                    RefundId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    AdjustmentId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedBy = table.Column<long>(type: "bigint", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequest", x => x.RefundId);
                    table.CheckConstraint("CK_RefundRequest_1", "[Amount] > 0");
                    table.CheckConstraint("CK_RefundRequest_2", "[Status] IN (N'Pending',N'Approved',N'Rejected')");
                    table.CheckConstraint("CK_RefundRequest_3", "([Status] = N'Pending' AND [ApprovedBy] IS NULL AND [ApprovedAt] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL AND [ApprovedBy] <> [RequestedBy])");
                    table.ForeignKey(
                        name: "FK_RefundRequest_Payment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payment",
                        principalColumn: "PaymentId");
                    table.ForeignKey(
                        name: "FK_RefundRequest_SalesAdjustment_AdjustmentId",
                        column: x => x.AdjustmentId,
                        principalTable: "SalesAdjustment",
                        principalColumn: "AdjustmentId");
                    table.ForeignKey(
                        name: "FK_RefundRequest_StaffAccount_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                    table.ForeignKey(
                        name: "FK_RefundRequest_StaffAccount_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "RecipeDetail",
                columns: table => new
                {
                    RecipeVersionId = table.Column<long>(type: "bigint", nullable: false),
                    IngredientId = table.Column<long>(type: "bigint", nullable: false),
                    QuantityRequired = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeDetail", x => new { x.RecipeVersionId, x.IngredientId });
                    table.CheckConstraint("CK_RecipeDetail_1", "[QuantityRequired] > 0");
                    table.ForeignKey(
                        name: "FK_RecipeDetail_Ingredient_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredient",
                        principalColumn: "IngredientId");
                    table.ForeignKey(
                        name: "FK_RecipeDetail_RecipeVersion_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "RecipeVersion",
                        principalColumn: "RecipeVersionId");
                });

            migrationBuilder.CreateTable(
                name: "OrderLineAddon",
                columns: table => new
                {
                    LineAddonId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineId = table.Column<long>(type: "bigint", nullable: false),
                    AddonVariantId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AddonNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLineAddon", x => x.LineAddonId);
                    table.CheckConstraint("CK_OrderLineAddon_1", "[Quantity] > 0");
                    table.CheckConstraint("CK_OrderLineAddon_2", "[UnitPriceSnapshot] >= 0");
                    table.ForeignKey(
                        name: "FK_OrderLineAddon_OrderLine_LineId",
                        column: x => x.LineId,
                        principalTable: "OrderLine",
                        principalColumn: "LineId");
                    table.ForeignKey(
                        name: "FK_OrderLineAddon_ProductVariant_AddonVariantId",
                        column: x => x.AddonVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                });

            migrationBuilder.CreateTable(
                name: "PreparationUnit",
                columns: table => new
                {
                    UnitId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    LineId = table.Column<long>(type: "bigint", nullable: false),
                    UnitNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecipeVersionId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationUnit", x => x.UnitId);
                    table.CheckConstraint("CK_PreparationUnit_1", "[Status] IN (N'Waiting',N'Preparing',N'Ready',N'Delivered',N'Cancelled')");
                    table.CheckConstraint("CK_PreparationUnit_2", "[UnitNumber] > 0");
                    table.ForeignKey(
                        name: "FK_PreparationUnit_OrderLine_LineId",
                        column: x => x.LineId,
                        principalTable: "OrderLine",
                        principalColumn: "LineId");
                    table.ForeignKey(
                        name: "FK_PreparationUnit_PreparationTicket_TicketId",
                        column: x => x.TicketId,
                        principalTable: "PreparationTicket",
                        principalColumn: "TicketId");
                    table.ForeignKey(
                        name: "FK_PreparationUnit_RecipeVersion_RecipeVersionId",
                        column: x => x.RecipeVersionId,
                        principalTable: "RecipeVersion",
                        principalColumn: "RecipeVersionId");
                });

            migrationBuilder.CreateTable(
                name: "RefundTransaction",
                columns: table => new
                {
                    RefundTransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundId = table.Column<long>(type: "bigint", nullable: false),
                    ShiftId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundTransaction", x => x.RefundTransactionId);
                    table.CheckConstraint("CK_RefundTransaction_1", "[Amount] > 0");
                    table.CheckConstraint("CK_RefundTransaction_2", "[Method] IN (N'Cash',N'BankTransfer')");
                    table.CheckConstraint("CK_RefundTransaction_3", "[Status] IN (N'Pending',N'Succeeded',N'Failed')");
                    table.CheckConstraint("CK_RefundTransaction_4", "[Status] <> N'Succeeded' OR [ExecutedAt] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_RefundTransaction_IdempotencyRecord_RequestId",
                        column: x => x.RequestId,
                        principalTable: "IdempotencyRecord",
                        principalColumn: "RequestId");
                    table.ForeignKey(
                        name: "FK_RefundTransaction_RefundRequest_RefundId",
                        column: x => x.RefundId,
                        principalTable: "RefundRequest",
                        principalColumn: "RefundId");
                    table.ForeignKey(
                        name: "FK_RefundTransaction_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shift",
                        principalColumn: "ShiftId");
                    table.ForeignKey(
                        name: "FK_RefundTransaction_StaffAccount_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "StaffAccount",
                        principalColumn: "StaffId");
                });

            migrationBuilder.CreateTable(
                name: "CancellationItem",
                columns: table => new
                {
                    CancellationId = table.Column<long>(type: "bigint", nullable: false),
                    UnitId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationItem", x => new { x.CancellationId, x.UnitId });
                    table.ForeignKey(
                        name: "FK_CancellationItem_CancellationRequest_CancellationId",
                        column: x => x.CancellationId,
                        principalTable: "CancellationRequest",
                        principalColumn: "CancellationId");
                    table.ForeignKey(
                        name: "FK_CancellationItem_PreparationUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PreparationUnit",
                        principalColumn: "UnitId");
                });

            migrationBuilder.CreateTable(
                name: "PreparationAttempt",
                columns: table => new
                {
                    AttemptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationAttempt", x => x.AttemptId);
                    table.CheckConstraint("CK_PreparationAttempt_1", "[AttemptNo] > 0");
                    table.CheckConstraint("CK_PreparationAttempt_2", "[Outcome] IN (N'Started',N'Completed',N'Failed')");
                    table.ForeignKey(
                        name: "FK_PreparationAttempt_PreparationUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PreparationUnit",
                        principalColumn: "UnitId");
                });

            migrationBuilder.CreateTable(
                name: "PreparationIngredient",
                columns: table => new
                {
                    UnitId = table.Column<long>(type: "bigint", nullable: false),
                    IngredientId = table.Column<long>(type: "bigint", nullable: false),
                    QuantitySnapshot = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparationIngredient", x => new { x.UnitId, x.IngredientId });
                    table.CheckConstraint("CK_PreparationIngredient_1", "[QuantitySnapshot] > 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantitySnapshot]");
                    table.ForeignKey(
                        name: "FK_PreparationIngredient_Ingredient_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredient",
                        principalColumn: "IngredientId");
                    table.ForeignKey(
                        name: "FK_PreparationIngredient_PreparationUnit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "PreparationUnit",
                        principalColumn: "UnitId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_StaffId",
                table: "AuditEvent",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeTable_TableCode",
                table: "CafeTable",
                column: "TableCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CancellationItem_UnitId",
                table: "CancellationItem",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequest_ApprovedBy",
                table: "CancellationRequest",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequest_OrderId",
                table: "CancellationRequest",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequest_RequestedBy",
                table: "CancellationRequest",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Category_CategoryName",
                table: "Category",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Counter_CounterCode",
                table: "Counter",
                column: "CounterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfile_IdentityUserId",
                table: "CustomerProfile",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngredientBatch_IngredientId",
                table: "IngredientBatch",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLine_RoundId",
                table: "OrderLine",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLine_VariantId",
                table: "OrderLine",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLineAddon_AddonVariantId",
                table: "OrderLineAddon",
                column: "AddonVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLineAddon_LineId",
                table: "OrderLineAddon",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRound_OrderId_RoundNumber",
                table: "OrderRound",
                columns: new[] { "OrderId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_CreatedBy",
                table: "Payment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrderId",
                table: "Payment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaymentCode",
                table: "Payment",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RequestId",
                table: "Payment",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ShiftId",
                table: "Payment",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_PermissionCode",
                table: "Permission",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationAttempt_UnitId_AttemptNo",
                table: "PreparationAttempt",
                columns: new[] { "UnitId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationIngredient_IngredientId",
                table: "PreparationIngredient",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_PreparationTicket_RoundId",
                table: "PreparationTicket",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationTicket_Status_CreatedAt",
                table: "PreparationTicket",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PreparationUnit_LineId_UnitNumber",
                table: "PreparationUnit",
                columns: new[] { "LineId", "UnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreparationUnit_RecipeVersionId",
                table: "PreparationUnit",
                column: "RecipeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PreparationUnit_TicketId",
                table: "PreparationUnit",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_VariantId_EffectiveFrom",
                table: "PriceHistory",
                columns: new[] { "VariantId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId",
                table: "Product",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductId",
                table: "ProductVariant",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeDetail_IngredientId",
                table: "RecipeDetail",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeVersion_VariantId_EffectiveFrom",
                table: "RecipeVersion",
                columns: new[] { "VariantId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_AdjustmentId",
                table: "RefundRequest",
                column: "AdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_ApprovedBy",
                table: "RefundRequest",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_PaymentId",
                table: "RefundRequest",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_RefundCode",
                table: "RefundRequest",
                column: "RefundCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_RequestedBy",
                table: "RefundRequest",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransaction_CreatedBy",
                table: "RefundTransaction",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransaction_RefundId",
                table: "RefundTransaction",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransaction_RequestId",
                table: "RefundTransaction",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransaction_ShiftId",
                table: "RefundTransaction",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAdjustment_CreatedBy",
                table: "SalesAdjustment",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAdjustment_OrderId",
                table: "SalesAdjustment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_CustomerId",
                table: "SalesOrder",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_OrderCode",
                table: "SalesOrder",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_SessionId",
                table: "SalesOrder",
                column: "SessionId",
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrder_Status_CreatedAt",
                table: "SalesOrder",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSession_CurrentTableId",
                table: "ServiceSession",
                column: "CurrentTableId",
                unique: true,
                filter: "[ClosedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_ClosedBy",
                table: "Shift",
                column: "ClosedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_CounterId",
                table: "Shift",
                column: "CounterId",
                unique: true,
                filter: "[ClosedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shift_OpenedBy",
                table: "Shift",
                column: "OpenedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCashEntry_CashEntryCode",
                table: "ShiftCashEntry",
                column: "CashEntryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCashEntry_CreatedBy",
                table: "ShiftCashEntry",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftCashEntry_ShiftId",
                table: "ShiftCashEntry",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAccount_IdentityUserId",
                table: "StaffAccount",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffRole_RoleId",
                table: "StaffRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCount_ApprovedBy",
                table: "StockCount",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockCount_CountedBy",
                table: "StockCount",
                column: "CountedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockCount_IngredientId",
                table: "StockCount",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_IngredientId",
                table: "StockMovement",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_MovementCode",
                table: "StockMovement",
                column: "MovementCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TableTransferHistory_FromTableId",
                table: "TableTransferHistory",
                column: "FromTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TableTransferHistory_SessionId",
                table: "TableTransferHistory",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TableTransferHistory_ToTableId",
                table: "TableTransferHistory",
                column: "ToTableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditEvent");

            migrationBuilder.DropTable(
                name: "CancellationItem");

            migrationBuilder.DropTable(
                name: "IngredientBatch");

            migrationBuilder.DropTable(
                name: "OrderLineAddon");

            migrationBuilder.DropTable(
                name: "PreparationAttempt");

            migrationBuilder.DropTable(
                name: "PreparationIngredient");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "RecipeDetail");

            migrationBuilder.DropTable(
                name: "RefundTransaction");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "ShiftCashEntry");

            migrationBuilder.DropTable(
                name: "StaffRole");

            migrationBuilder.DropTable(
                name: "StockBalance");

            migrationBuilder.DropTable(
                name: "StockCount");

            migrationBuilder.DropTable(
                name: "StockMovement");

            migrationBuilder.DropTable(
                name: "TableTransferHistory");

            migrationBuilder.DropTable(
                name: "CancellationRequest");

            migrationBuilder.DropTable(
                name: "PreparationUnit");

            migrationBuilder.DropTable(
                name: "RefundRequest");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Ingredient");

            migrationBuilder.DropTable(
                name: "OrderLine");

            migrationBuilder.DropTable(
                name: "PreparationTicket");

            migrationBuilder.DropTable(
                name: "RecipeVersion");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "SalesAdjustment");

            migrationBuilder.DropTable(
                name: "OrderRound");

            migrationBuilder.DropTable(
                name: "ProductVariant");

            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropTable(
                name: "Shift");

            migrationBuilder.DropTable(
                name: "SalesOrder");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Counter");

            migrationBuilder.DropTable(
                name: "StaffAccount");

            migrationBuilder.DropTable(
                name: "CustomerProfile");

            migrationBuilder.DropTable(
                name: "ServiceSession");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CafeTable");
        }
    }
}
