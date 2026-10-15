using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGymManagement.Enums;

namespace SmartGymManagement.Models
{
    public class ClassSchedule
    {
        [Key]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Class name is required.")]
        [StringLength(100, ErrorMessage = "Class name cannot exceed 100 characters.")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trainer is required.")]
        public int TrainerId { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer? Trainer { get; set; }

        [Required(ErrorMessage = "Room is required.")]
        public int? RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public DateTime EndTime { get; set; }

        [Range(1, 30, ErrorMessage = "Capacity must be between 1 and 30.")]
        public int Capacity { get; set; }

        public ClassStatus Status { get; set; } = ClassStatus.Scheduled;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}