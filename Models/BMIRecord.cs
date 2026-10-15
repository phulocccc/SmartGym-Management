using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class BMIRecord
    {
        [Key]
        public int BMIRecordId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        // Basic input
        [Required]
        public decimal HeightCm { get; set; }

        [Required]
        public decimal WeightKg { get; set; }

        public int AgeAtRecord { get; set; }

        [StringLength(20)]
        public string GenderAtRecord { get; set; } = string.Empty;

        public ActivityLevel ActivityLevel { get; set; }

        public FitnessGoal FitnessGoal { get; set; }

        // Optional body measurements
        public decimal? NeckCm { get; set; }

        public decimal? WaistCm { get; set; }

        public decimal? HipCm { get; set; }

        // Basic calculated metrics
        public decimal BMIValue { get; set; }

        public BMICategory Category { get; set; }

        public decimal BMR { get; set; }

        public decimal TDEE { get; set; }

        public decimal TargetCalories { get; set; }

        // Macronutrients
        public decimal ProteinGrams { get; set; }

        public decimal CarbohydrateGrams { get; set; }

        public decimal FatGrams { get; set; }

        // Ideal body weight
        public decimal? IdealBodyWeightKg { get; set; }

        // Advanced body composition
        public decimal? BodyFatPercentage { get; set; }

        public decimal? LeanBodyMassKg { get; set; }

        public decimal? WaistToHipRatio { get; set; }

        public decimal? WaistToHeightRatio { get; set; }

        // Hydration estimate
        public decimal EstimatedWaterLiters { get; set; }

        public DateTime RecordedAt { get; set; } = GymClock.Now;

        [StringLength(500)]
        public string? Note { get; set; }
    }
}