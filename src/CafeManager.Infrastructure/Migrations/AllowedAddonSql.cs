namespace CafeManager.Infrastructure.Migrations;

internal static class AllowedAddonSql
{
    internal const string TR_AllowedAddon_Type = """
CREATE TRIGGER dbo.TR_AllowedAddon_Type ON dbo.AllowedAddon AFTER INSERT, UPDATE
AS
BEGIN
 SET NOCOUNT ON;

IF EXISTS (
 SELECT 1 FROM inserted a
 JOIN dbo.ProductVariant v ON v.VariantId=a.AddonVariantId
 WHERE v.VariantType<>N'Addon'
) THROW 51008, 'Allowed addon must reference an Addon variant', 1;
END
""";
}
