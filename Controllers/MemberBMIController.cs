using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.ViewModels;
using static SmartGymManagement.Services.FitnessCalculationService;
using SmartGymManagement.Services;

namespace SmartGymManagement.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberBMIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberBMIController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound(
                    "Member profile was not found for this account."
                );
            }

            var records = await _context.BMIRecords
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.RecordedAt)
                .ToPagedListAsync(this);

            return View(records);
        }

        public async Task<IActionResult> Create()
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound(
                    "Member profile was not found for this account."
                );
            }

            int? age = CalculateAge(member.DateOfBirth);

            if (age == null)
            {
                TempData["ErrorMessage"] =
                    "Please update your date of birth before using the fitness calculator.";

                return RedirectToAction(nameof(Index));
            }

            if (age < 18)
            {
                TempData["ErrorMessage"] =
                    "Advanced fitness calculations are currently available for members aged 18 and above.";

                return RedirectToAction(nameof(Index));
            }

            if (member.Gender != "Male" &&
                member.Gender != "Female")
            {
                TempData["ErrorMessage"] =
                    "Please set your gender to Male or Female before using this calculator.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Age = age.Value;
            ViewBag.Gender = member.Gender;

            return View(new BMIRecordFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            BMIRecordFormViewModel model)
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound(
                    "Member profile was not found for this account."
                );
            }

            int? age = CalculateAge(member.DateOfBirth);

            if (age == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Date of birth is required."
                );
            }
            else if (age < 18)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Advanced fitness calculations are available for adults aged 18 and above."
                );
            }

            string gender = member.Gender ?? string.Empty;

            ViewBag.Age = age ?? 0;
            ViewBag.Gender = gender;

            if (gender != "Male" && gender != "Female")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Gender must be Male or Female for this calculation."
                );
            }

            if (!Enum.IsDefined(model.ActivityLevel)) ModelState.AddModelError(nameof(model.ActivityLevel), "Mức hoạt động không hợp lệ.");
            if (!Enum.IsDefined(model.FitnessGoal)) ModelState.AddModelError(nameof(model.FitnessGoal), "Mục tiêu không hợp lệ.");
            ValidateBodyMeasurements(model, gender);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            decimal bmi =
                CalculateBMI(model.HeightCm, model.WeightKg);

            BMICategory bmiCategory =
                GetBMICategory(CalculateRawBMI(model.HeightCm, model.WeightKg));

            decimal bmr =
                CalculateBMR(
                    model.HeightCm,
                    model.WeightKg,
                    age!.Value,
                    gender
                );

            decimal tdee =
                CalculateTDEE(
                    bmr,
                    model.ActivityLevel
                );

            decimal targetCalories =
                CalculateTargetCalories(
                    tdee,
                    model.FitnessGoal
                );

            var macros =
                CalculateMacros(
                    targetCalories,
                    model.FitnessGoal
                );

            decimal? idealBodyWeight =
                CalculateIdealBodyWeight(
                    model.HeightCm,
                    gender
                );

            decimal? bodyFatPercentage =
                CalculateBodyFatPercentage(
                    model.HeightCm,
                    gender,
                    model.NeckCm,
                    model.WaistCm,
                    model.HipCm
                );

            if (model.NeckCm.HasValue && model.WaistCm.HasValue && bodyFatPercentage == null)
            {
                ModelState.AddModelError(string.Empty, "Số đo cho kết quả tỷ lệ mỡ không hợp lý. Vui lòng kiểm tra lại.");
                return View(model);
            }
            decimal? leanBodyMass =
                CalculateLeanBodyMass(
                    model.WeightKg,
                    bodyFatPercentage
                );

            decimal? waistToHipRatio =
                CalculateWaistToHipRatio(
                    model.WaistCm,
                    model.HipCm
                );

            decimal? waistToHeightRatio =
                CalculateWaistToHeightRatio(
                    model.WaistCm,
                    model.HeightCm
                );

            decimal estimatedWater =
                CalculateEstimatedWater(
                    model.WeightKg,
                    model.ActivityLevel
                );

            var record = new BMIRecord
            {
                MemberId = member.MemberId,

                HeightCm = model.HeightCm,
                WeightKg = model.WeightKg,

                AgeAtRecord = age.Value,
                GenderAtRecord = gender,

                ActivityLevel = model.ActivityLevel,
                FitnessGoal = model.FitnessGoal,

                NeckCm = model.NeckCm,
                WaistCm = model.WaistCm,
                HipCm = model.HipCm,

                BMIValue = bmi,
                Category = bmiCategory,

                BMR = bmr,
                TDEE = tdee,
                TargetCalories = targetCalories,

                ProteinGrams = macros.Protein,
                CarbohydrateGrams = macros.Carbs,
                FatGrams = macros.Fat,

                IdealBodyWeightKg = idealBodyWeight,

                BodyFatPercentage = bodyFatPercentage,
                LeanBodyMassKg = leanBodyMass,

                WaistToHipRatio = waistToHipRatio,
                WaistToHeightRatio = waistToHeightRatio,

                EstimatedWaterLiters = estimatedWater,

                RecordedAt = GymClock.Now,
                Note = model.Note?.Trim()
            };

            _context.BMIRecords.Add(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Fitness assessment created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = record.BMIRecordId }
            );
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return NotFound(
                    "Member profile was not found for this account."
                );
            }

            var record = await _context.BMIRecords
                .FirstOrDefaultAsync(b =>
                    b.BMIRecordId == id &&
                    b.MemberId == member.MemberId
                );

            if (record == null)
            {
                return NotFound();
            }

            var history = await _context.BMIRecords
                .Where(b => b.MemberId == member.MemberId)
                .OrderBy(b => b.RecordedAt)
                .ToListAsync();

            var model = new FitnessReportViewModel
            {
                CurrentRecord = record,
                History = history
            };

            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();
            var record = await _context.BMIRecords.SingleOrDefaultAsync(b => b.BMIRecordId == id && b.MemberId == member.MemberId);
            return record == null ? NotFound() : View(record);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();
            var record = await _context.BMIRecords.SingleOrDefaultAsync(b => b.BMIRecordId == id && b.MemberId == member.MemberId);
            if (record == null) return NotFound();
            _context.BMIRecords.Remove(record);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa bản đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            return await _context.Members
                .Include(m => m.User)
                .FirstOrDefaultAsync(
                    m => m.UserId == user.Id
                );
        }

        private void ValidateBodyMeasurements(
            BMIRecordFormViewModel model,
            string gender)
        {
            if (gender == "Male" && model.NeckCm != null &&
                model.WaistCm != null &&
                model.WaistCm <= model.NeckCm)
            {
                ModelState.AddModelError(
                    "WaistCm",
                    "Waist measurement must be greater than neck measurement."
                );
            }


        }
    }
}