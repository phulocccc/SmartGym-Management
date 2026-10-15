using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Validation;

namespace SmartGymManagement.ViewModels;

public sealed class MemberProfileViewModel
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [StringLength(20), RegularExpression(@"^[0-9+()\- ]{7,20}$")] public string? PhoneNumber { get; set; }
    [NotFutureDate, DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    [StringLength(20)] public string? Gender { get; set; }
    [StringLength(255)] public string? Address { get; set; }
    [RegularExpression(@"^[0-9]{9,15}$")] public string? EmergencyContact { get; set; }
}
