using CafeManager.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CafeManager.Infrastructure;

public sealed class CafeDbContext(DbContextOptions<CafeDbContext> options)
    : IdentityUserContext<IdentityUser>(options)
{
    public DbSet<Category> Category => Set<Category>();
    public DbSet<Product> Product => Set<Product>();
    public DbSet<ProductVariant> ProductVariant => Set<ProductVariant>();
    public DbSet<AllowedAddon> AllowedAddon => Set<AllowedAddon>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();
    public DbSet<CafeTable> CafeTable => Set<CafeTable>();
    public DbSet<CustomerProfile> CustomerProfile => Set<CustomerProfile>();
    public DbSet<ServiceSession> ServiceSession => Set<ServiceSession>();
    public DbSet<TableTransferHistory> TableTransferHistory => Set<TableTransferHistory>();
    public DbSet<SalesOrder> SalesOrder => Set<SalesOrder>();
    public DbSet<OrderRound> OrderRound => Set<OrderRound>();
    public DbSet<OrderLine> OrderLine => Set<OrderLine>();
    public DbSet<OrderLineAddon> OrderLineAddon => Set<OrderLineAddon>();
    public DbSet<PreparationTicket> PreparationTicket => Set<PreparationTicket>();
    public DbSet<PreparationUnit> PreparationUnit => Set<PreparationUnit>();
    public DbSet<PreparationAttempt> PreparationAttempt => Set<PreparationAttempt>();
    public DbSet<PreparationIngredient> PreparationIngredient => Set<PreparationIngredient>();
    public DbSet<Counter> Counter => Set<Counter>();
    public DbSet<Shift> Shift => Set<Shift>();
    public DbSet<Payment> Payment => Set<Payment>();
    public DbSet<SalesAdjustment> SalesAdjustment => Set<SalesAdjustment>();
    public DbSet<RefundRequest> RefundRequest => Set<RefundRequest>();
    public DbSet<RefundTransaction> RefundTransaction => Set<RefundTransaction>();
    public DbSet<Ingredient> Ingredient => Set<Ingredient>();
    public DbSet<RecipeVersion> RecipeVersion => Set<RecipeVersion>();
    public DbSet<RecipeDetail> RecipeDetail => Set<RecipeDetail>();
    public DbSet<IngredientBatch> IngredientBatch => Set<IngredientBatch>();
    public DbSet<StockBalance> StockBalance => Set<StockBalance>();
    public DbSet<StockMovement> StockMovement => Set<StockMovement>();
    public DbSet<StockCount> StockCount => Set<StockCount>();
    public DbSet<StaffAccount> StaffAccount => Set<StaffAccount>();
    public DbSet<Role> Role => Set<Role>();
    public DbSet<Permission> Permission => Set<Permission>();
    public DbSet<StaffRole> StaffRole => Set<StaffRole>();
    public DbSet<RolePermission> RolePermission => Set<RolePermission>();
    public DbSet<AuditEvent> AuditEvent => Set<AuditEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecord => Set<IdempotencyRecord>();
    public DbSet<CancellationRequest> CancellationRequest => Set<CancellationRequest>();
    public DbSet<CancellationItem> CancellationItem => Set<CancellationItem>();
    public DbSet<ShiftCashEntry> ShiftCashEntry => Set<ShiftCashEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Category>(e =>
        {
            e.ToTable("Category", t =>
            {
            });
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(200).IsUnicode();
            e.HasIndex(x => x.CategoryName).IsUnique();
        });
        b.Entity<Product>(e =>
        {
            e.ToTable("Product", t =>
            {
            });
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductName).HasMaxLength(200).IsUnicode();
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<ProductVariant>(e =>
        {
            e.ToTable("ProductVariant", t =>
            {
                t.HasCheckConstraint("CK_ProductVariant_1", "[VariantType] IN (N'Drink',N'Addon')");
            });
            e.HasKey(x => x.VariantId);
            e.Property(x => x.VariantName).HasMaxLength(200).IsUnicode();
            e.Property(x => x.VariantType).HasMaxLength(30).IsUnicode();
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<PriceHistory>(e =>
        {
            e.ToTable("PriceHistory", t =>
            {
                t.HasCheckConstraint("CK_PriceHistory_1", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                t.HasCheckConstraint("CK_PriceHistory_2", "[UnitPrice] >= 0");
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.PriceHistoryId);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.EffectiveFrom).HasColumnType("datetime2");
            e.Property(x => x.EffectiveTo).HasColumnType("datetime2");
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.VariantId, x.EffectiveFrom }).IsUnique();
        });
        b.Entity<CafeTable>(e =>
        {
            e.ToTable("CafeTable", t =>
            {
                t.HasCheckConstraint("CK_CafeTable_1", "[Status] IN (N'Ready',N'Serving',N'NeedsCleaning')");
            });
            e.HasKey(x => x.TableId);
            e.Property(x => x.TableCode).HasMaxLength(40).IsUnicode();
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.HasIndex(x => x.TableCode).IsUnique();
        });
        b.Entity<CustomerProfile>(e =>
        {
            e.ToTable("CustomerProfile", t =>
            {
            });
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.IdentityUserId).HasMaxLength(450).IsUnicode();
            e.Property(x => x.FullName).HasMaxLength(200).IsUnicode();
            e.Property(x => x.Phone).HasMaxLength(30).IsUnicode();
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.IdentityUserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.IdentityUserId).IsUnique();
        });
        b.Entity<ServiceSession>(e =>
        {
            e.ToTable("ServiceSession", t =>
            {
                t.HasCheckConstraint("CK_ServiceSession_1", "[ClosedAt] IS NULL OR [ClosedAt] >= [OpenedAt]");
            });
            e.HasKey(x => x.SessionId);
            e.Property(x => x.OpenedAt).HasColumnType("datetime2");
            e.Property(x => x.ClosedAt).HasColumnType("datetime2");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<CafeTable>().WithMany().HasForeignKey(x => x.CurrentTableId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.CurrentTableId).IsUnique().HasFilter("[ClosedAt] IS NULL");
        });
        b.Entity<TableTransferHistory>(e =>
        {
            e.ToTable("TableTransferHistory", t =>
            {
                t.HasCheckConstraint("CK_TableTransferHistory_1", "[FromTableId] <> [ToTableId]");
            });
            e.HasKey(x => x.TransferId);
            e.Property(x => x.TransferAt).HasColumnType("datetime2");
            e.HasOne<ServiceSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<CafeTable>().WithMany().HasForeignKey(x => x.FromTableId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<CafeTable>().WithMany().HasForeignKey(x => x.ToTableId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<AllowedAddon>(e =>
        {
            e.ToTable("AllowedAddon", t =>
            {
                t.HasCheckConstraint("CK_AllowedAddon_1", "[MaxPerUnit] > 0");
            });
            e.HasKey(x => new { x.ProductId, x.AddonVariantId });
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.AddonVariantId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<SalesOrder>(e =>
        {
            e.ToTable("SalesOrder", t =>
            {
                t.HasCheckConstraint("CK_SalesOrder_1", "([OrderType] = N'Web' AND [CustomerId] IS NOT NULL AND [SessionId] IS NULL) OR ([OrderType] = N'Table' AND [SessionId] IS NOT NULL)");
                t.HasCheckConstraint("CK_SalesOrder_2", "[Status] IN (N'Pending',N'Serving',N'Settling',N'Closed',N'Cancelled',N'Rejected')");
                t.HasCheckConstraint("CK_SalesOrder_3", "([RejectedBy] IS NULL AND [RejectedAt] IS NULL AND [RejectReason] IS NULL) OR ([RejectedBy] IS NOT NULL AND [RejectedAt] IS NOT NULL AND [RejectReason] IS NOT NULL AND [Status] = N'Rejected')");
            });
            e.HasKey(x => x.OrderId);
            e.Property(x => x.ContactName).HasMaxLength(200).IsUnicode();
            e.Property(x => x.ContactPhone).HasMaxLength(30).IsUnicode();
            e.Property(x => x.PickupCode).HasMaxLength(20).IsUnicode();
            e.Property(x => x.RejectReason).HasMaxLength(500).IsUnicode();
            e.Property(x => x.RejectedAt).HasColumnType("datetime2");
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.RejectedBy).OnDelete(DeleteBehavior.NoAction);
            e.Property(x => x.OrderCode).HasMaxLength(40).IsUnicode();
            e.Property(x => x.OrderType).HasMaxLength(30).IsUnicode();
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.Property(x => x.ExpectedPickupAt).HasColumnType("datetime2");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<CustomerProfile>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ServiceSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.OrderCode).IsUnique();
            e.HasIndex(x => x.SessionId).IsUnique().HasFilter("[SessionId] IS NOT NULL");
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });
        b.Entity<OrderRound>(e =>
        {
            e.ToTable("OrderRound", t =>
            {
                t.HasCheckConstraint("CK_OrderRound_1", "[Status] IN (N'Draft',N'Accepted',N'Cancelled')");
                t.HasCheckConstraint("CK_OrderRound_2", "([Status] = N'Draft' AND [AcceptedAt] IS NULL) OR ([Status] = N'Accepted' AND [AcceptedAt] IS NOT NULL) OR [Status] = N'Cancelled'");
                t.HasCheckConstraint("CK_OrderRound_3", "[RoundNumber] > 0");
            });
            e.HasKey(x => x.RoundId);
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.AcceptedAt).HasColumnType("datetime2");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.OrderId, x.RoundNumber }).IsUnique();
        });
        b.Entity<OrderLine>(e =>
        {
            e.ToTable("OrderLine", t =>
            {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_OrderLine_1", "[Quantity] > 0");
                t.HasCheckConstraint("CK_OrderLine_2", "[UnitPriceSnapshot] >= 0");
            });
            e.HasKey(x => x.LineId);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(200).IsUnicode();
            e.Property(x => x.VariantNameSnapshot).HasMaxLength(200).IsUnicode();
            e.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
            e.HasOne<OrderRound>().WithMany().HasForeignKey(x => x.RoundId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<OrderLineAddon>(e =>
        {
            e.ToTable("OrderLineAddon", t =>
            {
                t.HasCheckConstraint("CK_OrderLineAddon_1", "[Quantity] > 0");
                t.HasCheckConstraint("CK_OrderLineAddon_2", "[UnitPriceSnapshot] >= 0");
            });
            e.HasKey(x => x.LineAddonId);
            e.Property(x => x.AddonNameSnapshot).HasMaxLength(200).IsUnicode();
            e.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
            e.HasOne<OrderLine>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.AddonVariantId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<PreparationTicket>(e =>
        {
            e.ToTable("PreparationTicket", t =>
            {
                t.HasCheckConstraint("CK_PreparationTicket_1", "[Status] IN (N'Waiting',N'Preparing',N'Ready',N'Completed',N'Cancelled')");
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.TicketId);
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.HasOne<OrderRound>().WithMany().HasForeignKey(x => x.RoundId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.RoundId).IsUnique();
            e.HasIndex(x => new { x.Status, x.CreatedAt });
        });
        b.Entity<PreparationUnit>(e =>
        {
            e.ToTable("PreparationUnit", t =>
            {
                t.HasCheckConstraint("CK_PreparationUnit_1", "[Status] IN (N'Waiting',N'Preparing',N'Ready',N'Delivered',N'Cancelled')");
                t.HasCheckConstraint("CK_PreparationUnit_2", "[UnitNumber] > 0");
                t.HasCheckConstraint("CK_PreparationUnit_3", "([DeliveredBy] IS NULL AND [DeliveredAt] IS NULL) OR ([DeliveredBy] IS NOT NULL AND [DeliveredAt] IS NOT NULL AND [Status] = N'Delivered')");
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.UnitId);
            e.Property(x => x.DeliveredAt).HasColumnType("datetime2");
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.DeliveredBy).OnDelete(DeleteBehavior.NoAction);
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<PreparationTicket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<OrderLine>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<RecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.LineId, x.UnitNumber }).IsUnique();
        });
        b.Entity<PreparationAttempt>(e =>
        {
            e.ToTable("PreparationAttempt", t =>
            {
                t.HasCheckConstraint("CK_PreparationAttempt_1", "[AttemptNo] > 0");
                t.HasCheckConstraint("CK_PreparationAttempt_2", "[Outcome] IN (N'Started',N'Completed',N'Failed')");
            });
            e.HasKey(x => x.AttemptId);
            e.Property(x => x.Outcome).HasMaxLength(30).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.HasOne<PreparationUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.UnitId, x.AttemptNo }).IsUnique();
        });
        b.Entity<PreparationIngredient>(e =>
        {
            e.ToTable("PreparationIngredient", t =>
            {
                t.HasCheckConstraint("CK_PreparationIngredient_1", "[QuantitySnapshot] > 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantitySnapshot]");
            });
            e.HasKey(x => new { x.UnitId, x.IngredientId });
            e.Property(x => x.QuantitySnapshot).HasPrecision(18, 3);
            e.Property(x => x.QuantityReserved).HasPrecision(18, 3);
            e.HasOne<PreparationUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<Counter>(e =>
        {
            e.ToTable("Counter", t =>
            {
            });
            e.HasKey(x => x.CounterId);
            e.Property(x => x.CounterCode).HasMaxLength(40).IsUnicode();
            e.HasIndex(x => x.CounterCode).IsUnique();
        });
        b.Entity<Shift>(e =>
        {
            e.ToTable("Shift", t =>
            {
                t.HasCheckConstraint("CK_Shift_1", "[OpeningCash] >= 0 AND ([CountedCash] IS NULL OR [CountedCash] >= 0)");
                t.HasCheckConstraint("CK_Shift_2", "([ClosedAt] IS NULL AND [ClosedBy] IS NULL AND [CountedCash] IS NULL) OR ([ClosedAt] IS NOT NULL AND [ClosedBy] IS NOT NULL AND [CountedCash] IS NOT NULL AND [ClosedAt] >= [OpenedAt])");
            });
            e.HasKey(x => x.ShiftId);
            e.Property(x => x.OpenedAt).HasColumnType("datetime2");
            e.Property(x => x.OpeningCash).HasPrecision(18, 2);
            e.Property(x => x.CountedCash).HasPrecision(18, 2);
            e.Property(x => x.ClosedAt).HasColumnType("datetime2");
            e.Property(x => x.CloseNote).HasMaxLength(500).IsUnicode();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<Counter>().WithMany().HasForeignKey(x => x.CounterId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.OpenedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.ClosedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.CounterId).IsUnique().HasFilter("[ClosedAt] IS NULL");
        });
        b.Entity<Payment>(e =>
        {
            e.ToTable("Payment", t =>
            {
                t.HasCheckConstraint("CK_Payment_1", "[Amount] > 0");
                t.HasCheckConstraint("CK_Payment_2", "[Method] IN (N'Cash',N'BankTransfer')");
                t.HasCheckConstraint("CK_Payment_3", "[Status] IN (N'Pending',N'Succeeded',N'Failed')");
                t.HasCheckConstraint("CK_Payment_4", "[Status] <> N'Succeeded' OR [PaidAt] IS NOT NULL");
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentCode).HasMaxLength(40).IsUnicode();
            e.Property(x => x.ExternalReference).HasMaxLength(100).IsUnicode();
            e.HasIndex(x => x.ExternalReference).IsUnique().HasFilter("[ExternalReference] IS NOT NULL");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Method).HasMaxLength(30).IsUnicode();
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.PaidAt).HasColumnType("datetime2");
            e.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<IdempotencyRecord>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.PaymentCode).IsUnique();
            e.HasIndex(x => x.RequestId).IsUnique();
        });
        b.Entity<SalesAdjustment>(e =>
        {
            e.ToTable("SalesAdjustment", t =>
            {
            });
            e.HasKey(x => x.AdjustmentId);
            e.Property(x => x.AmountSigned).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(500).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<RefundRequest>(e =>
        {
            e.ToTable("RefundRequest", t =>
            {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_RefundRequest_1", "[Amount] > 0");
                t.HasCheckConstraint("CK_RefundRequest_2", "[Status] IN (N'Pending',N'Approved',N'Rejected')");
                t.HasCheckConstraint("CK_RefundRequest_3", "([Status] = N'Pending' AND [ApprovedBy] IS NULL AND [ApprovedAt] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL AND [ApprovedBy] <> [RequestedBy])");
            });
            e.HasKey(x => x.RefundId);
            e.Property(x => x.RefundCode).HasMaxLength(40).IsUnicode();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.ApprovedAt).HasColumnType("datetime2");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<SalesAdjustment>().WithMany().HasForeignKey(x => x.AdjustmentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.RefundCode).IsUnique();
        });
        b.Entity<RefundTransaction>(e =>
        {
            e.ToTable("RefundTransaction", t =>
            {
                t.HasCheckConstraint("CK_RefundTransaction_1", "[Amount] > 0");
                t.HasCheckConstraint("CK_RefundTransaction_2", "[Method] IN (N'Cash',N'BankTransfer')");
                t.HasCheckConstraint("CK_RefundTransaction_3", "[Status] IN (N'Pending',N'Succeeded',N'Failed')");
                t.HasCheckConstraint("CK_RefundTransaction_4", "[Status] <> N'Succeeded' OR [ExecutedAt] IS NOT NULL");
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.RefundTransactionId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Method).HasMaxLength(30).IsUnicode();
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.ExternalReference).HasMaxLength(100).IsUnicode();
            e.Property(x => x.ExecutedAt).HasColumnType("datetime2");
            e.HasOne<RefundRequest>().WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<IdempotencyRecord>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.RequestId).IsUnique();
        });
        b.Entity<Ingredient>(e =>
        {
            e.ToTable("Ingredient", t =>
            {
            });
            e.HasKey(x => x.IngredientId);
            e.Property(x => x.IngredientName).HasMaxLength(200).IsUnicode();
            e.Property(x => x.UnitName).HasMaxLength(30).IsUnicode();
        });
        b.Entity<RecipeVersion>(e =>
        {
            e.ToTable("RecipeVersion", t =>
            {
                t.HasCheckConstraint("CK_RecipeVersion_1", "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.RecipeVersionId);
            e.Property(x => x.EffectiveFrom).HasColumnType("datetime2");
            e.Property(x => x.EffectiveTo).HasColumnType("datetime2");
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.VariantId, x.EffectiveFrom }).IsUnique();
        });
        b.Entity<RecipeDetail>(e =>
        {
            e.ToTable("RecipeDetail", t =>
            {
                t.HasCheckConstraint("CK_RecipeDetail_1", "[QuantityRequired] > 0");
            });
            e.HasKey(x => new { x.RecipeVersionId, x.IngredientId });
            e.Property(x => x.QuantityRequired).HasPrecision(18, 3);
            e.HasOne<RecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<IngredientBatch>(e =>
        {
            e.ToTable("IngredientBatch", t =>
            {
                t.HasCheckConstraint("CK_IngredientBatch_1", "[QuantityReceived] > 0");
            });
            e.HasKey(x => x.BatchId);
            e.Property(x => x.QuantityReceived).HasPrecision(18, 3);
            e.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<StockBalance>(e =>
        {
            e.ToTable("StockBalance", t =>
            {
                t.HasCheckConstraint("CK_StockBalance_1", "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]");
            });
            e.HasKey(x => x.IngredientId);
            e.Property(x => x.IngredientId).ValueGeneratedNever();
            e.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
            e.Property(x => x.QuantityReserved).HasPrecision(18, 3);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovement", t =>
            {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_StockMovement_1", "[Quantity] > 0");
                t.HasCheckConstraint("CK_StockMovement_2", "[MovementType] IN (N'Receipt',N'Consume',N'Waste',N'AdjustmentIn',N'AdjustmentOut',N'Reserve',N'Release')");
            });
            e.HasKey(x => x.MovementId);
            e.Property(x => x.MovementCode).HasMaxLength(40).IsUnicode();
            e.Property(x => x.MovementType).HasMaxLength(30).IsUnicode();
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.SourceType).HasMaxLength(30).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.MovementCode).IsUnique();
        });
        b.Entity<StockCount>(e =>
        {
            e.ToTable("StockCount", t =>
            {
                t.HasCheckConstraint("CK_StockCount_Approval", "([Status] = N'Pending' AND [ApprovedBy] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL)");
                t.HasCheckConstraint("CK_StockCount_1", "[BookQty] >= 0 AND [CountedQty] >= 0");
                t.HasCheckConstraint("CK_StockCount_2", "[Status] IN (N'Pending',N'Approved',N'Rejected')");
                t.HasCheckConstraint("CK_StockCount_3", "[ApprovedBy] IS NULL OR [ApprovedBy] <> [CountedBy]");
            });
            e.HasKey(x => x.CountId);
            e.Property(x => x.BookQty).HasPrecision(18, 3);
            e.Property(x => x.CountedQty).HasPrecision(18, 3);
            e.Property(x => x.CountedAt).HasColumnType("datetime2");
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.CountedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<StaffAccount>(e =>
        {
            e.ToTable("StaffAccount", t =>
            {
            });
            e.HasKey(x => x.StaffId);
            e.Property(x => x.IdentityUserId).HasMaxLength(450).IsUnicode();
            e.Property(x => x.FullName).HasMaxLength(200).IsUnicode();
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.IdentityUserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.IdentityUserId).IsUnique();
        });
        b.Entity<Role>(e =>
        {
            e.ToTable("Role", t =>
            {
            });
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasMaxLength(100).IsUnicode();
        });
        b.Entity<Permission>(e =>
        {
            e.ToTable("Permission", t =>
            {
            });
            e.HasKey(x => x.PermissionId);
            e.Property(x => x.PermissionCode).HasMaxLength(100).IsUnicode();
            e.HasIndex(x => x.PermissionCode).IsUnique();
        });
        b.Entity<StaffRole>(e =>
        {
            e.ToTable("StaffRole", t =>
            {
            });
            e.HasKey(x => new { x.StaffId, x.RoleId });
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermission", t =>
            {
            });
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<AuditEvent>(e =>
        {
            e.ToTable("AuditEvent", t =>
            {
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => x.AuditId);
            e.Property(x => x.ActionCode).HasMaxLength(100).IsUnicode();
            e.Property(x => x.EntityName).HasMaxLength(100).IsUnicode();
            e.Property(x => x.EntityReference).HasMaxLength(100).IsUnicode();
            e.Property(x => x.Reason).HasMaxLength(500).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("IdempotencyRecord", t =>
            {
            });
            e.HasKey(x => x.RequestId);
            e.Property(x => x.RequestId).ValueGeneratedNever();
            e.Property(x => x.RequestHash).HasMaxLength(128).IsUnicode();
            e.Property(x => x.ResultReference).HasMaxLength(200).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
        });
        b.Entity<CancellationRequest>(e =>
        {
            e.ToTable("CancellationRequest", t =>
            {
                t.UseSqlOutputClause(false);
                t.HasCheckConstraint("CK_CancellationRequest_1", "[Status] IN (N'Pending',N'Approved',N'Rejected')");
                t.HasCheckConstraint("CK_CancellationRequest_2", "([Status] = N'Pending' AND [ApprovedBy] IS NULL AND [ApprovedAt] IS NULL) OR ([Status] IN (N'Approved',N'Rejected') AND [ApprovedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL AND [ApprovedBy] <> [RequestedBy])");
            });
            e.HasKey(x => x.CancellationId);
            e.Property(x => x.Reason).HasMaxLength(500).IsUnicode();
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.Property(x => x.ApprovedAt).HasColumnType("datetime2");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<CancellationItem>(e =>
        {
            e.ToTable("CancellationItem", t =>
            {
                t.UseSqlOutputClause(false);
            });
            e.HasKey(x => new { x.CancellationId, x.UnitId });
            e.HasOne<CancellationRequest>().WithMany().HasForeignKey(x => x.CancellationId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<PreparationUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<ShiftCashEntry>(e =>
        {
            e.ToTable("ShiftCashEntry", t =>
            {
                t.HasCheckConstraint("CK_ShiftCashEntry_1", "[Amount] > 0");
                t.HasCheckConstraint("CK_ShiftCashEntry_2", "[EntryType] IN (N'OtherIn',N'OtherOut')");
                t.HasCheckConstraint("CK_ShiftCashEntry_3", "[Status] IN (N'Posted',N'Voided')");
            });
            e.HasKey(x => x.CashEntryId);
            e.Property(x => x.CashEntryCode).HasMaxLength(40).IsUnicode();
            e.Property(x => x.EntryType).HasMaxLength(30).IsUnicode();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(500).IsUnicode();
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.Property(x => x.Status).HasMaxLength(30).IsUnicode();
            e.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<StaffAccount>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.CashEntryCode).IsUnique();
        });
        // Identity also retains authentication history until explicitly removed.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.NoAction;
    }

    private void ValidateUtc()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        foreach (var property in entry.Properties.Where(p => p.IsModified || entry.State == EntityState.Added))
            if (property.CurrentValue is DateTime time && time.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException($"{entry.Metadata.ClrType.Name}.{property.Metadata.Name} must be UTC.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateUtc();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateUtc();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

