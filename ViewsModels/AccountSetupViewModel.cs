using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.ViewModels;

public sealed class AccountSetupViewModel
{
    [Required, StringLength(450)] public string UserId { get; set; } = string.Empty;
    [Required, StringLength(4096)] public string Token { get; set; } = string.Empty;
    [Required, MinLength(10), StringLength(128), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
