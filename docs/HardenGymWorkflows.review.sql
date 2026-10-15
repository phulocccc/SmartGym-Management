-- REVIEW ONLY: generated offline; not executed against any database.
-- Target must already be at 20260709233645_AddChatbotMessages.
-- Run only after backup, data review and explicit deployment authorization.
SET XACT_ABORT ON;
BEGIN TRY
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260709233645_AddChatbotMessages')
    THROW 51006, 'The expected predecessor migration is not recorded; review the target database.', 1;
IF EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] > N'20260709233645_AddChatbotMessages')
    THROW 51007, 'The database already has later migrations; do not run this script again.', 1;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM ChatbotMessages WHERE MemberId1 IS NOT NULL AND MemberId1 <> MemberId)
    THROW 51001, 'Review conflicting chat member relationships before migration.', 1;
IF EXISTS (SELECT BookingId FROM Attendances GROUP BY BookingId HAVING COUNT(*) > 1)
    THROW 51002, 'Review duplicate attendance records before migration.', 1;
IF EXISTS (SELECT MemberId, ClassId FROM Bookings WHERE Status IN (0, 2) GROUP BY MemberId, ClassId HAVING COUNT(*) > 1)
    THROW 51003, 'Review duplicate reserved bookings before migration.', 1;
IF EXISTS (SELECT InvoiceNumber FROM Invoices GROUP BY InvoiceNumber HAVING COUNT(*) > 1)
    THROW 51004, 'Review duplicate invoice numbers before migration.', 1;
IF EXISTS (SELECT TransactionCode FROM PaymentTransactions WHERE TransactionCode IS NOT NULL GROUP BY TransactionCode HAVING COUNT(*) > 1)
    THROW 51005, 'Review duplicate payment references before migration.', 1;

ALTER TABLE [ChatbotMessages] DROP CONSTRAINT [FK_ChatbotMessages_Members_MemberId1];

DROP INDEX [IX_ChatbotMessages_MemberId] ON [ChatbotMessages];

DROP INDEX [IX_ChatbotMessages_MemberId1] ON [ChatbotMessages];

DROP INDEX [IX_Bookings_ClassId] ON [Bookings];

DROP INDEX [IX_Bookings_MemberId] ON [Bookings];

DROP INDEX [IX_BMIRecords_MemberId] ON [BMIRecords];

DROP INDEX [IX_Attendances_BookingId] ON [Attendances];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChatbotMessages]') AND [c].[name] = N'MemberId1');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [ChatbotMessages] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [ChatbotMessages] DROP COLUMN [MemberId1];

ALTER TABLE [PaymentTransactions] ADD [RefundedAt] datetime2 NULL;

ALTER TABLE [Memberships] ADD [PurchasedDurationDays] int NULL;

ALTER TABLE [Memberships] ADD [PurchasedPackageName] nvarchar(100) NULL;

ALTER TABLE [Memberships] ADD [PurchasedPrice] decimal(18,2) NULL;

CREATE UNIQUE INDEX [IX_PaymentTransactions_TransactionCode] ON [PaymentTransactions] ([TransactionCode]) WHERE [TransactionCode] IS NOT NULL;

CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [Invoices] ([InvoiceNumber]);

CREATE INDEX [IX_ClassSchedules_Status_StartTime] ON [ClassSchedules] ([Status], [StartTime]);

CREATE INDEX [IX_ChatbotMessages_MemberId_CreatedAt] ON [ChatbotMessages] ([MemberId], [CreatedAt]);

CREATE INDEX [IX_Bookings_ClassId_Status] ON [Bookings] ([ClassId], [Status]);

CREATE UNIQUE INDEX [IX_Bookings_MemberId_ClassId] ON [Bookings] ([MemberId], [ClassId]) WHERE [Status] IN (0, 2);

CREATE INDEX [IX_BMIRecords_MemberId_RecordedAt] ON [BMIRecords] ([MemberId], [RecordedAt]);

CREATE UNIQUE INDEX [IX_Attendances_BookingId] ON [Attendances] ([BookingId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261008192434_HardenGymWorkflows', N'10.0.9');

COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
