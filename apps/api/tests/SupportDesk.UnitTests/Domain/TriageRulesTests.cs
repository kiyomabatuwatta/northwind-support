using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Triage;
using Xunit;

namespace SupportDesk.UnitTests.Domain;

public class TriageRulesTests
{
    private static readonly DateTime Now = new(2026, 9, 27, 9, 0, 0, DateTimeKind.Utc);

    private static readonly CategoryInfo Security = new(5, "Security", true);
    private static readonly CategoryInfo Technical = new(3, "Technical", false);

    private readonly SlaOptions _options = new();
    private readonly AgentSelector _selector = new();

    private SlaCalculator Sla => new(_options);

    private static AgentCandidate Agent(int id, int open, int max = 8, bool active = true, params int[] specs)
        => new(id, active, max, open, specs.ToHashSet());

    [Theory]
    [InlineData(TicketPriority.Critical, false, 4)]
    [InlineData(TicketPriority.High, false, 8)]
    [InlineData(TicketPriority.Medium, false, 24)]
    [InlineData(TicketPriority.Low, false, 72)]
    [InlineData(TicketPriority.Critical, true, 2)]
    [InlineData(TicketPriority.Low, true, 36)]
    public void Window_matches_priority_and_customer_tier(TicketPriority priority, bool premium, double hours)
    {
        Assert.Equal(TimeSpan.FromHours(hours), Sla.WindowFor(priority, premium).Duration);
    }

    [Fact]
    public void Premium_window_never_drops_below_the_floor()
    {
        _options.BaseWindowHours[TicketPriority.Critical] = 1;

        Assert.Equal(TimeSpan.FromHours(1), Sla.WindowFor(TicketPriority.Critical, true).Duration);
    }

    [Fact]
    public void Forced_critical_category_overrides_the_requested_priority()
    {
        var decision = PriorityRule.Apply(TicketPriority.Low, true, "Security");

        Assert.Equal(TicketPriority.Critical, decision.Priority);
    }

    [Fact]
    public void Requested_priority_is_respected_and_default_is_medium()
    {
        Assert.Equal(TicketPriority.High, PriorityRule.Apply(TicketPriority.High, false, "Billing").Priority);
        Assert.Equal(TicketPriority.Medium, PriorityRule.Apply(null, false, "Technical").Priority);
    }

    [Fact]
    public void Fewest_open_specialist_wins_and_full_or_non_specialist_agents_are_skipped()
    {
        var agents = new[]
        {
            Agent(3, open: 4, specs: 5),
            Agent(5, open: 6, max: 6, specs: 5),
            Agent(1, open: 0, specs: 3),
        };

        Assert.Equal(3, _selector.Select(agents, Security).AgentId);
    }

    [Fact]
    public void Inactive_agents_are_never_chosen()
    {
        var agents = new[] { Agent(6, open: 0, active: false, specs: 3) };

        Assert.Null(_selector.Select(agents, Technical).AgentId);
    }

    [Fact]
    public void Ties_break_on_lowest_id()
    {
        var agents = new[] { Agent(9, 2), Agent(4, 2), Agent(7, 2) };

        Assert.Equal(4, _selector.Select(agents, Technical).AgentId);
    }

    [Fact]
    public void Nobody_eligible_returns_unassigned_with_a_reason()
    {
        var decision = _selector.Select(Array.Empty<AgentCandidate>(), Security);

        Assert.Null(decision.AgentId);
        Assert.Equal("No available agent specialised in Security", decision.Reason);
    }

    [Fact]
    public void Escalation_steps_up_one_level_and_recomputes_due_from_now()
    {
        var policy = new EscalationPolicy(Sla, _selector);

        var plan = policy.Plan(TicketPriority.High, TicketStatus.InProgress, 1, false, Technical,
            new[] { Agent(1, 2) }, Now);

        Assert.Equal(TicketPriority.Critical, plan.ToPriority);
        Assert.Equal(Now.AddHours(4), plan.ToDueAtUtc);
        Assert.Equal(1, plan.ToAgentId);
    }

    [Fact]
    public void Escalation_reassigns_when_the_owner_is_no_longer_eligible()
    {
        var policy = new EscalationPolicy(Sla, _selector);
        var agents = new[] { Agent(1, 1, specs: 3), Agent(3, 4, specs: 5) };

        var plan = policy.Plan(TicketPriority.Medium, TicketStatus.InProgress, 1, false, Security, agents, Now);

        Assert.Equal(3, plan.ToAgentId);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, TicketStatus.InProgress)]
    [InlineData(TicketPriority.High, TicketStatus.Resolved)]
    [InlineData(TicketPriority.High, TicketStatus.Closed)]
    public void Escalation_is_rejected_for_critical_resolved_or_closed_tickets(
        TicketPriority priority, TicketStatus status)
    {
        var policy = new EscalationPolicy(Sla, _selector);

        Assert.Throws<BusinessRuleViolationException>(() =>
            policy.Plan(priority, status, null, false, Technical, Array.Empty<AgentCandidate>(), Now));
    }
}
