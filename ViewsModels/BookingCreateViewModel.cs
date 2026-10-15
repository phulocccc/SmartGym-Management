using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.ViewModels
{
    public class BookingCreateViewModel
    {
        [Required(ErrorMessage = "Member is required.")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Class is required.")]
        public int ClassId { get; set; }

        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }
}