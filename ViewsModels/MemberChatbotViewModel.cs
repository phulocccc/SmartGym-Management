using System.ComponentModel.DataAnnotations;
using SmartGymManagement.Models;

namespace SmartGymManagement.ViewModels
{
    public class MemberChatbotViewModel
    {
        [Required(ErrorMessage = "Please enter a message.")]
        [StringLength(
            1000,
            ErrorMessage = "Message cannot exceed 1000 characters."
        )]
        public string Message { get; set; } = string.Empty;

        public List<ChatbotMessage> History { get; set; }
            = new List<ChatbotMessage>();
    }
}