-- ============================================================
-- ECommercePOC — Seed Data Script
-- Target: (localdb)\MSSQLLocalDB / ECommercePOC
-- Run:  sqlcmd -S "(localdb)\MSSQLLocalDB" -d ECommercePOC -i scripts\seed-data.sql
-- ============================================================

USE [ECommercePOC];
GO

SET NOCOUNT ON;
SET IDENTITY_INSERT [dbo].[ApplicationUsers] ON;

-- ============================================================
-- 1. APPLICATION USERS
--    Passwords:  Admin@123  |  User@123
-- ============================================================
MERGE INTO [dbo].[ApplicationUsers] AS tgt
USING (VALUES
    (1, N'admin',       N'admin@test.com',       N'$2a$11$Ga50ruZl9U6oLs3JnexpV.l2Uu0S2OG9eWPVq6TqLvifk8tBS5rny', N'Admin',    N'User',     N'Admin', N'Active', GETUTCDATE(), GETUTCDATE()),
    (2, N'johndoe',     N'john@test.com',        N'$2a$11$uqzB1ZH1BcY626AOb2/1W.Uzi0ZbEHkJkKYlocDY1KuG2KAyqQ2DS', N'John',     N'Doe',      N'User',  N'Active', GETUTCDATE(), GETUTCDATE()),
    (3, N'janesmith',   N'jane@test.com',        N'$2a$11$uqzB1ZH1BcY626AOb2/1W.Uzi0ZbEHkJkKYlocDY1KuG2KAyqQ2DS', N'Jane',     N'Smith',    N'User',  N'Active', GETUTCDATE(), GETUTCDATE()),
    (4, N'bobwilson',   N'bob@test.com',         N'$2a$11$uqzB1ZH1BcY626AOb2/1W.Uzi0ZbEHkJkKYlocDY1KuG2KAyqQ2DS', N'Bob',      N'Wilson',   N'User',  N'Active', GETUTCDATE(), GETUTCDATE()),
    (5, N'alicejones',  N'alice@test.com',       N'$2a$11$uqzB1ZH1BcY626AOb2/1W.Uzi0ZbEHkJkKYlocDY1KuG2KAyqQ2DS', N'Alice',    N'Jones',    N'User',  N'Active', GETUTCDATE(), GETUTCDATE())
) AS src (Id, Username, Email, PasswordHash, FirstName, LastName, Role, Status, CreatedAt, UpdatedAt)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Username, Email, PasswordHash, FirstName, LastName, Role, Status, CreatedAt, UpdatedAt)
    VALUES (src.Id, src.Username, src.Email, src.PasswordHash, src.FirstName, src.LastName, src.Role, src.Status, src.CreatedAt, src.UpdatedAt);

SET IDENTITY_INSERT [dbo].[ApplicationUsers] OFF;
PRINT '✅ ApplicationUsers seeded';
GO

-- ============================================================
-- 2. CATEGORIES
-- ============================================================
SET IDENTITY_INSERT [dbo].[Categories] ON;

MERGE INTO [dbo].[Categories] AS tgt
USING (VALUES
    (1, N'Electronics',       N'Smartphones, laptops, tablets, and other electronic devices'),
    (2, N'Clothing',          N'Men''s and women''s apparel, shoes, and accessories'),
    (3, N'Books',             N'Fiction, non-fiction, textbooks, and e-books'),
    (4, N'Home & Kitchen',    N'Furniture, appliances, cookware, and home décor'),
    (5, N'Sports & Outdoors', N'Fitness equipment, camping gear, and sportswear'),
    (6, N'Toys & Games',      N'Board games, action figures, puzzles, and educational toys'),
    (7, N'Health & Beauty',   N'Skincare, supplements, grooming, and wellness products'),
    (8, N'Automotive',        N'Car accessories, tools, and maintenance supplies')
) AS src (CategoryId, Name, Description)
ON tgt.CategoryId = src.CategoryId
WHEN NOT MATCHED THEN
    INSERT (CategoryId, Name, Description)
    VALUES (src.CategoryId, src.Name, src.Description);

