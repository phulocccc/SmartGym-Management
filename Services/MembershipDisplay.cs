using SmartGymManagement.Enums;
using SmartGymManagement.Models;

namespace SmartGymManagement.Services;

public static class MembershipDisplay
{
    public static string Status(Membership membership) => membership.Status switch
    {
        MembershipStatus.Cancelled => "Đã hủy",
        MembershipStatus.Pending => "Chờ thanh toán",
        MembershipStatus.Expired => "Hết hạn",
        _ when membership.EndDate.Date < GymClock.Today => "Hết hạn",
        _ when membership.StartDate.Date > GymClock.Today => "Chưa bắt đầu",
        _ => "Có hiệu lực"
    };
}
