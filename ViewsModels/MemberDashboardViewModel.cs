namespace SmartGymManagement.ViewModels
{
    public class MemberDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string MemberStatus { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; }

        public bool HasActiveMembership { get; set; }

        public string? PackageName { get; set; }

        public DateTime? MembershipStartDate { get; set; }

        public DateTime? MembershipEndDate { get; set; }

        public int RemainingDays { get; set; }

        public int TotalBookings { get; set; }

        public int UpcomingBookingsCount { get; set; }

        public int AttendedCount { get; set; }

        public int CancelledCount { get; set; }

        public int NoShowCount { get; set; }

        public List<MemberDashboardBookingItemViewModel> UpcomingBookings { get; set; }
            = new List<MemberDashboardBookingItemViewModel>();

        public List<MemberDashboardAttendanceItemViewModel> RecentAttendances { get; set; }
            = new List<MemberDashboardAttendanceItemViewModel>();
    }

    public class MemberDashboardBookingItemViewModel
    {
        public int BookingId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? TrainerName { get; set; }

        public string? RoomName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class MemberDashboardAttendanceItemViewModel
    {
        public int AttendanceId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? TrainerName { get; set; }

        public string? RoomName { get; set; }

        public DateTime CheckInTime { get; set; }

        public string Method { get; set; } = string.Empty;
    }
}