SET IDENTITY_INSERT [dbo].[Categories] OFF;
PRINT '✅ Categories seeded';
GO

-- ============================================================
-- 3. PRODUCTS
-- ============================================================
SET IDENTITY_INSERT [dbo].[Products] ON;

MERGE INTO [dbo].[Products] AS tgt
USING (VALUES
    -- Electronics
    ( 1, N'iPhone 15 Pro',             N'Apple iPhone 15 Pro with A17 Pro chip, 256GB storage, and titanium design.',                   999.99,  N'/images/products/iphone15pro.jpg',       50, 1, 1, 0),
    ( 2, N'Samsung Galaxy S24 Ultra',  N'Samsung flagship with S Pen, 200MP camera, and Snapdragon 8 Gen 3.',                          1199.99, N'/images/products/galaxys24.jpg',         35, 1, 1, 0),
    ( 3, N'MacBook Air M3',            N'Apple MacBook Air with M3 chip, 15-inch Liquid Retina display, 16GB RAM.',                    1299.00, N'/images/products/macbookair.jpg',        25, 1, 1, 0),
    ( 4, N'Sony WH-1000XM5',          N'Premium noise-cancelling wireless headphones with 30-hour battery life.',                       349.99, N'/images/products/sonywh1000.jpg',        80, 1, 1, 0),
    ( 5, N'iPad Pro 12.9"',            N'Apple iPad Pro with M2 chip, Liquid Retina XDR display, and Apple Pencil support.',            1099.00, N'/images/products/ipadpro.jpg',           40, 1, 1, 0),

    -- Clothing
    ( 6, N'Nike Air Max 270',          N'Men''s running shoes with Max Air unit for all-day comfort.',                                  149.99, N'/images/products/nikeairmax.jpg',       120, 1, 2, 0),
    ( 7, N'Levi''s 501 Original',      N'Classic straight-fit jeans in dark indigo wash.',                                               69.50, N'/images/products/levis501.jpg',         200, 1, 2, 0),
    ( 8, N'North Face Puffer Jacket',  N'700-fill goose down insulated jacket for extreme cold.',                                       249.00, N'/images/products/northface.jpg',         60, 1, 2, 0),
    ( 9, N'Adidas Ultraboost',         N'Women''s running shoes with responsive Boost midsole.',                                        189.99, N'/images/products/ultraboost.jpg',        90, 1, 2, 0),

    -- Books
    (10, N'Clean Code',                N'A Handbook of Agile Software Craftsmanship by Robert C. Martin.',                               39.99, N'/images/products/cleancode.jpg',        150, 1, 3, 0),
    (11, N'Designing Data-Intensive Applications', N'The definitive guide to distributed systems by Martin Kleppmann.',                   44.99, N'/images/products/ddia.jpg',             100, 1, 3, 0),
    (12, N'The Pragmatic Programmer',  N'Your journey to mastery, 20th Anniversary Edition.',                                            49.99, N'/images/products/pragprog.jpg',          80, 1, 3, 0),

    -- Home & Kitchen
    (13, N'Instant Pot Duo 7-in-1',    N'Electric pressure cooker, slow cooker, rice cooker, steamer, sauté, yogurt maker, warmer.',     89.95, N'/images/products/instantpot.jpg',       110, 1, 4, 0),
    (14, N'Dyson V15 Detect',          N'Cordless vacuum with laser dust detection and HEPA filtration.',                                749.99, N'/images/products/dysonv15.jpg',          30, 1, 4, 0),
    (15, N'KitchenAid Stand Mixer',    N'Artisan series 5-quart tilt-head stand mixer in Empire Red.',                                   379.99, N'/images/products/kitchenaid.jpg',        45, 1, 4, 0),

    -- Sports & Outdoors
    (16, N'Yoga Mat Premium',          N'Extra-thick 6mm non-slip exercise mat with carrying strap.',                                     29.99, N'/images/products/yogamat.jpg',          300, 1, 5, 0),
    (17, N'Fitbit Charge 6',           N'Advanced fitness tracker with built-in GPS and heart rate monitoring.',                          149.95, N'/images/products/fitbit.jpg',            75, 1, 5, 0),
    (18, N'Coleman 4-Person Tent',     N'WeatherTec system dome tent with easy 10-minute setup.',                                        119.99, N'/images/products/colemantent.jpg',       55, 1, 5, 0),

    -- Toys & Games
    (19, N'LEGO Technic Bugatti',      N'3,599-piece scale model of Bugatti Chiron with working gearbox.',                               349.99, N'/images/products/legobugatti.jpg',       20, 1, 6, 0),
    (20, N'PlayStation 5',             N'Sony PS5 console with DualSense controller and 825GB SSD.',                                     499.99, N'/images/products/ps5.jpg',               15, 1, 6, 0),

    -- Health & Beauty
    (21, N'CeraVe Moisturizing Cream', N'Daily face and body moisturizer for dry skin with ceramides and hyaluronic acid.',               16.99, N'/images/products/cerave.jpg',           250, 1, 7, 0),
    (22, N'Vitamin D3 5000 IU',        N'High-potency vitamin D supplement, 360 softgels.',                                               18.99, N'/images/products/vitamind.jpg',         180, 1, 7, 0),

    -- Automotive
    (23, N'Portable Jump Starter',     N'2000A peak car battery jump starter with USB-C power bank.',                                     79.99, N'/images/products/jumpstarter.jpg',       65, 1, 8, 0),
    (24, N'Dash Cam 4K',              N'Front and rear dual dash camera with night vision and parking mode.',                             129.99, N'/images/products/dashcam.jpg',           50, 1, 8, 0),

    -- Soft-deleted product (for admin testing)
    (25, N'Discontinued Widget',       N'This product has been discontinued and soft-deleted.',                                             9.99, N'/images/products/widget.jpg',             0, 0, 1, 1)
) AS src (ProductId, Name, Description, Price, ImageUrl, Stock, IsActive, CategoryId, IsDeleted)
ON tgt.ProductId = src.ProductId
WHEN NOT MATCHED THEN
    INSERT (ProductId, Name, Description, Price, ImageUrl, Stock, IsActive, IsDeleted, CategoryId)
    VALUES (src.ProductId, src.Name, src.Description, src.Price, src.ImageUrl, src.Stock, src.IsActive, src.IsDeleted, src.CategoryId);

