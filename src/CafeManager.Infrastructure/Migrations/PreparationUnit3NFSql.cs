namespace CafeManager.Infrastructure.Migrations;

// Trigger viết lại cho mô hình 3NF của PreparationUnit (migration PreparationUnit3NF).
// Tên trigger và mã lỗi (51002) giữ nguyên như IntegritySql; chỉ đổi cách suy ra phiếu và công thức:
//   phiếu của đơn vị  = PreparationTicket có RoundId = OrderLine.RoundId (RoundId là duy nhất trong PreparationTicket)
//   công thức của đơn vị = OrderLine.RecipeVersionId
internal static class PreparationUnit3NFSql
{
    // Dữ liệu cũ phải nhất quán thì mới chuyển được: mỗi dòng chỉ được có một phiên bản công thức.
    internal const string GuardUp = """
IF EXISTS (SELECT 1 FROM dbo.PreparationUnit GROUP BY LineId HAVING COUNT(DISTINCT RecipeVersionId)>1)
 THROW 51010, 'Cannot normalize: a line has preparation units with different recipe versions', 1;
""";

    internal const string CopyRecipeToLine = """
UPDATE l SET l.RecipeVersionId = x.RecipeVersionId
FROM dbo.OrderLine l
JOIN (SELECT LineId, MIN(RecipeVersionId) AS RecipeVersionId FROM dbo.PreparationUnit GROUP BY LineId) x
 ON x.LineId = l.LineId;
""";

    // Khi hạ cấp: mọi đơn vị phải tìm lại được phiếu (theo lượt) và công thức (theo dòng).
    internal const string GuardDown = """
IF EXISTS (SELECT 1 FROM dbo.PreparationUnit u JOIN dbo.OrderLine l ON l.LineId=u.LineId
 WHERE l.RecipeVersionId IS NULL
    OR NOT EXISTS (SELECT 1 FROM dbo.PreparationTicket t WHERE t.RoundId=l.RoundId))
 THROW 51011, 'Cannot denormalize: a preparation unit has no ticket or recipe version', 1;
""";

    internal const string RestoreTicketAndRecipe = """
UPDATE u SET u.TicketId = t.TicketId, u.RecipeVersionId = l.RecipeVersionId
FROM dbo.PreparationUnit u
JOIN dbo.OrderLine l ON l.LineId = u.LineId
JOIN dbo.PreparationTicket t ON t.RoundId = l.RoundId;
""";

    internal const string TR_PreparationUnit_Round = """
CREATE TRIGGER dbo.TR_PreparationUnit_Round ON dbo.PreparationUnit AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;

IF EXISTS (
 SELECT 1 FROM inserted u
 JOIN dbo.OrderLine l ON l.LineId=u.LineId
 JOIN dbo.OrderRound r ON r.RoundId=l.RoundId
 LEFT JOIN dbo.RecipeVersion v ON v.RecipeVersionId=l.RecipeVersionId
 WHERE r.AcceptedAt IS NULL OR r.Status<>N'Accepted'
    OR u.UnitNumber>l.Quantity
    OR v.RecipeVersionId IS NULL OR v.VariantId<>l.VariantId
    OR NOT EXISTS (SELECT 1 FROM dbo.PreparationTicket t WHERE t.RoundId=l.RoundId)
) OR EXISTS (
 SELECT 1 FROM inserted i JOIN deleted d ON d.UnitId=i.UnitId
 JOIN dbo.OrderLine ol ON ol.LineId=d.LineId
 JOIN dbo.OrderLine nl ON nl.LineId=i.LineId
 WHERE i.LineId<>d.LineId
   AND (ol.RoundId<>nl.RoundId OR ol.VariantId<>nl.VariantId
        OR ISNULL(ol.RecipeVersionId,0)<>ISNULL(nl.RecipeVersionId,0))
) THROW 51002, 'Unit, ticket, line, recipe or round mismatch', 1;
END
""";

    internal const string TR_PreparationTicket_Round = """
CREATE TRIGGER dbo.TR_PreparationTicket_Round ON dbo.PreparationTicket AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM inserted t JOIN dbo.OrderRound r ON r.RoundId=t.RoundId
 WHERE r.Status<>N'Accepted' OR r.AcceptedAt IS NULL)
 OR EXISTS (SELECT 1 FROM deleted d
 JOIN dbo.OrderLine l ON l.RoundId=d.RoundId
 JOIN dbo.PreparationUnit u ON u.LineId=l.LineId
 WHERE NOT EXISTS (SELECT 1 FROM dbo.PreparationTicket t WHERE t.RoundId=d.RoundId))
 THROW 51002, 'Ticket must belong to accepted round and match lines', 1;
END
""";

    internal const string TR_OrderLine_Units = """
CREATE TRIGGER dbo.TR_OrderLine_Units ON dbo.OrderLine AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM inserted l JOIN dbo.PreparationUnit u ON u.LineId=l.LineId
 LEFT JOIN dbo.RecipeVersion v ON v.RecipeVersionId=l.RecipeVersionId
 WHERE u.UnitNumber>l.Quantity
    OR v.RecipeVersionId IS NULL OR v.VariantId<>l.VariantId
    OR NOT EXISTS (SELECT 1 FROM dbo.PreparationTicket t WHERE t.RoundId=l.RoundId))
 OR EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.LineId=i.LineId
 WHERE i.RoundId<>d.RoundId AND EXISTS (SELECT 1 FROM dbo.PreparationUnit u WHERE u.LineId=i.LineId))
 THROW 51002, 'Line change invalidates preparation units', 1;
END
""";
}
