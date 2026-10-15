using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Enums;
using SmartGymManagement.Validation;

namespace SmartGymManagement.ViewModels
{
    public class MemberFormViewModel
    {
        public int? MemberId { get; set; }

        public string? UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [RegularExpression(@"^[A-Za-z0-9._%+-]+@smartgym\.com$", ErrorMessage = "Email must end with @smartgym.com.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [NotFutureDate]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters.")]
        public string? Gender { get; set; }

        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; }

        [StringLength(15, ErrorMessage = "Emergency contact cannot exceed 15 digits.")]
        [RegularExpression(@"^[0-9]{9,15}$", ErrorMessage = "Emergency contact must contain only numbers and be between 9 and 15 digits.")]
        public string? EmergencyContact { get; set; }

        public MemberStatus Status { get; set; } = MemberStatus.Active;

        public bool IsActive { get; set; } = true;
    }
}