using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGymManagement.Models
{
    public class ChatbotMessage
    {
        [Key]
        public int ChatbotMessageId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public Member? Member { get; set; }

        [Required]
        [StringLength(
            1000,
            ErrorMessage = "Message cannot exceed 1000 characters."
        )]
        public string UserMessage { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string BotResponse { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = GymClock.Now;
    }
}