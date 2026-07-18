-- Required for Qi refill / debt payment flow (ProfileId, SaleType, RefillExecuted, Purpose).
-- Run on each tenant database (IBS_WiFi, IBS_ConnectFTTH, IBS_KGD, IBS_SASNet,
-- IBS_MyNetSpeed, IBS_Online, IBS_Wael, IBS_ArjwanFTTH).

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    RAISERROR('Payments table not found.', 16, 1);
    RETURN;
END

IF COL_LENGTH('dbo.Payments', 'ProfileId') IS NULL
    ALTER TABLE dbo.Payments ADD ProfileId int NULL;

IF COL_LENGTH('dbo.Payments', 'SaleType') IS NULL
    ALTER TABLE dbo.Payments ADD SaleType bit NOT NULL CONSTRAINT DF_Payments_SaleType DEFAULT (1);

IF COL_LENGTH('dbo.Payments', 'RefillExecuted') IS NULL
    ALTER TABLE dbo.Payments ADD RefillExecuted bit NOT NULL CONSTRAINT DF_Payments_RefillExecuted DEFAULT (0);

IF COL_LENGTH('dbo.Payments', 'Purpose') IS NULL
    ALTER TABLE dbo.Payments ADD Purpose nvarchar(100) NULL;
