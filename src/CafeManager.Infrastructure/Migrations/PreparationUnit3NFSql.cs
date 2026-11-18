namespace CafeManager.Infrastructure.Migrations;

// SQL cho migration PreparationUnit3NF (chuẩn hóa PreparationUnit về 3NF/BCNF).
// Tên trigger và mã lỗi 51002 giữ nguyên như IntegritySql; chỉ đổi cách suy ra phiếu và công thức của một ly:
//   phiếu của ly    = PreparationTicket có RoundId = OrderLine.RoundId (RoundId là duy nhất trong PreparationTicket)
//   công thức của ly = OrderLine.RecipeVersionId
internal static class PreparationUnit3NFSql
{
    // Chỉ chuyển khi dữ liệu cũ nhất quán: mỗi dòng một phiên bản công thức, và phiếu của từng ly đúng là phiếu của lượt chứa dòng.
    internal const string GuardUp = """
IF EXISTS (SELECT 1 FROM dbo.PreparationUnit GROUP BY LineId HAVING COUNT(DISTINCT RecipeVersionId)>1)
 THROW 51010, 'Cannot normalize: a line has preparation units with different recipe versions', 1;
IF EXISTS (SELECT 1 FROM dbo.PreparationUnit u
 JOIN dbo.OrderLine l ON l.LineId=u.LineId
 JOIN dbo.PreparationTicket t ON t.TicketId=u.TicketId
 WHERE t.RoundId<>l.RoundId)
 THROW 51010, 'Cannot normalize: a preparation unit ticket does not match its line round', 1;
""";

    internal const string CopyRecipeToLine = """
UPDATE l SET l.RecipeVersionId = x.RecipeVersionId
FROM dbo.OrderLine l
JOIN (SELECT LineId, MIN(RecipeVersionId) AS RecipeVersionId FROM dbo.PreparationUnit GROUP BY LineId) x
 ON x.LineId = l.LineId;
""";

    // Khi hạ cấp: mọi ly phải tìm lại được phiếu (theo lượt) và công thức (theo dòng).
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

    // Ly chỉ thuộc lượt đã tiếp nhận có phiếu, số thứ tự không vượt số lượng của dòng, dòng đã chốt công thức đúng biến thể.
    // Đổi ly sang dòng khác chỉ được khi hai dòng cùng lượt, cùng biến thể, cùng công thức.
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

    // Phiếu chỉ thuộc lượt đã tiếp nhận. Thay cho khóa ngoại đã bỏ: không xóa hoặc chuyển phiếu khi lượt cũ còn ly.
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

    // Dòng đã có ly: không giảm số lượng dưới số thứ tự ly, không đổi biến thể hay lượt,
    // và không đổi công thức đã chốt (giữ đúng vết công thức của các ly đã làm).
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
 WHERE (i.RoundId<>d.RoundId OR ISNULL(i.RecipeVersionId,0)<>ISNULL(d.RecipeVersionId,0))
   AND EXISTS (SELECT 1 FROM dbo.PreparationUnit u WHERE u.LineId=i.LineId))
 THROW 51002, 'Line change invalidates preparation units', 1;
END
""";
}
