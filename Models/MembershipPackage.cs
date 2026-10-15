using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.Models
{
    public class MembershipPackage
    {
        [Key]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "Package name is required.")]
        [StringLength(100, ErrorMessage = "Package name cannot exceed 100 characters.")]
        public string PackageName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Duration days is required.")]
        [Range(1, 3650, ErrorMessage = "Duration days must be greater than 0.")]
        public int DurationDays { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(typeof(decimal), "1000", "999999999999", ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = GymClock.Now;

        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}