using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Enums;

namespace SmartGymManagement.ViewModels
{
    public class BookingEditViewModel
    {
        public int BookingId { get; set; }

        public string MemberName { get; set; } = string.Empty;

        public string MemberEmail { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public BookingStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }
}