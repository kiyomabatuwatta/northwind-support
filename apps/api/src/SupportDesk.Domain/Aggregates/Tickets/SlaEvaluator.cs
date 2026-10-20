namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    /// <summary>Without a window start there is no at-risk state.</summary>
    public static SlaStatus Evaluate(DateTime? dueAtUtc, DateTime? resolvedAtUtc, DateTime nowUtc) =>
        Evaluate(dueAtUtc, resolvedAtUtc, nowUtc, windowStartUtc: null, atRiskThresholdPercent: 0);

    /// <param name="windowStartUtc">When the current SLA window began.</param>
    /// <param name="atRiskThresholdPercent">At risk when this percent or less of the window remains.</param>
    public static SlaStatus Evaluate(
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime nowUtc,
        DateTime? windowStartUtc,
        double atRiskThresholdPercent)
    {
        if (dueAtUtc is not { } due)
        {
            return SlaStatus.NotApplicable;
        }

        if (resolvedAtUtc is { } resolved)
        {
            return resolved <= due ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > due)
        {
            return SlaStatus.Breached;
        }

        return IsAtRisk(due, windowStartUtc, nowUtc, atRiskThresholdPercent)
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }

    private static bool IsAtRisk(DateTime due, DateTime? windowStartUtc, DateTime nowUtc, double thresholdPercent)
    {
        if (windowStartUtc is not { } start || thresholdPercent <= 0)
        {
            return false;
        }

        var window = due - start;
        var remaining = due - nowUtc;

        return (double)remaining.Ticks * 100 <= (double)window.Ticks * thresholdPercent;
    }
}
