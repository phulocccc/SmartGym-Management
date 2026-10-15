using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class Trainer
    {
        [Key]
        public int TrainerId { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        [StringLength(150, ErrorMessage = "Specialization cannot exceed 150 characters.")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [RegularExpression(@"^[A-Za-z0-9._%+-]+@smartgym\.com$", ErrorMessage = "Email must end with @smartgym.com.")]
        public string Email { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Experience years must be between 0 and 100.")]
        public int ExperienceYears { get; set; }

        public TrainerStatus Status { get; set; } = TrainerStatus.Active;

        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
    }
}