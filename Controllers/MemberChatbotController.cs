using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.Services;
using SmartGymManagement.ViewModels;

namespace SmartGymManagement.Controllers;

[Authorize(Roles = "Member")]
public class MemberChatbotController(ApplicationDbContext db, UserManager<ApplicationUser> users,
    FitnessAssistantService ai, IConfiguration configuration, ILogger<MemberChatbotController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var member = await GetMemberAsync();
        if (member == null) return NotFound("Không tìm thấy hồ sơ hội viên.");
        var history = await db.ChatbotMessages.AsNoTracking().Where(c => c.MemberId == member.MemberId)
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.ChatbotMessageId).Take(20).ToListAsync();
        history.Reverse();
        ViewBag.AIEnabled = ai.Enabled;
        return View(new MemberChatbotViewModel { History = history });
    }

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("chatbot")]
    public async Task<IActionResult> Ask([FromForm] string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 1000)
            return Json(new { success = false, message = "Nhập câu hỏi từ 1 đến 1000 ký tự." });
        message = message.Trim();
        var member = await GetMemberAsync();
        if (member == null) return Json(new { success = false, message = "Không tìm thấy hồ sơ hội viên." });
        var intent = FitnessIntentClassifier.Classify(message);
        var reply = await BuildDataReplyAsync(member.MemberId, intent);
        var usedAI = false;
        if (reply == null)
        {
            var assessment = await db.BMIRecords.AsNoTracking().Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.RecordedAt).FirstOrDefaultAsync();
            var history = await db.ChatbotMessages.AsNoTracking().Where(c => c.MemberId == member.MemberId)
                .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.ChatbotMessageId).Take(6).ToListAsync();
            history.Reverse();
            // No names, email addresses, account IDs or raw profile are sent to a model.
            var context = assessment == null ? null : new { assessment.RecordedAt, assessment.AgeAtRecord,
                assessment.GenderAtRecord, assessment.HeightCm, assessment.WeightKg, assessment.BMIValue,
                assessment.TDEE, assessment.TargetCalories, assessment.FitnessGoal, assessment.ActivityLevel };
            reply = await ai.ReplyAsync(message, (object?)context ?? new { assessment = "Chưa có đánh giá" },
                history.Select(h => (h.UserMessage, h.BotResponse)), HttpContext.RequestAborted);
            usedAI = reply != null;
            reply ??= intent == FitnessIntent.Knowledge ? LocalFitnessKnowledge.Reply(message) :
                "Tôi có thể giúp xem gói tập, lớp sắp tới, điểm danh, BMI, calories và kiến thức fitness. Hãy hỏi cụ thể một nội dung.";
        }
        try
        {
            var entry = new ChatbotMessage { MemberId = member.MemberId, UserMessage = message,
                BotResponse = reply, CreatedAt = GymClock.Now };
            db.ChatbotMessages.Add(entry);
            await db.SaveChangesAsync();
            var limit = Math.Clamp(configuration.GetValue("Chatbot:HistoryLimit", 500), 20, 2000);
            var obsoleteIds = await db.ChatbotMessages.Where(c => c.MemberId == member.MemberId)
                .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.ChatbotMessageId).Skip(limit)
                .Select(c => c.ChatbotMessageId).ToListAsync();
            if (obsoleteIds.Count > 0) await db.ChatbotMessages.Where(c => c.MemberId == member.MemberId && obsoleteIds.Contains(c.ChatbotMessageId)).ExecuteDeleteAsync();
            return Json(new { success = true, reply, usedAI, createdAt = entry.CreatedAt.ToString("dd/MM/yyyy HH:mm") });
        }
        catch (DbUpdateException)
        {
            logger.LogWarning("Could not save assistant conversation.");
            return Json(new { success = false, message = "Không thể lưu hội thoại. Vui lòng thử lại sau." });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearHistory()
    {
        var member = await GetMemberAsync();
        if (member == null) return NotFound();
        await db.ChatbotMessages.Where(c => c.MemberId == member.MemberId).ExecuteDeleteAsync();
        TempData["SuccessMessage"] = "Đã xóa lịch sử hội thoại.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Member?> GetMemberAsync()
    {
        var user = await users.GetUserAsync(User);
        return user == null ? null : await db.Members.SingleOrDefaultAsync(m => m.UserId == user.Id);
    }

    private async Task<string?> BuildDataReplyAsync(int memberId, FitnessIntent intent)
    {
        if (intent == FitnessIntent.Help)
            return "Tôi hỗ trợ xem thời hạn gói tập, lịch lớp, điểm danh, BMI, BMR/TDEE, calories, macros và nước uống. Tôi cũng giải thích kiến thức tập luyện; không đặt lớp hoặc thanh toán thay bạn.";
        if (intent == FitnessIntent.Membership)
        {
            var today = GymClock.Today;
            var membership = await db.Memberships.AsNoTracking().Include(m => m.Package)
                .Where(m => m.MemberId == memberId && m.Status == MembershipStatus.Active &&
                    m.StartDate.Date <= today && m.EndDate.Date >= today).OrderByDescending(m => m.EndDate).FirstOrDefaultAsync();
            return membership == null ? "Bạn chưa có gói tập đang có hiệu lực." :
                $"Gói {membership.PurchasedPackageName ?? membership.Package?.PackageName} có hiệu lực đến hết ngày {membership.EndDate:dd/MM/yyyy}. Còn {(membership.EndDate.Date - today).Days + 1} ngày sử dụng, tính cả hôm nay.";
        }
        if (intent == FitnessIntent.Bookings)
        {
            var bookings = await db.Bookings.AsNoTracking().Include(b => b.ClassSchedule).ThenInclude(c => c!.Room)
                .Where(b => b.MemberId == memberId && b.Status == BookingStatus.Booked && b.ClassSchedule != null &&
                    b.ClassSchedule.Status == ClassStatus.Scheduled && b.ClassSchedule.StartTime > GymClock.Now)
                .OrderBy(b => b.ClassSchedule!.StartTime).Take(3).ToListAsync();
            return bookings.Count == 0 ? "Bạn chưa có lớp sắp tới." : "Lớp sắp tới của bạn:\n" +
                string.Join("\n", bookings.Select(b => $"- {b.ClassSchedule!.ClassName}: {b.ClassSchedule.StartTime:dd/MM/yyyy HH:mm}, phòng {b.ClassSchedule.Room?.RoomName}."));
        }
        if (intent == FitnessIntent.Attendance)
        {
            var count = await db.Attendances.CountAsync(a => a.MemberId == memberId);
            return $"Bạn có {count} lần điểm danh. Xem các lần gần nhất ở Dashboard.";
        }
        if (intent is not (FitnessIntent.Water or FitnessIntent.Nutrition or FitnessIntent.Metrics)) return null;
        var record = await db.BMIRecords.AsNoTracking().Where(b => b.MemberId == memberId)
            .OrderByDescending(b => b.RecordedAt).FirstOrDefaultAsync();
        if (record == null) return "Bạn chưa có assessment. Hãy tạo báo cáo BMI/fitness trước để xem mục tiêu cá nhân.";
        var date = $"Theo assessment ngày {record.RecordedAt:dd/MM/yyyy}: ";
        if (intent == FitnessIntent.Water) return date + $"lượng nước ước tính {record.EstimatedWaterLiters:N2} lít/ngày. Đây là giá trị tham khảo.";
        if (intent == FitnessIntent.Nutrition) return date +
            $"BMR {record.BMR:N0}, TDEE {record.TDEE:N0}, mục tiêu {record.TargetCalories:N0} kcal/ngày. Protein {record.ProteinGrams:N0} g, carbs {record.CarbohydrateGrams:N0} g, fat {record.FatGrams:N0} g. Đây là ước tính, không phải chỉ định dinh dưỡng.";
        return date + $"cân nặng {record.WeightKg:N1} kg, BMI {record.BMIValue:N2} ({record.Category}). " +
            (record.BodyFatPercentage.HasValue ? $"Mỡ cơ thể ước tính {record.BodyFatPercentage:N2}%." : "Chưa đủ số đo để ước tính mỡ cơ thể.");
    }
}
