using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = GymClock.Now;

        public bool IsActive { get; set; } = true;

        public Member? Member { get; set; }

        public Trainer? Trainer { get; set; }
    }
}