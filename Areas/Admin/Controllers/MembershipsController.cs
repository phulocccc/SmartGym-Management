using Microsoft.AspNetCore.Authorization;
using SmartGymManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MembershipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingService _reservations;

        public MembershipsController(ApplicationDbContext context, BookingService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 100)];
            ViewData["StatusType"] = typeof(MembershipStatus);
            var parsedStatus = Enum.TryParse<MembershipStatus>(status, true, out var value) && Enum.IsDefined(value) ? (MembershipStatus?)value : null;
            var memberships = await _context.Memberships
                .Include(m => m.Member)
                    .ThenInclude(member => member!.User)
                .Include(m => m.Package)
                .Where(m => search == null || (m.Member != null && m.Member.User != null && (m.Member.User.FullName.Contains(search) || m.Member.User.Email!.Contains(search))))
                .Where(m => parsedStatus == null || m.Status == parsedStatus)
                .OrderByDescending(m => m.CreatedAt)
                .ToPagedListAsync(this);

            return View(memberships);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membership = await _context.Memberships
                .Include(m => m.Member)
                    .ThenInclude(member => member!.User)
                .Include(m => m.Package)
                .FirstOrDefaultAsync(m => m.MembershipId == id);

            if (membership == null)
            {
                return NotFound();
            }

            var payments = await _context.PaymentTransactions
                .Where(p => p.MembershipId == membership.MembershipId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var invoices = await _context.Invoices
                .Where(i => i.MembershipId == membership.MembershipId)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            ViewBag.Payments = payments;
            ViewBag.Invoices = invoices;

            return View(membership);
        }

        public async Task<IActionResult> Purchase()
        {
            await LoadDropdownsAsync();

            var model = new MembershipPurchaseViewModel
            {
                StartDate = GymClock.Today,
                PaymentMethod = PaymentMethod.Cash
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(MembershipPurchaseViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var member = await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MemberId == model.MemberId);

            var package = await _context.MembershipPackages
                .FirstOrDefaultAsync(p => p.PackageId == model.PackageId);

            if (member == null)
            {
                ModelState.AddModelError("MemberId", "Selected member does not exist.");
            }
            else if (member.Status != MemberStatus.Active || member.User == null || !member.User.IsActive)
            {
                ModelState.AddModelError("MemberId", "Selected member is not active.");
            }

            if (package == null)
            {
                ModelState.AddModelError("PackageId", "Selected membership package does not exist.");
            }
            else if (!package.IsActive)
            {
                ModelState.AddModelError("PackageId", "Selected membership package is not active.");
            }

            if (!Enum.IsDefined(model.PaymentMethod)) ModelState.AddModelError(nameof(model.PaymentMethod), "Phương thức thanh toán không hợp lệ.");
            if (model.PaymentConfirmed && model.PaymentMethod != PaymentMethod.Cash)
                ModelState.AddModelError(nameof(model.PaymentConfirmed), "Thanh toán điện tử cần được đối soát ở trang chi tiết.");
            var paid = model.PaymentMethod == PaymentMethod.Cash && model.PaymentConfirmed;
            var startDate = model.StartDate.Date;

            if (startDate < GymClock.Today || startDate > GymClock.Today.AddYears(5))
            {
                ModelState.AddModelError("StartDate", "Ngày bắt đầu phải từ hôm nay đến tối đa 5 năm tới.");
            }

            DateTime endDate = startDate;

            if (package != null && startDate >= GymClock.Today && startDate <= GymClock.Today.AddYears(5))
            {
                endDate = MembershipRules.EndDate(startDate, package.DurationDays);

                bool hasOverlappingMembership = await _context.Memberships.AnyAsync(m =>
                    m.MemberId == model.MemberId &&
                    (m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Pending) &&
                    startDate <= m.EndDate && endDate >= m.StartDate
                );

                if (hasOverlappingMembership)
                {
                    ModelState.AddModelError(
                        "StartDate",
                        "This member already has an active membership during the selected period. Please choose a later start date."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model.MemberId, model.PackageId);
                return View(model);
            }


            try
            {
                var membership = new Membership
                {
                    MemberId = model.MemberId,
                    PackageId = model.PackageId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = paid ? MembershipStatus.Active : MembershipStatus.Pending,
                    PurchasedPackageName = package!.PackageName,
                    PurchasedPrice = package.Price,
                    PurchasedDurationDays = package.DurationDays,
                    CreatedAt = GymClock.Now
                };

                _context.Memberships.Add(membership);
                await _context.SaveChangesAsync();

                var payment = new PaymentTransaction
                {
                    MemberId = model.MemberId,
                    MembershipId = membership.MembershipId,
                    Amount = package!.Price,
                    PaymentMethod = model.PaymentMethod,
                    Status = paid ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = GymClock.Now,
                    TransactionCode = GenerateTransactionCode(),
                    Note = model.Note?.Trim()
                };

                _context.PaymentTransactions.Add(payment);

                var invoice = new Invoice
                {
                    InvoiceNumber = GenerateInvoiceNumber(model.MemberId),
                    MemberId = model.MemberId,
                    MembershipId = membership.MembershipId,
                    TotalAmount = package.Price,
                    IssueDate = GymClock.Now,
                    Status = paid ? InvoiceStatus.Issued : InvoiceStatus.Draft
                };

                _context.Invoices.Add(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Membership purchase created successfully.";

                return RedirectToAction(nameof(Details), new { id = membership.MembershipId });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(string.Empty, "An error occurred while creating the membership purchase.");

                await LoadDropdownsAsync(model.MemberId, model.PackageId);
                return View(model);
            }
        }

        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membership = await _context.Memberships
                .Include(m => m.Member)
                    .ThenInclude(member => member!.User)
                .Include(m => m.Package)
                .FirstOrDefaultAsync(m => m.MembershipId == id);

            if (membership == null)
            {
                return NotFound();
            }

            return View(membership);
        }

        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == id);

            if (membership == null)
            {
                return NotFound();
            }

            membership.Status = MembershipStatus.Cancelled;

            var invoices = await _context.Invoices
                .Where(i => i.MembershipId == id)
                .ToListAsync();

            foreach (var invoice in invoices)
            {
                invoice.Status = InvoiceStatus.Cancelled;
            }

            var pendingPayments = await _context.PaymentTransactions.Where(p => p.MembershipId == id && p.Status == PaymentStatus.Pending).ToListAsync();
            foreach (var payment in pendingPayments) payment.Status = PaymentStatus.Failed;
            var futureBookings = await _context.Bookings.Include(b => b.ClassSchedule)
                .Where(b => b.MemberId == membership.MemberId && b.Status == BookingStatus.Booked &&
                    b.ClassSchedule != null && b.ClassSchedule.StartTime > GymClock.Now).ToListAsync();
            foreach (var booking in futureBookings)
            {
                var date = booking.ClassSchedule!.StartTime.Date;
                if (!await _context.Memberships.AnyAsync(m => m.MembershipId != id && m.MemberId == membership.MemberId &&
                    m.Status == MembershipStatus.Active && m.StartDate.Date <= date && m.EndDate.Date >= date))
                {
                    booking.Status = BookingStatus.Cancelled;
                    _context.Notifications.Add(new Notification { MemberId = membership.MemberId, Title = "Booking đã hủy",
                        Message = "Booking bị hủy vì gói tập không còn hiệu lực. Vui lòng liên hệ quản trị viên." });
                }
            }
            _context.Memberships.Update(membership);
            _context.Invoices.UpdateRange(invoices);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id, string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
            {
                TempData["ErrorMessage"] = "Nhập mã đối soát hoặc biên nhận hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var membership = await _context.Memberships.Include(m => m.Member).ThenInclude(m => m!.User).SingleOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null) return NotFound();
            if (membership.Status != MembershipStatus.Pending || membership.Member?.User?.IsActive != true || membership.Member.Status != MemberStatus.Active)
                return BadRequest("Gói không còn chờ thanh toán hoặc tài khoản không hoạt động.");
            var payments = await _context.PaymentTransactions.Where(p => p.MembershipId == id && p.Status == PaymentStatus.Pending).ToListAsync();
            if (payments.Count != 1) return BadRequest("Cần kiểm tra lại giao dịch trước khi xác nhận.");
            payments[0].Status = PaymentStatus.Paid;
            payments[0].PaymentDate = GymClock.Now;
            var note = (payments[0].Note + " | Đã đối soát: " + reference.Trim());
            payments[0].Note = note.Length > 500 ? note[..500] : note;
            membership.Status = membership.EndDate.Date < GymClock.Today ? MembershipStatus.Expired : MembershipStatus.Active;
            var invoices = await _context.Invoices.Where(i => i.MembershipId == id && i.Status == InvoiceStatus.Draft).ToListAsync();
            foreach (var invoice in invoices) { invoice.Status = InvoiceStatus.Issued; invoice.IssueDate = GymClock.Now; }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã xác nhận thanh toán thủ công sau đối soát.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRefund(int id, string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100) return BadRequest("Cần mã biên nhận hoàn tiền.");
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _reservations.LockAsync();
            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null) return NotFound();
            if (membership.Status != MembershipStatus.Cancelled) return BadRequest("Chỉ hoàn tiền cho gói đã hủy.");
            var payments = await _context.PaymentTransactions.Where(p => p.MembershipId == id && p.Status == PaymentStatus.Paid).ToListAsync();
            if (payments.Count == 0) return BadRequest("Không có giao dịch đã thu để hoàn tiền.");
            foreach (var payment in payments)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = GymClock.Now;
                payment.Note = "Đã xác nhận hoàn toàn bộ tiền: " + reference.Trim();
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã ghi nhận hoàn tiền. Thao tác này không tự chuyển tiền qua ngân hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task LoadDropdownsAsync(int? selectedMemberId = null, int? selectedPackageId = null)
        {
            var members = await _context.Members
                .Include(m => m.User)
                .Where(m =>
                    m.Status == MemberStatus.Active &&
                    m.User != null &&
                    m.User.IsActive
                )
                .OrderBy(m => m.User!.FullName)
                .ToListAsync();

            var memberItems = members.Select(m => new
            {
                m.MemberId,
                DisplayName = $"{m.User?.FullName} - {m.User?.Email}"
            }).ToList();

            ViewBag.MemberId = new SelectList(
                memberItems,
                "MemberId",
                "DisplayName",
                selectedMemberId
            );

            var packages = await _context.MembershipPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.PackageName)
                .ToListAsync();

            var packageItems = packages.Select(p => new
            {
                p.PackageId,
                DisplayName = $"{p.PackageName} - {p.DurationDays} days - {p.Price:N0} VND"
            }).ToList();

            ViewBag.PackageId = new SelectList(
                packageItems,
                "PackageId",
                "DisplayName",
                selectedPackageId
            );
        }

        private string GenerateTransactionCode()
        {
            return $"TXN-{Guid.NewGuid():N}";
        }

        private string GenerateInvoiceNumber(int memberId)
        {
            return $"INV-{Guid.NewGuid():N}";
        }
    }
}