SET IDENTITY_INSERT [dbo].[Products] OFF;
PRINT '✅ Products seeded';
GO

-- ============================================================
-- 4. ORDERS
-- ============================================================
MERGE INTO [dbo].[Orders] AS tgt
USING (VALUES
    (N'A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D', N'John Doe',     N'123 Main St, New York, NY 10001',     N'+1-555-0101', DATEADD(DAY, -10, GETUTCDATE()), N'Delivered', 2),
    (N'B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E', N'Jane Smith',   N'456 Oak Ave, Los Angeles, CA 90001',  N'+1-555-0102', DATEADD(DAY, -5, GETUTCDATE()),  N'Shipped',   3),
    (N'C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F', N'Bob Wilson',   N'789 Pine Rd, Chicago, IL 60601',      N'+1-555-0103', DATEADD(DAY, -2, GETUTCDATE()),  N'Processing', 4),
    (N'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A', N'Alice Jones',  N'321 Elm St, Houston, TX 77001',       N'+1-555-0104', DATEADD(DAY, -1, GETUTCDATE()),  N'Pending',   5),
    (N'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B', N'John Doe',     N'123 Main St, New York, NY 10001',     N'+1-555-0101', GETUTCDATE(),                    N'Pending',   2)
) AS src (Id, CustomerName, ShippingAddress, Phone, CreatedAt, Status, UserId)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, CustomerName, ShippingAddress, Phone, CreatedAt, Status, UserId)
    VALUES (src.Id, src.CustomerName, src.ShippingAddress, src.Phone, src.CreatedAt, src.Status, src.UserId);

