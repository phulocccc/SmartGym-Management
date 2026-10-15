using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Enums;

namespace SmartGymManagement.ViewModels
{
    public class MembershipPurchaseViewModel
    {
        [Required(ErrorMessage = "Member is required.")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Membership package is required.")]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = GymClock.Today;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public bool PaymentConfirmed { get; set; }

        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }
}
