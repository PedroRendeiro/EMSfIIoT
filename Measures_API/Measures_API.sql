CREATE TABLE [dbo].[Measure_API] (
    [Id]            BIGINT       IDENTITY (1, 1) NOT NULL,
    [timeStamp]     DATETIME     NOT NULL,
    [MeasureTypeID] INT          NOT NULL,
    [LocationID]    BIGINT       NOT NULL,
    [Value]         REAL         NOT NULL,
    [Unit]          VARCHAR (10) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE TRIGGER trg_UpdateTimeEntryEvent
ON [dbo].[Event]
AFTER INSERT
AS
	UPDATE [dbo].[Event]
	SET StartSubscriptionDate = GETDATE ()
	WHERE Id IN ( SELECT DISTINCT Id FROM Inserted );