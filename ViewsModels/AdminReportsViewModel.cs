namespace SmartGymManagement.ViewModels
{
    public class AdminReportsViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal Refunds { get; set; }
        // Summary cards
        public int TotalMembers { get; set; }

        public int ActiveMembers { get; set; }

        public int ActiveMemberships { get; set; }

        public int UpcomingClasses { get; set; }

        public int TotalBookings { get; set; }

        public int TotalAttendances { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal AttendanceRate { get; set; }

        // Chart data
        public List<MonthlyReportItemViewModel> MonthlyData { get; set; }
            = new List<MonthlyReportItemViewModel>();

        public List<TopClassReportItemViewModel> TopClasses { get; set; }
            = new List<TopClassReportItemViewModel>();

        public List<TopPackageReportItemViewModel> TopPackages { get; set; }
            = new List<TopPackageReportItemViewModel>();
    }

    public class MonthlyReportItemViewModel
    {
        public string MonthLabel { get; set; } = string.Empty;

        public decimal Revenue { get; set; }

        public int Bookings { get; set; }

        public int Attendances { get; set; }
    }

    public class TopClassReportItemViewModel
    {
        public string ClassName { get; set; } = string.Empty;

        public int BookingCount { get; set; }
    }

    public class TopPackageReportItemViewModel
    {
        public string PackageName { get; set; } = string.Empty;

        public int PurchaseCount { get; set; }
    }
}
