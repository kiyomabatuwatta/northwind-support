using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Sla;

public sealed record SlaWindow(TimeSpan Duration, string Reason);

/// <summary>BR-2, BR-3, BR-6. Shared by create and escalate so the rule exists once.</summary>
public sealed class SlaCalculator
{
    private readonly SlaOptions _options;

    public SlaCalculator(SlaOptions options) => _options = options;

    public SlaWindow WindowFor(TicketPriority priority, bool isPremiumCustomer)
    {
        var baseHours = _options.BaseWindowHours[priority];

        if (!isPremiumCustomer)
        {
            return new SlaWindow(
                TimeSpan.FromHours(baseHours),
                $"{priority} base window {baseHours:0.##}h");
        }

        var halved = baseHours * _options.PremiumCustomerMultiplier;
        var hours = Math.Max(halved, _options.MinimumWindowHours);
        var floorApplied = hours > halved;

        return new SlaWindow(
            TimeSpan.FromHours(hours),
            $"{priority} base window {baseHours:0.##}h, halved for a Premium customer"
            + (floorApplied ? $" (minimum {_options.MinimumWindowHours:0.##}h applies)" : string.Empty));
    }

    public DateTime DueAt(DateTime startUtc, TicketPriority priority, bool isPremiumCustomer)
        => startUtc + WindowFor(priority, isPremiumCustomer).Duration;
}
