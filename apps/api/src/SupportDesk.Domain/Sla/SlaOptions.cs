using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Sla;

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

    public double PremiumMultiplier { get; set; } = 0.5;

    public double MinimumWindowHours { get; set; } = 1;

    public double AtRiskThreshold { get; set; } = 0.25;
}
