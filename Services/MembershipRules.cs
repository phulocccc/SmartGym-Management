namespace SmartGymManagement.Services;

public static class MembershipRules
{
    public static DateTime EndDate(DateTime start, int durationDays)
    {
        if (durationDays < 1 || durationDays > 3650) throw new ArgumentOutOfRangeException(nameof(durationDays));
        return start.Date.AddDays(durationDays - 1);
    }
    public static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd)
        => start.Date <= otherEnd.Date && end.Date >= otherStart.Date;
}
