using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public ClassSchedule? ClassSchedule { get; set; }

        public DateTime CheckInTime { get; set; } = GymClock.Now;

        public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;

        [StringLength(500)]
        public string? Note { get; set; }
    }
}