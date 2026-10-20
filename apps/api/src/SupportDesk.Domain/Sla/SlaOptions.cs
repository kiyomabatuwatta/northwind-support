using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Sla;

/// <summary>Bound from the "Sla" configuration section. Defaults match the business rules.</summary>
public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public Dictionary<TicketPriority, double> BaseWindowHours { get; set; } = new()
    {
        [TicketPriority.Critical] = 4,
        [TicketPriority.High] = 8,
        [TicketPriority.Medium] = 24,
        [TicketPriority.Low] = 72,
    };

    public double PremiumCustomerMultiplier { get; set; } = 0.5;

    public double MinimumWindowHours { get; set; } = 1;

    /// <summary>A ticket is at risk when this percent or less of its window remains.</summary>
    public double AtRiskThresholdPercent { get; set; } = 25;
}
