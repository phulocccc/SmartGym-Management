using SmartGymManagement.Enums;

namespace SmartGymManagement.Services;

public static class AttendanceWindowRules
{
    public static bool AllowsAttendance(ClassStatus status) =>
        status is ClassStatus.Scheduled or ClassStatus.Completed;

    public static string? Validate(ClassStatus status, DateTime start, DateTime end, DateTime now)
    {
        if (!AllowsAttendance(status)) return "Selected class does not allow check-in.";
        if (now < start.AddMinutes(-30)) return "Check-in is only allowed within 30 minutes before the class starts.";
        if (now > end.AddHours(2)) return "Check-in time is over for this class.";
        return null;
    }
}
