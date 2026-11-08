-- Read-only inspection; execute against the development database, not master.
IF DB_NAME() <> N'CafeManager_Group6_Dev'
    THROW 51000, 'Select CafeManager_Group6_Dev before running this inspection', 1;

SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT t.name AS TableName FROM sys.tables t ORDER BY t.name;
SELECT OBJECT_NAME(c.object_id) AS TableName, c.name AS ColumnName,
       TYPE_NAME(c.user_type_id) AS SqlType, c.max_length, c.precision, c.scale,
       c.is_nullable, c.is_identity
FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
ORDER BY t.name,c.column_id;
SELECT name, OBJECT_NAME(parent_object_id) AS TableName,
       OBJECT_NAME(referenced_object_id) AS ReferencedTable,
       delete_referential_action_desc, is_disabled, is_not_trusted
FROM sys.foreign_keys ORDER BY TableName,name;
SELECT OBJECT_NAME(parent_object_id) AS TableName, name, definition, is_disabled
FROM sys.check_constraints ORDER BY TableName,name;
SELECT OBJECT_NAME(object_id) AS TableName, name, is_unique, filter_definition
FROM sys.indexes WHERE index_id>0 AND OBJECTPROPERTY(object_id,'IsUserTable')=1
ORDER BY TableName,name;
SELECT name, OBJECT_NAME(parent_id) AS TableName, is_disabled
FROM sys.triggers WHERE is_ms_shipped=0 ORDER BY name;
SELECT CategoryId, CategoryName, IsActive FROM dbo.Category;
SELECT ProductId, ProductName, IsActive FROM dbo.Product;
SELECT IngredientId, QuantityOnHand, QuantityReserved, RowVersion FROM dbo.StockBalance;
SELECT TableCode, Status FROM dbo.CafeTable;
SELECT CounterCode, IsActive FROM dbo.Counter;
SELECT COUNT(*) AS IdentityAccountCount FROM dbo.AspNetUsers;
