using SmartGymManagement.Enums;
using SmartGymManagement.Models;

namespace SmartGymManagement.Services;

public static class PaymentReportQueries
{
    // Include both receipts and refunds in the period, once per transaction.
    public static IQueryable<PaymentTransaction> InReportPeriod(this IQueryable<PaymentTransaction> payments,
        ReportPeriod period)
    {
        var start = period.From;
        var until = period.Until;
        return payments.Where(p => (p.PaymentDate >= start && p.PaymentDate < until) ||
            (p.Status == PaymentStatus.Refunded &&
                (p.RefundedAt ?? p.PaymentDate) >= start && (p.RefundedAt ?? p.PaymentDate) < until));
    }
}
