namespace CafeManager.Infrastructure.Migrations;

internal static class IntegritySql
{
    internal const string TR_PriceHistory_NoOverlap = """
CREATE TRIGGER dbo.TR_PriceHistory_NoOverlap ON dbo.PriceHistory AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (
    SELECT 1 FROM dbo.PriceHistory a WITH (UPDLOCK,HOLDLOCK)
    JOIN dbo.PriceHistory b WITH (UPDLOCK,HOLDLOCK) ON a.VariantId=b.VariantId AND a.PriceHistoryId<b.PriceHistoryId
    WHERE a.VariantId IN (SELECT VariantId FROM inserted)
      AND (b.EffectiveTo IS NULL OR a.EffectiveFrom<b.EffectiveTo)
      AND (a.EffectiveTo IS NULL OR b.EffectiveFrom<a.EffectiveTo)
) THROW 51001, 'Overlapping effective intervals', 1;
END
""";
    internal const string TR_RecipeVersion_NoOverlap = """
CREATE TRIGGER dbo.TR_RecipeVersion_NoOverlap ON dbo.RecipeVersion AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (
    SELECT 1 FROM dbo.RecipeVersion a WITH (UPDLOCK,HOLDLOCK)
    JOIN dbo.RecipeVersion b WITH (UPDLOCK,HOLDLOCK) ON a.VariantId=b.VariantId AND a.RecipeVersionId<b.RecipeVersionId
    WHERE a.VariantId IN (SELECT VariantId FROM inserted)
      AND (b.EffectiveTo IS NULL OR a.EffectiveFrom<b.EffectiveTo)
      AND (a.EffectiveTo IS NULL OR b.EffectiveFrom<a.EffectiveTo)
) THROW 51001, 'Overlapping effective intervals', 1;
END
""";
    internal const string TR_PreparationUnit_Round = """
CREATE TRIGGER dbo.TR_PreparationUnit_Round ON dbo.PreparationUnit AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (
 SELECT 1 FROM inserted u
 JOIN dbo.PreparationTicket t ON t.TicketId=u.TicketId
 JOIN dbo.OrderLine l ON l.LineId=u.LineId
 JOIN dbo.OrderRound r ON r.RoundId=t.RoundId
 JOIN dbo.RecipeVersion v ON v.RecipeVersionId=u.RecipeVersionId
 WHERE t.RoundId<>l.RoundId OR r.AcceptedAt IS NULL OR r.Status<>N'Accepted'
    OR u.UnitNumber>l.Quantity OR v.VariantId<>l.VariantId
) THROW 51002, 'Unit, ticket, line, recipe or round mismatch', 1;
END
""";
    internal const string TR_PreparationTicket_Round = """
CREATE TRIGGER dbo.TR_PreparationTicket_Round ON dbo.PreparationTicket AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM inserted t JOIN dbo.OrderRound r ON r.RoundId=t.RoundId
 WHERE r.Status<>N'Accepted' OR r.AcceptedAt IS NULL)
 OR EXISTS (SELECT 1 FROM inserted t JOIN dbo.PreparationUnit u ON u.TicketId=t.TicketId
 JOIN dbo.OrderLine l ON l.LineId=u.LineId WHERE l.RoundId<>t.RoundId)
 THROW 51002, 'Ticket must belong to accepted round and match lines', 1;
END
""";
    internal const string TR_OrderLine_Units = """
CREATE TRIGGER dbo.TR_OrderLine_Units ON dbo.OrderLine AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM inserted l JOIN dbo.PreparationUnit u ON u.LineId=l.LineId
 JOIN dbo.PreparationTicket t ON t.TicketId=u.TicketId
 JOIN dbo.RecipeVersion v ON v.RecipeVersionId=u.RecipeVersionId
 WHERE t.RoundId<>l.RoundId OR u.UnitNumber>l.Quantity OR v.VariantId<>l.VariantId)
 THROW 51002, 'Line change invalidates preparation units', 1;
END
""";
    internal const string TR_Payment_History = """
CREATE TRIGGER dbo.TR_Payment_History ON dbo.Payment AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM deleted WHERE Status=N'Succeeded')
 THROW 51003, 'Successful payments are immutable; use refund documents', 1;
IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Shift s WITH (UPDLOCK,HOLDLOCK) ON s.ShiftId=i.ShiftId
 WHERE s.ClosedAt IS NOT NULL)
 THROW 51004, 'Cannot post into closed shift', 1;
IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.RefundTransaction r ON r.RequestId=i.RequestId)
 THROW 51005, 'Request already used by refund operation', 1;
END
""";
    internal const string TR_RefundTransaction_Limit = """
CREATE TRIGGER dbo.TR_RefundTransaction_Limit ON dbo.RefundTransaction AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 
IF EXISTS (SELECT 1 FROM deleted WHERE Status=N'Succeeded')
 THROW 51003, 'Successful refunds are immutable', 1;
IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Shift s WITH (UPDLOCK,HOLDLOCK) ON s.ShiftId=i.ShiftId
 WHERE s.ClosedAt IS NOT NULL)
 THROW 51004, 'Cannot post into closed shift', 1;
IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Payment p ON p.RequestId=i.RequestId)
 THROW 51005, 'Request already used by payment operation', 1;
IF EXISTS (SELECT 1 FROM inserted i
 JOIN dbo.RefundRequest rr ON rr.RefundId=i.RefundId
 JOIN dbo.Payment p WITH (UPDLOCK,HOLDLOCK) ON p.PaymentId=rr.PaymentId
 WHERE i.Status IN (N'Pending',N'Succeeded') AND
 (rr.Status<>N'Approved' OR p.Status<>N'Succeeded'
  OR (SELECT COALESCE(SUM(rt.Amount),0) FROM dbo.RefundTransaction rt WITH (UPDLOCK,HOLDLOCK)
      JOIN dbo.RefundRequest r ON r.RefundId=rt.RefundId
      WHERE r.PaymentId=p.PaymentId AND rt.Status IN (N'Pending',N'Succeeded'))>p.Amount
  OR (SELECT COALESCE(SUM(rt.Amount),0) FROM dbo.RefundTransaction rt WITH (UPDLOCK,HOLDLOCK)
      WHERE rt.RefundId=rr.RefundId AND rt.Status IN (N'Pending',N'Succeeded'))>rr.Amount))
 THROW 51006, 'Refund not approved or exceeds request/payment allowance', 1;
END
""";
}

