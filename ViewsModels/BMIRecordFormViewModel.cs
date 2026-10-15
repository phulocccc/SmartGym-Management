using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Enums;

namespace SmartGymManagement.ViewModels
{
    public class BMIRecordFormViewModel
    {
        [Required(ErrorMessage = "Height is required.")]
        [Range(
            typeof(decimal),
            "100",
            "250",
            ErrorMessage = "Height must be between 100 and 250 cm."
        )]
        public decimal HeightCm { get; set; }

        [Required(ErrorMessage = "Weight is required.")]
        [Range(
            typeof(decimal),
            "20",
            "300",
            ErrorMessage = "Weight must be between 20 and 300 kg."
        )]
        public decimal WeightKg { get; set; }

        [Required(ErrorMessage = "Activity level is required.")]
        public ActivityLevel ActivityLevel { get; set; }

        [Required(ErrorMessage = "Fitness goal is required.")]
        public FitnessGoal FitnessGoal { get; set; }

        [Range(
            typeof(decimal),
            "20",
            "100",
            ErrorMessage = "Neck measurement must be between 20 and 100 cm."
        )]
        public decimal? NeckCm { get; set; }

        [Range(
            typeof(decimal),
            "30",
            "250",
            ErrorMessage = "Waist measurement must be between 30 and 250 cm."
        )]
        public decimal? WaistCm { get; set; }

        [Range(
            typeof(decimal),
            "30",
            "250",
            ErrorMessage = "Hip measurement must be between 30 and 250 cm."
        )]
        public decimal? HipCm { get; set; }

        [StringLength(
            500,
            ErrorMessage = "Note cannot exceed 500 characters."
        )]
        public string? Note { get; set; }
    }
}