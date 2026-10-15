-- READ ONLY. Review on the intended test copy before applying the new migration.
-- This file was created for review and was not executed during the source-code changes.
SELECT COUNT(*) AS ConflictingChatRelationships
FROM ChatbotMessages
WHERE MemberId1 IS NOT NULL AND MemberId1 <> MemberId;

SELECT COUNT(*) AS DuplicateAttendanceGroups
FROM (SELECT BookingId FROM Attendances GROUP BY BookingId HAVING COUNT(*) > 1) AS DuplicateGroups;

SELECT COUNT(*) AS DuplicateReservedBookingGroups
FROM (SELECT MemberId, ClassId FROM Bookings WHERE Status IN (0, 2)
      GROUP BY MemberId, ClassId HAVING COUNT(*) > 1) AS DuplicateGroups;

SELECT COUNT(*) AS DuplicateInvoiceNumberGroups
FROM (SELECT InvoiceNumber FROM Invoices GROUP BY InvoiceNumber HAVING COUNT(*) > 1) AS DuplicateGroups;

SELECT COUNT(*) AS DuplicateTransactionReferenceGroups
FROM (SELECT TransactionCode FROM PaymentTransactions WHERE TransactionCode IS NOT NULL
      GROUP BY TransactionCode HAVING COUNT(*) > 1) AS DuplicateGroups;

SELECT COUNT(*) AS AttendanceBookingMismatches
FROM Attendances AS A
JOIN Bookings AS B ON B.BookingId = A.BookingId
WHERE A.MemberId <> B.MemberId OR A.ClassId <> B.ClassId OR B.Status <> 2;

SELECT COUNT(*) AS AttendedBookingsWithoutAttendance
FROM Bookings AS B
WHERE B.Status = 2 AND NOT EXISTS (SELECT 1 FROM Attendances AS A WHERE A.BookingId = B.BookingId);