PRINT '✅ Orders seeded';
GO

-- ============================================================
-- 5. ORDER ITEMS
-- ============================================================
SET IDENTITY_INSERT [dbo].[OrderItems] ON;

MERGE INTO [dbo].[OrderItems] AS tgt
USING (VALUES
    -- Order 1 (John - Delivered)
    (1,  1, 1, N'A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D'),  -- iPhone 15 Pro x1
    (2,  4, 2, N'A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D'),  -- Sony WH-1000XM5 x2
    -- Order 2 (Jane - Shipped)
    (3,  6, 1, N'B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E'),  -- Nike Air Max x1
    (4, 10, 3, N'B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E'),  -- Clean Code x3
    (5, 21, 2, N'B2C3D4E5-F6A7-4B8C-9D0E-1F2A3B4C5D6E'),  -- CeraVe x2
    -- Order 3 (Bob - Processing)
    (6,  3, 1, N'C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F'),  -- MacBook Air x1
    (7, 13, 1, N'C3D4E5F6-A7B8-4C9D-0E1F-2A3B4C5D6E7F'),  -- Instant Pot x1
    -- Order 4 (Alice - Pending)
    (8, 19, 1, N'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A'),  -- LEGO Bugatti x1
    (9, 17, 1, N'D4E5F6A7-B8C9-4D0E-1F2A-3B4C5D6E7F8A'),  -- Fitbit x1
    -- Order 5 (John - Pending, new order)
    (10, 20, 1, N'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B'), -- PS5 x1
    (11, 16, 2, N'E5F6A7B8-C9D0-4E1F-2A3B-4C5D6E7F8A9B')  -- Yoga Mat x2
) AS src (Id, ProductId, Quantity, OrderId)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, ProductId, Quantity, OrderId)
    VALUES (src.Id, src.ProductId, src.Quantity, src.OrderId);

SET IDENTITY_INSERT [dbo].[OrderItems] OFF;
PRINT '✅ OrderItems seeded';
GO

-- ============================================================
-- 6. CARTS
-- ============================================================
SET IDENTITY_INSERT [dbo].[Cart] ON;

MERGE INTO [dbo].[Cart] AS tgt
USING (VALUES
    (1, N'2', GETUTCDATE()),    -- John's cart
    (2, N'3', GETUTCDATE()),    -- Jane's cart
    (3, N'5', GETUTCDATE())     -- Alice's cart
) AS src (CartId, UserId, CreatedAt)
ON tgt.CartId = src.CartId
WHEN NOT MATCHED THEN
    INSERT (CartId, UserId, CreatedAt)
    VALUES (src.CartId, src.UserId, src.CreatedAt);

SET IDENTITY_INSERT [dbo].[Cart] OFF;
PRINT '✅ Carts seeded';
GO

-- ============================================================
-- 7. CART ITEMS
-- ============================================================
SET IDENTITY_INSERT [dbo].[CartItems] ON;

MERGE INTO [dbo].[CartItems] AS tgt
USING (VALUES
    (1, 1, 2,  1, GETUTCDATE()),   -- John: Samsung Galaxy x1
    (2, 1, 11, 2, GETUTCDATE()),   -- John: DDIA x2
    (3, 2, 15, 1, GETUTCDATE()),   -- Jane: KitchenAid x1
    (4, 3, 8,  1, GETUTCDATE()),   -- Alice: North Face Jacket x1
    (5, 3, 22, 3, GETUTCDATE())    -- Alice: Vitamin D x3
) AS src (CartItemId, CartId, ProductId, Quantity, AddedAt)
ON tgt.CartItemId = src.CartItemId
WHEN NOT MATCHED THEN
    INSERT (CartItemId, CartId, ProductId, Quantity, AddedAt)
    VALUES (src.CartItemId, src.CartId, src.ProductId, src.Quantity, src.AddedAt);

