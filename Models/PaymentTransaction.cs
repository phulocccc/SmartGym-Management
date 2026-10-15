using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int PaymentTransactionId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        public int? MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Amount cannot be negative.")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

        public DateTime PaymentDate { get; set; } = GymClock.Now;

        public DateTime? RefundedAt { get; set; }

        [StringLength(100)]
        public string? TransactionCode { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
