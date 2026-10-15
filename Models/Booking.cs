using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public ClassSchedule? ClassSchedule { get; set; }

        public DateTime BookingDate { get; set; } = GymClock.Now;

        public BookingStatus Status { get; set; } = BookingStatus.Booked;

        [StringLength(500)]
        public string? Note { get; set; }
    }
}