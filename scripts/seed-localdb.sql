/*
  Idempotent development seed for either:
    LocalDB: (localdb)\MSSQLLocalDB / ECommercePOC
    Docker:  ecommerce-sqlserver / EcommerceDB

  Local demo credentials:
    Admin: admin@local.test / Admin123!
    User:  user@local.test  / User123!

  Do not use these credentials outside a local development database.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT IN (N'ECommercePOC', N'EcommerceDB')
    THROW 51000, 'Refusing to seed a database other than ECommercePOC or EcommerceDB.', 1;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

BEGIN TRY
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Categories WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Mobile Phones'
    )
        INSERT dbo.Categories (Name, Description)
        VALUES (N'Mobile Phones', N'Smartphones and mobile devices');

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Categories WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Laptops'
    )
        INSERT dbo.Categories (Name, Description)
        VALUES (N'Laptops', N'Portable computers for work and entertainment');

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Categories WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Audio'
    )
        INSERT dbo.Categories (Name, Description)
        VALUES (N'Audio', N'Headphones and personal audio equipment');

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Categories WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Accessories'
    )
        INSERT dbo.Categories (Name, Description)
        VALUES (N'Accessories', N'Charging and everyday technology accessories');

    DECLARE @MobilePhonesId int =
        (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE Name = N'Mobile Phones' ORDER BY CategoryId);
    DECLARE @LaptopsId int =
        (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE Name = N'Laptops' ORDER BY CategoryId);
    DECLARE @AudioId int =
        (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE Name = N'Audio' ORDER BY CategoryId);
    DECLARE @AccessoriesId int =
        (SELECT TOP (1) CategoryId FROM dbo.Categories WHERE Name = N'Accessories' ORDER BY CategoryId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'iPhone 14 Pro' AND CategoryId = @MobilePhonesId
    )
        INSERT dbo.Products
            (Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
        VALUES
            (N'iPhone 14 Pro', N'Apple smartphone with a Pro camera system and OLED display.',
             999.00, N'/assets/images/iphone14pro.jpg', 15, 1, 0, @MobilePhonesId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Samsung Galaxy S23' AND CategoryId = @MobilePhonesId
    )
        INSERT dbo.Products
            (Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
        VALUES
            (N'Samsung Galaxy S23', N'Android smartphone with a bright AMOLED display.',
             799.00, N'/assets/images/galaxys23.jpg', 18, 1, 0, @MobilePhonesId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'MacBook Pro 16' AND CategoryId = @LaptopsId
    )
        INSERT dbo.Products
            (Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
        VALUES
            (N'MacBook Pro 16', N'High-performance 16-inch laptop for professional workloads.',
             2499.00, N'/assets/images/macbookpro16.jpg', 8, 1, 0, @LaptopsId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Dell XPS 13' AND CategoryId = @LaptopsId
    )
        INSERT dbo.Products
            (Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
        VALUES
            (N'Dell XPS 13', N'Compact premium Windows laptop with an edge-to-edge display.',
             1299.00, N'/assets/images/dellxps13.jpg', 10, 1, 0, @LaptopsId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Bluetooth Headphones' AND CategoryId = @AudioId
    )
        INSERT dbo.Products
            (Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
        VALUES
            (N'Bluetooth Headphones', N'Wireless over-ear headphones with clear, balanced sound.',
             149.99, N'/assets/images/bluetoothheadphones.jpg', 30, 1, 0, @AudioId);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
        WHERE Name = N'Wireless Charger' AND CategoryId = @AccessoriesId
    )
        INSERT dbo.Products
            (Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
        VALUES
            (N'Wireless Charger', N'Compact wireless charging pad for compatible devices.',
             39.99, N'/assets/images/wirelesscharger.jpg', 40, 1, 0, @AccessoriesId);

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ApplicationUsers WITH (UPDLOCK, HOLDLOCK)
        WHERE Email = N'admin@local.test'
    )
        INSERT dbo.ApplicationUsers
            (Username, Email, PasswordHash, FirstName, LastName, Role, Status, CreatedAt, UpdatedAt)
        VALUES
            (N'localadmin', N'admin@local.test',
             N'$2a$11$1CCL9O8FO.0J214fbznvq.HxqjbnO8ALrhX56MkuwaUp.XpqZRKBe',
             N'Local', N'Admin', N'Admin', N'Active', SYSUTCDATETIME(), SYSUTCDATETIME());

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ApplicationUsers WITH (UPDLOCK, HOLDLOCK)
        WHERE Email = N'user@local.test'
    )
        INSERT dbo.ApplicationUsers
            (Username, Email, PasswordHash, FirstName, LastName, Role, Status, CreatedAt, UpdatedAt)
        VALUES
            (N'localuser', N'user@local.test',
             N'$2a$11$PrCCSLb2Lz1W6SCKFkru4uoEd7b287GgmIoyFcKF9K7zvbQXne18O',
             N'Local', N'User', N'User', N'Active', SYSUTCDATETIME(), SYSUTCDATETIME());

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'Categories' AS Entity, COUNT_BIG(*) AS [Rows] FROM dbo.Categories
UNION ALL
SELECT N'Products', COUNT_BIG(*) FROM dbo.Products
UNION ALL
SELECT N'ApplicationUsers', COUNT_BIG(*) FROM dbo.ApplicationUsers;