SET IDENTITY_INSERT [dbo].[CartItems] OFF;
PRINT '✅ CartItems seeded';
GO

-- ============================================================
-- 8. WISHLIST ITEMS
-- ============================================================
SET IDENTITY_INSERT [dbo].[WishlistItems] ON;

MERGE INTO [dbo].[WishlistItems] AS tgt
USING (VALUES
    (1, 2, 3,  GETUTCDATE(), 1),   -- John wants MacBook Air
    (2, 2, 20, GETUTCDATE(), 1),   -- John wants PS5
    (3, 3, 14, GETUTCDATE(), 1),   -- Jane wants Dyson V15
    (4, 5, 5,  GETUTCDATE(), 1),   -- Alice wants iPad Pro
    (5, 5, 19, GETUTCDATE(), 1)    -- Alice wants LEGO Bugatti
) AS src (Id, UserId, ProductId, AddedAt, IsActive)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, UserId, ProductId, AddedAt, IsActive)
    VALUES (src.Id, src.UserId, src.ProductId, src.AddedAt, src.IsActive);

SET IDENTITY_INSERT [dbo].[WishlistItems] OFF;
PRINT '✅ WishlistItems seeded';
GO

-- ============================================================
-- 9. SYSTEM METRICS (sample monitoring data)
-- ============================================================
SET IDENTITY_INSERT [dbo].[SystemMetrics] ON;

MERGE INTO [dbo].[SystemMetrics] AS tgt
USING (VALUES
    (1, N'/api/products',             N'GET',  45,  200, DATEADD(MINUTE, -30, GETUTCDATE()), 0, N'Mozilla/5.0', N'127.0.0.1'),
    (2, N'/api/products',             N'GET',  52,  200, DATEADD(MINUTE, -25, GETUTCDATE()), 0, N'Mozilla/5.0', N'127.0.0.1'),
    (3, N'/api/auth/login',           N'POST', 120, 200, DATEADD(MINUTE, -20, GETUTCDATE()), 0, N'Mozilla/5.0', N'192.168.1.10'),
    (4, N'/api/orders/checkout',      N'POST', 350, 200, DATEADD(MINUTE, -15, GETUTCDATE()), 0, N'Mozilla/5.0', N'192.168.1.10'),
    (5, N'/api/products',             N'GET',  2500,200, DATEADD(MINUTE, -10, GETUTCDATE()), 1, N'Mozilla/5.0', N'10.0.0.5'),
    (6, N'/api/categories',           N'GET',  30,  200, DATEADD(MINUTE, -5,  GETUTCDATE()), 0, N'Mozilla/5.0', N'127.0.0.1'),
    (7, N'/api/products/1',           N'GET',  65,  200, GETUTCDATE(),                       0, N'PostmanRuntime/7.36', N'127.0.0.1'),
    (8, N'/api/admin/orders',         N'GET',  180, 200, GETUTCDATE(),                       0, N'Mozilla/5.0', N'127.0.0.1'),
    (9, N'/api/products',             N'POST', 95,  401, DATEADD(MINUTE, -8, GETUTCDATE()),  0, N'curl/8.4',    N'10.0.0.99'),
    (10,N'/api/orders/checkout',      N'POST', 5200,500, DATEADD(MINUTE, -3, GETUTCDATE()),  1, N'Mozilla/5.0', N'192.168.1.15')
) AS src (Id, Endpoint, Method, ResponseTimeMs, StatusCode, Timestamp, IsThresholdExceeded, UserAgent, IpAddress)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Endpoint, Method, ResponseTimeMs, StatusCode, Timestamp, IsThresholdExceeded, UserAgent, IpAddress)
    VALUES (src.Id, src.Endpoint, src.Method, src.ResponseTimeMs, src.StatusCode, src.Timestamp, src.IsThresholdExceeded, src.UserAgent, src.IpAddress);

