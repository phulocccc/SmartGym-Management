using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Enums;

namespace SmartGymManagement.ViewModels
{
    public class AttendanceCheckInViewModel
    {
        [Required(ErrorMessage = "Booking is required.")]
        public int BookingId { get; set; }

        public AttendanceMethod Method { get; set; }
            = AttendanceMethod.Manual;

        [StringLength(2000)]
        public string? QrToken { get; set; }

        [StringLength(
            500,
            ErrorMessage = "Note cannot exceed 500 characters."
        )]
        public string? Note { get; set; }
    }
}
