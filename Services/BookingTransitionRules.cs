using SmartGymManagement.Enums;

namespace SmartGymManagement.Services;

public static class BookingTransitionRules
{
    public static string? Validate(BookingStatus current, BookingStatus next, bool hasAttendance,
        DateTime? classEnd, DateTime now)
    {
        if (!Enum.IsDefined(next)) return "Trạng thái không hợp lệ.";
        if (next == current) return null;
        if (current == BookingStatus.Attended || hasAttendance) return "Booking đã điểm danh; không thể sửa trạng thái trực tiếp.";
        if (next == BookingStatus.Attended) return "Vui lòng dùng chức năng Check-in để điểm danh.";
        if (next == BookingStatus.Booked) return null; // Caller must revalidate eligibility/capacity under its lock.
        if (current != BookingStatus.Booked) return "Không thể chuyển trạng thái booking này.";
        if (next == BookingStatus.NoShow && (classEnd == null || classEnd.Value.AddHours(2) >= now))
            return "Chỉ ghi nhận vắng mặt sau khi hết thời gian check-in.";
        return null;
    }
}