SET IDENTITY_INSERT [dbo].[SystemMetrics] OFF;
PRINT '✅ SystemMetrics seeded';
GO

-- ============================================================
-- 10. API ALERTS (sample alert data)
-- ============================================================
SET IDENTITY_INSERT [dbo].[ApiAlerts] ON;

MERGE INTO [dbo].[ApiAlerts] AS tgt
USING (VALUES
    (1, N'/api/products',        2500, DATEADD(MINUTE, -10, GETUTCDATE())),
    (2, N'/api/orders/checkout', 5200, DATEADD(MINUTE, -3,  GETUTCDATE()))
) AS src (Id, Path, ResponseTimeMs, Timestamp)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Path, ResponseTimeMs, Timestamp)
    VALUES (src.Id, src.Path, src.ResponseTimeMs, src.Timestamp);

SET IDENTITY_INSERT [dbo].[ApiAlerts] OFF;
PRINT '✅ ApiAlerts seeded';
GO

-- ============================================================
-- 11. ERROR LOGS (sample error data)
-- ============================================================
SET IDENTITY_INSERT [dbo].[ErrorLogs] ON;

MERGE INTO [dbo].[ErrorLogs] AS tgt
USING (VALUES
    (1, N'Unhandled exception in checkout pipeline',       N'System.InvalidOperationException: Insufficient stock for ProductId 20...',   N'/api/orders/checkout', DATEADD(HOUR, -2, GETUTCDATE()), N'Error',    N'Mozilla/5.0', N'192.168.1.15'),
    (2, N'Database connection timeout',                     N'Microsoft.Data.SqlClient.SqlException: Connection Timeout Expired...',      N'/api/products',        DATEADD(HOUR, -1, GETUTCDATE()), N'Critical', N'Mozilla/5.0', N'127.0.0.1'),
    (3, N'JWT token validation failed',                     N'Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException...',           N'/api/admin/orders',    DATEADD(MINUTE, -30, GETUTCDATE()), N'Warning', N'PostmanRuntime/7.36', N'10.0.0.99')
) AS src (Id, Message, StackTrace, Path, Timestamp, Severity, UserAgent, IpAddress)
ON tgt.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Message, StackTrace, Path, Timestamp, Severity, UserAgent, IpAddress)
    VALUES (src.Id, src.Message, src.StackTrace, src.Path, src.Timestamp, src.Severity, src.UserAgent, src.IpAddress);

SET IDENTITY_INSERT [dbo].[ErrorLogs] OFF;
PRINT '✅ ErrorLogs seeded';
GO

-- ============================================================
-- Summary
-- ============================================================
PRINT '';
PRINT '============================';
PRINT '  SEED DATA COMPLETE';
PRINT '============================';
SELECT 'ApplicationUsers' AS [Table], COUNT(*) AS [Rows] FROM ApplicationUsers UNION ALL
SELECT 'Categories',        COUNT(*) FROM Categories        UNION ALL
SELECT 'Products',          COUNT(*) FROM Products          UNION ALL
SELECT 'Orders',            COUNT(*) FROM Orders            UNION ALL
SELECT 'OrderItems',        COUNT(*) FROM OrderItems        UNION ALL
SELECT 'Cart',              COUNT(*) FROM Cart              UNION ALL
SELECT 'CartItems',         COUNT(*) FROM CartItems         UNION ALL
SELECT 'WishlistItems',     COUNT(*) FROM WishlistItems     UNION ALL
SELECT 'SystemMetrics',     COUNT(*) FROM SystemMetrics     UNION ALL
SELECT 'ApiAlerts',         COUNT(*) FROM ApiAlerts         UNION ALL
SELECT 'ErrorLogs',         COUNT(*) FROM ErrorLogs
ORDER BY [Table];
GO
