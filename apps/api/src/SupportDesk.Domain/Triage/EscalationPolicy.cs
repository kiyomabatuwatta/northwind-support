using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.Sla;

namespace SupportDesk.Domain.Triage;

public sealed record EscalationPlan(
    TicketPriority ToPriority,
    DateTime ToDueAtUtc,
    int? ToAgentId,
    string AssignmentReason);

/// <summary>BR-7. Reuses SlaCalculator and AgentSelector, same as triage on create.</summary>
public sealed class EscalationPolicy
{
    private readonly SlaCalculator _sla;
    private readonly AgentSelector _selector;

    public EscalationPolicy(SlaCalculator sla, AgentSelector selector)
    {
        _sla = sla;
        _selector = selector;
    }

    public EscalationPlan Plan(
        TicketPriority current,
        TicketStatus status,
        int? currentAgentId,
        bool isPremiumCustomer,
        CategoryInfo category,
        IReadOnlyCollection<AgentCandidate> agents,
        DateTime nowUtc)
    {
        if (status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new BusinessRuleViolationException($"A {status} ticket cannot be escalated.");
        }

        if (current == TicketPriority.Critical)
        {
            throw new BusinessRuleViolationException("The ticket is already Critical and cannot be escalated further.");
        }

        var next = current + 1;
        var due = _sla.DueAt(nowUtc, next, isPremiumCustomer);

        var owner = agents.FirstOrDefault(agent => agent.AgentId == currentAgentId);
        if (owner is not null && _selector.IsEligible(owner, category, ownsTicket: true))
        {
            return new(next, due, owner.AgentId, "Current agent is still eligible, so they keep the ticket");
        }

        var decision = _selector.Select(agents.Where(agent => agent.AgentId != currentAgentId), category);
        return new(next, due, decision.AgentId, decision.Reason);
    }
}
