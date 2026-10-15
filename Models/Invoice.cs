using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        public int? MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Total amount cannot be negative.")]
        public decimal TotalAmount { get; set; }

        public DateTime IssueDate { get; set; } = GymClock.Now;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    }
}