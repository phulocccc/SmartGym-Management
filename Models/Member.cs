using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class Member
    {
        [Key]
        public int MemberId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? EmergencyContact { get; set; }

        public DateTime JoinDate { get; set; } = GymClock.Now;

        public MemberStatus Status { get; set; } = MemberStatus.Active;

        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<BMIRecord> BMIRecords { get; set; } = new List<BMIRecord>();

        public ICollection<ChatbotMessage> ChatbotMessages { get; set; } = new List<ChatbotMessage>();
    }
}