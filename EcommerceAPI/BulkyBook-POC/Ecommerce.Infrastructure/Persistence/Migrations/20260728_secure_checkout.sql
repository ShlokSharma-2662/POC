/*
  Apply once to existing SQL Server databases before deploying the secure checkout.
  New databases created from the EF model already include these columns and index.
*/

IF COL_LENGTH('Orders', 'PaymentIntentId') IS NULL
    ALTER TABLE Orders ADD PaymentIntentId nvarchar(255) NULL;

IF COL_LENGTH('Orders', 'CartFingerprint') IS NULL
    ALTER TABLE Orders ADD CartFingerprint nvarchar(64) NOT NULL
        CONSTRAINT DF_Orders_CartFingerprint DEFAULT('');

IF COL_LENGTH('Orders', 'TotalAmount') IS NULL
    ALTER TABLE Orders ADD TotalAmount decimal(18,2) NOT NULL
        CONSTRAINT DF_Orders_TotalAmount DEFAULT(0);

IF COL_LENGTH('Orders', 'Currency') IS NULL
    ALTER TABLE Orders ADD Currency nvarchar(3) NOT NULL
        CONSTRAINT DF_Orders_Currency DEFAULT('usd');

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Orders_PaymentIntentId'
      AND object_id = OBJECT_ID('Orders')
)
    CREATE UNIQUE INDEX IX_Orders_PaymentIntentId
        ON Orders(PaymentIntentId)
        WHERE PaymentIntentId IS NOT NULL;
