using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.ViewModels;
using ClosedXML.Excel;
using SmartGymManagement.Services;

namespace SmartGymManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportService _reports;
        private readonly MembershipMaintenanceService _maintenance;
        private ReportPeriod _period = ReportPeriod.Create(null, null);

        public ReportsController(ApplicationDbContext context, ReportService reports, MembershipMaintenanceService maintenance)
        {
            _context = context;
            _reports = reports;
            _maintenance = maintenance;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            return View(await _reports.BuildAsync(ReportPeriod.Create(from, to)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatuses()
        {
            await _maintenance.UpdateStatusesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật gói hết hạn và booking vắng mặt.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportAllReports(DateTime? from, DateTime? to)
        {
            _period = ReportPeriod.Create(from, to);
            var rowCount = await _context.Members.CountAsync(m => m.JoinDate >= _period.From && m.JoinDate < _period.Until)
                + await _context.Bookings.CountAsync(b => b.BookingDate >= _period.From && b.BookingDate < _period.Until)
                + await _context.Attendances.CountAsync(a => a.CheckInTime >= _period.From && a.CheckInTime < _period.Until)
                + await _context.PaymentTransactions.InReportPeriod(_period).CountAsync()
                + await _context.Memberships.CountAsync(m => m.CreatedAt >= _period.From && m.CreatedAt < _period.Until);
            if (rowCount > 50000) return BadRequest("Dữ liệu quá lớn. Vui lòng chọn khoảng ngày ngắn hơn.");
            using var workbook = new XLWorkbook();
            await CreateSummarySheetAsync(workbook);

            await CreateMembersSheetAsync(workbook);
            await CreateMembershipsSheetAsync(workbook);
            await CreateBookingsSheetAsync(workbook);
            await CreateAttendancesSheetAsync(workbook);
            await CreatePaymentsSheetAsync(workbook);

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            string fileName =
                $"SmartGym_Report_{GymClock.Now:yyyy-MM-dd_HHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        private async Task CreateMembersSheetAsync(
    XLWorkbook workbook)
        {
            var members = await _context.Members
                .Where(m => m.JoinDate >= _period.From && m.JoinDate < _period.Until)
                .AsNoTracking()
                .Include(m => m.User)
                .OrderBy(m => m.MemberId)
                .ToListAsync();

            var worksheet =
                workbook.Worksheets.Add("Members");

            string[] headers =
            {
        "Member ID",
        "Full Name",
        "Email",
        "Phone Number",
        "Date of Birth",
        "Gender",
        "Address",
        "Emergency Contact",
        "Join Date",
        "Member Status",
        "Account Active"
    };

            for (int column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(1, column + 1)
                    .Value = headers[column];
            }

            int row = 2;

            foreach (var member in members)
            {
                worksheet.Cell(row, 1)
                    .Value = member.MemberId;

                worksheet.Cell(row, 2)
                    .Value = member.User?.FullName ?? "";

                worksheet.Cell(row, 3)
                    .Value = member.User?.Email ?? "";

                worksheet.Cell(row, 4)
                    .Value = member.User?.PhoneNumber ?? "";

                if (member.DateOfBirth.HasValue)
                {
                    worksheet.Cell(row, 5)
                        .Value = member.DateOfBirth.Value;

                    worksheet.Cell(row, 5)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy";
                }

                worksheet.Cell(row, 6)
                    .Value = member.Gender ?? "";

                worksheet.Cell(row, 7)
                    .Value = member.Address ?? "";

                worksheet.Cell(row, 8)
                    .Value = member.EmergencyContact ?? "";

                worksheet.Cell(row, 9)
                    .Value = member.JoinDate;

                worksheet.Cell(row, 9)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy";

                worksheet.Cell(row, 10)
                    .Value = member.Status.ToString();

                worksheet.Cell(row, 11)
                    .Value = member.User?.IsActive ?? false;

                row++;
            }

            StyleWorksheet(
                worksheet,
                headers.Length,
                row - 1
            );
        }

        private async Task CreateMembershipsSheetAsync(
    XLWorkbook workbook)
        {
            var memberships =
                await _context.Memberships
                .Where(m => m.CreatedAt >= _period.From && m.CreatedAt < _period.Until)
                    .AsNoTracking()
                    .Include(m => m.Member)
                        .ThenInclude(m => m!.User)
                    .Include(m => m.Package)
                    .Include(m => m.PaymentTransactions)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

            var worksheet =
                workbook.Worksheets.Add("Memberships");

            string[] headers =
            {
        "Membership ID",
        "Member",
        "Email",
        "Package",
        "Start Date",
        "End Date",
        "Duration Days",
        "Price",
        "Status",
        "Created At"
    };

            for (int column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(1, column + 1)
                    .Value = headers[column];
            }

            int row = 2;

            foreach (var membership in memberships)
            {
                worksheet.Cell(row, 1)
                    .Value = membership.MembershipId;

                worksheet.Cell(row, 2)
                    .Value =
                        membership.Member?.User?.FullName
                        ?? "";

                worksheet.Cell(row, 3)
                    .Value =
                        membership.Member?.User?.Email
                        ?? "";

                worksheet.Cell(row, 4)
                    .Value =
                        membership.PurchasedPackageName ?? membership.Package?.PackageName
                        ?? "";

                worksheet.Cell(row, 5)
                    .Value = membership.StartDate;

                worksheet.Cell(row, 5)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy";

                worksheet.Cell(row, 6)
                    .Value = membership.EndDate;

                worksheet.Cell(row, 6)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy";

                worksheet.Cell(row, 7)
                    .Value =
                        membership.PurchasedDurationDays ?? (int?)(membership.EndDate.Date - membership.StartDate.Date).Days + 1
                        ?? 0;

                worksheet.Cell(row, 8)
                    .Value =
                        membership.PurchasedPrice ?? membership.PaymentTransactions.Where(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Refunded).Select(p => (decimal?)p.Amount).Sum()
                        ?? 0;

                worksheet.Cell(row, 8)
                    .Style.NumberFormat.Format =
                        "#,##0";

                worksheet.Cell(row, 9)
                    .Value = membership.Status.ToString();

                worksheet.Cell(row, 10)
                    .Value = membership.CreatedAt;

                worksheet.Cell(row, 10)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy HH:mm";

                row++;
            }

            StyleWorksheet(
                worksheet,
                headers.Length,
                row - 1
            );
        }

        private async Task CreateBookingsSheetAsync(
    XLWorkbook workbook)
        {
            var bookings =
                await _context.Bookings
                .Where(b => b.BookingDate >= _period.From && b.BookingDate < _period.Until)
                    .AsNoTracking()
                    .Include(b => b.Member)
                        .ThenInclude(m => m!.User)
                    .Include(b => b.ClassSchedule)
                        .ThenInclude(c => c!.Trainer)
                    .Include(b => b.ClassSchedule)
                        .ThenInclude(c => c!.Room)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

            var worksheet =
                workbook.Worksheets.Add("Bookings");

            string[] headers =
            {
        "Booking ID",
        "Member",
        "Email",
        "Class",
        "Trainer",
        "Room",
        "Class Start",
        "Class End",
        "Booking Date",
        "Status",
        "Note"
    };

            for (int column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(1, column + 1)
                    .Value = headers[column];
            }

            int row = 2;

            foreach (var booking in bookings)
            {
                worksheet.Cell(row, 1)
                    .Value = booking.BookingId;

                worksheet.Cell(row, 2)
                    .Value =
                        booking.Member?.User?.FullName
                        ?? "";

                worksheet.Cell(row, 3)
                    .Value =
                        booking.Member?.User?.Email
                        ?? "";

                worksheet.Cell(row, 4)
                    .Value =
                        booking.ClassSchedule?.ClassName
                        ?? "";

                worksheet.Cell(row, 5)
                    .Value =
                        booking.ClassSchedule?.Trainer?.FullName
                        ?? "";

                worksheet.Cell(row, 6)
                    .Value =
                        booking.ClassSchedule?.Room?.RoomName
                        ?? "";

                if (booking.ClassSchedule != null)
                {
                    worksheet.Cell(row, 7)
                        .Value =
                            booking.ClassSchedule.StartTime;

                    worksheet.Cell(row, 7)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy HH:mm";

                    worksheet.Cell(row, 8)
                        .Value =
                            booking.ClassSchedule.EndTime;

                    worksheet.Cell(row, 8)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy HH:mm";
                }

                worksheet.Cell(row, 9)
                    .Value = booking.BookingDate;

                worksheet.Cell(row, 9)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy HH:mm";

                worksheet.Cell(row, 10)
                    .Value = booking.Status.ToString();

                worksheet.Cell(row, 11)
                    .Value = booking.Note ?? "";

                row++;
            }

            StyleWorksheet(
                worksheet,
                headers.Length,
                row - 1
            );
        }

        private async Task CreateAttendancesSheetAsync(
    XLWorkbook workbook)
        {
            var attendances =
                await _context.Attendances
                .Where(a => a.CheckInTime >= _period.From && a.CheckInTime < _period.Until)
                    .AsNoTracking()
                    .Include(a => a.Member)
                        .ThenInclude(m => m!.User)
                    .Include(a => a.Booking)
                    .Include(a => a.ClassSchedule)
                        .ThenInclude(c => c!.Trainer)
                    .Include(a => a.ClassSchedule)
                        .ThenInclude(c => c!.Room)
                    .OrderByDescending(a => a.CheckInTime)
                    .ToListAsync();

            var worksheet =
                workbook.Worksheets.Add("Attendances");

            string[] headers =
            {
        "Attendance ID",
        "Member",
        "Email",
        "Booking ID",
        "Class",
        "Trainer",
        "Room",
        "Class Start",
        "Check-in Time",
        "Method",
        "Booking Status",
        "Note"
    };

            for (int column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(1, column + 1)
                    .Value = headers[column];
            }

            int row = 2;

            foreach (var attendance in attendances)
            {
                worksheet.Cell(row, 1)
                    .Value = attendance.AttendanceId;

                worksheet.Cell(row, 2)
                    .Value =
                        attendance.Member?.User?.FullName
                        ?? "";

                worksheet.Cell(row, 3)
                    .Value =
                        attendance.Member?.User?.Email
                        ?? "";

                worksheet.Cell(row, 4)
                    .Value = attendance.BookingId;

                worksheet.Cell(row, 5)
                    .Value =
                        attendance.ClassSchedule?.ClassName
                        ?? "";

                worksheet.Cell(row, 6)
                    .Value =
                        attendance.ClassSchedule?.Trainer?.FullName
                        ?? "";

                worksheet.Cell(row, 7)
                    .Value =
                        attendance.ClassSchedule?.Room?.RoomName
                        ?? "";

                if (attendance.ClassSchedule != null)
                {
                    worksheet.Cell(row, 8)
                        .Value =
                            attendance.ClassSchedule.StartTime;

                    worksheet.Cell(row, 8)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy HH:mm";
                }

                worksheet.Cell(row, 9)
                    .Value = attendance.CheckInTime;

                worksheet.Cell(row, 9)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy HH:mm";

                worksheet.Cell(row, 10)
                    .Value = attendance.Method.ToString();

                worksheet.Cell(row, 11)
                    .Value =
                        attendance.Booking?.Status.ToString()
                        ?? "";

                worksheet.Cell(row, 12)
                    .Value = attendance.Note ?? "";

                row++;
            }

            StyleWorksheet(
                worksheet,
                headers.Length,
                row - 1
            );
        }

        private async Task CreatePaymentsSheetAsync(
    XLWorkbook workbook)
        {
            var payments =
                await _context.PaymentTransactions
                .InReportPeriod(_period)
                    .AsNoTracking()
                    .Include(p => p.Member)
                        .ThenInclude(m => m!.User)
                    .Include(p => p.Membership)
                        .ThenInclude(m => m!.Package)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

            var worksheet =
                workbook.Worksheets.Add("Payments");

            string[] headers =
            {
        "Payment ID",
        "Transaction Code",
        "Member",
        "Email",
        "Package",
        "Amount",
        "Payment Method",
        "Payment Status",
        "Payment Date",
        "Note",
        "Refunded At",
        "Received In Period",
        "Refunded In Period"
    };

            for (int column = 0;
                 column < headers.Length;
                 column++)
            {
                worksheet.Cell(1, column + 1)
                    .Value = headers[column];
            }

            int row = 2;

            foreach (var payment in payments)
            {
                worksheet.Cell(row, 1)
                    .Value =
                        payment.PaymentTransactionId;

                worksheet.Cell(row, 2)
                    .Value =
                        payment.TransactionCode ?? "";

                worksheet.Cell(row, 3)
                    .Value =
                        payment.Member?.User?.FullName
                        ?? "";

                worksheet.Cell(row, 4)
                    .Value =
                        payment.Member?.User?.Email
                        ?? "";

                worksheet.Cell(row, 5)
                    .Value =
                        payment.Membership?.PurchasedPackageName ?? payment.Membership?.Package?.PackageName
                        ?? "";

                worksheet.Cell(row, 6)
                    .Value = payment.Amount;

                worksheet.Cell(row, 6)
                    .Style.NumberFormat.Format =
                        "#,##0";

                worksheet.Cell(row, 7)
                    .Value =
                        payment.PaymentMethod.ToString();

                worksheet.Cell(row, 8)
                    .Value =
                        payment.Status.ToString();

                worksheet.Cell(row, 9)
                    .Value =
                        payment.PaymentDate;

                worksheet.Cell(row, 9)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy HH:mm";

                worksheet.Cell(row, 10)
                    .Value =
                        payment.Note ?? "";

                if (payment.RefundedAt.HasValue)
                {
                    worksheet.Cell(row, 11).Value = payment.RefundedAt.Value;
                    worksheet.Cell(row, 11).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }
                worksheet.Cell(row, 12).Value =
                    (payment.Status == PaymentStatus.Paid || payment.Status == PaymentStatus.Refunded) &&
                    payment.PaymentDate >= _period.From && payment.PaymentDate < _period.Until ? payment.Amount : 0m;
                var refundDate = payment.RefundedAt ?? payment.PaymentDate;
                worksheet.Cell(row, 13).Value = payment.Status == PaymentStatus.Refunded &&
                    refundDate >= _period.From && refundDate < _period.Until ? payment.Amount : 0m;
                worksheet.Cell(row, 12).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0";

                row++;
            }

            StyleWorksheet(
                worksheet,
                headers.Length,
                row - 1
            );
        }

        private async Task CreateSummarySheetAsync(XLWorkbook workbook)
        {
            var report = await _reports.BuildAsync(_period);
            var sheet = workbook.Worksheets.Add("Summary");
            sheet.Cell(1, 1).Value = "Kỳ báo cáo";
            sheet.Cell(1, 2).Value = $"{_period.From:dd/MM/yyyy} - {_period.To:dd/MM/yyyy}";
            var values = new (string Name, decimal Value)[] { ("Doanh thu thu vào", report.GrossRevenue),
                ("Hoàn tiền", report.Refunds), ("Doanh thu ròng", report.TotalRevenue),
                ("Booking", report.TotalBookings), ("Điểm danh", report.TotalAttendances), ("Tỷ lệ tham gia (%)", report.AttendanceRate) };
            for (var i = 0; i < values.Length; i++) { sheet.Cell(i + 2, 1).Value = values[i].Name; sheet.Cell(i + 2, 2).Value = values[i].Value; }
            sheet.Columns().AdjustToContents();
        }

        private void StyleWorksheet(
    IXLWorksheet worksheet,
    int totalColumns,
    int lastRow)
        {
            var headerRange =
                worksheet.Range(
                    1,
                    1,
                    1,
                    totalColumns
                );

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Fill.BackgroundColor =
                XLColor.LightGray;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            worksheet.Row(1).Height = 24;

            if (lastRow >= 1)
            {
                var usedRange =
                    worksheet.Range(
                        1,
                        1,
                        Math.Max(lastRow, 1),
                        totalColumns
                    );

                usedRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                usedRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Hair;
            }

            worksheet.SheetView.FreezeRows(1);

            worksheet.Columns().AdjustToContents();

            foreach (var column in worksheet.ColumnsUsed())
            {
                if (column.Width > 40)
                {
                    column.Width = 40;
                }
            }
        }

    }
}
