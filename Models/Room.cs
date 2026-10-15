using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Room name is required.")]
        [StringLength(100, ErrorMessage = "Room name cannot exceed 100 characters.")]
        public string RoomName { get; set; } = string.Empty;

        [Range(1, 30, ErrorMessage = "Room capacity must be between 1 and 30.")]
        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
    }
}