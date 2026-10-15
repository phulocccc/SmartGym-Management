namespace SmartGymManagement.Services;

public sealed record ReportPeriod(DateTime From, DateTime To)
{
    public DateTime Until => To.AddDays(1);
    public static ReportPeriod Create(DateTime? from, DateTime? to)
    {
        var today = GymClock.Today;
        var end = (to ?? today).Date;
        if (end > today || end < new DateTime(1900, 1, 1)) end = today;
        var start = (from ?? new DateTime(end.Year, end.Month, 1).AddMonths(-5)).Date;
        if (start > end) start = end;
        if (start < end.AddYears(-5)) start = end.AddYears(-5);
        return new(start, end);
    }
}
