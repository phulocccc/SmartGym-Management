namespace SmartGymManagement.ViewModels
{
    public class AvailableClassViewModel
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? TrainerName { get; set; }

        public string? RoomName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public int Capacity { get; set; }

        public int BookedCount { get; set; }

        public int RemainingSlots { get; set; }

        public bool IsFull { get; set; }

        public bool AlreadyBooked { get; set; }

        public bool HasActiveMembership { get; set; }

        public bool CanBook { get; set; }

        public string CannotBookReason { get; set; } = string.Empty;
    }
}