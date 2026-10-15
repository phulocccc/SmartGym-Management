using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class Membership
    {
        [Key]
        public int MembershipId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int PackageId { get; set; }

        [ForeignKey("PackageId")]
        public MembershipPackage? Package { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public MembershipStatus Status { get; set; } = MembershipStatus.Active;

        public DateTime CreatedAt { get; set; } = GymClock.Now;

        public string? PurchasedPackageName { get; set; }
        public decimal? PurchasedPrice { get; set; }
        public int? PurchasedDurationDays { get; set; }

        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
