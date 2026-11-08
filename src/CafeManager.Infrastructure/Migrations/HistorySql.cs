namespace CafeManager.Infrastructure.Migrations;

internal static class HistorySql
{
    internal const string TR_RefundRequest_History = """
CREATE TRIGGER dbo.TR_RefundRequest_History ON dbo.RefundRequest AFTER UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM deleted d JOIN dbo.RefundTransaction r WITH (UPDLOCK,HOLDLOCK)
 ON r.RefundId=d.RefundId WHERE r.Status IN (N'Pending',N'Succeeded'))
 THROW 51003, 'Cannot change a refund request with active transactions', 1;
END
""";
    internal const string TR_CancellationRequest_History = """
CREATE TRIGGER dbo.TR_CancellationRequest_History ON dbo.CancellationRequest AFTER UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM deleted WHERE Status<>N'Pending')
 THROW 51003, 'Cancellation decisions are immutable', 1;
IF EXISTS (SELECT 1 FROM inserted c JOIN dbo.CancellationItem i ON i.CancellationId=c.CancellationId
 JOIN dbo.PreparationUnit u ON u.UnitId=i.UnitId JOIN dbo.OrderLine l ON l.LineId=u.LineId
 JOIN dbo.OrderRound r ON r.RoundId=l.RoundId WHERE c.OrderId<>r.OrderId)
 THROW 51007, 'Cancellation unit must belong to same order', 1;
END
""";
    internal const string TR_CancellationItem_Order = """
CREATE TRIGGER dbo.TR_CancellationItem_Order ON dbo.CancellationItem AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.CancellationRequest c ON c.CancellationId=i.CancellationId
 JOIN dbo.PreparationUnit u ON u.UnitId=i.UnitId JOIN dbo.OrderLine l ON l.LineId=u.LineId
 JOIN dbo.OrderRound r ON r.RoundId=l.RoundId WHERE c.OrderId<>r.OrderId OR c.Status<>N'Pending')
 OR EXISTS (SELECT 1 FROM deleted d JOIN dbo.CancellationRequest c ON c.CancellationId=d.CancellationId
 WHERE c.Status<>N'Pending')
 THROW 51007, 'Cancellation must target own order while pending', 1;
END
""";
    internal const string TR_AuditEvent_AppendOnly = """
CREATE TRIGGER dbo.TR_AuditEvent_AppendOnly ON dbo.AuditEvent AFTER UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 THROW 51003, 'Audit events are append only', 1;
END
""";
    internal const string TR_StockMovement_AppendOnly = """
CREATE TRIGGER dbo.TR_StockMovement_AppendOnly ON dbo.StockMovement AFTER UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 THROW 51003, 'Stock movements are append only; post correcting movement', 1;
END
""";
}

