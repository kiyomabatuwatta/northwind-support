using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public sealed record PriorityDecision(TicketPriority Priority, string Reason);

/// <summary>BR-1.</summary>
public static class PriorityRule
{
    public static PriorityDecision Apply(
        TicketPriority? requested,
        bool categoryForcesCritical,
        string categoryName)
    {
        if (categoryForcesCritical)
        {
            return new(TicketPriority.Critical, $"Category '{categoryName}' is always treated as Critical");
        }

        return requested is { } priority
            ? new(priority, $"Priority {priority} requested by the caller")
            : new(TicketPriority.Medium, "No priority requested, defaulted to Medium");
    }
}
