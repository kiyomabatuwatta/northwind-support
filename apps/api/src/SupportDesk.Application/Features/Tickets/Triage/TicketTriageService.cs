using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Features.Tickets.Triage;

public sealed record TriageDecision(
    TicketPriority Priority,
    string PriorityReason,
    TimeSpan SlaWindow,
    string SlaReason,
    DateTime DueAtUtc,
    int? AgentId,
    string AssignmentReason)
{
    public TriageDto ToDto() => new(Priority, PriorityReason, SlaWindow.TotalHours, SlaReason, AssignmentReason);
}

/// <summary>
/// Decides priority, SLA due date and owner for a new ticket (BR-1 to BR-6) by composing the
/// small domain rules. It holds no rule of its own.
/// </summary>
public sealed class TicketTriageService(ITriageData data, SlaCalculator sla, AgentSelector selector)
{
    /// <exception cref="NotFoundException">The customer or the category does not exist.</exception>
    public async Task<TriageDecision> TriageAsync(
        TicketPriority? requested,
        int customerId,
        int categoryId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var context = await data.GetContextAsync(customerId, categoryId, ct)
            ?? throw new NotFoundException("Category", categoryId);

        var priority = PriorityRule.Apply(requested, context.ForcesCriticalPriority, context.Category.Name);
        var window = sla.WindowFor(priority.Priority, context.IsPremiumCustomer);

        var agents = await data.GetAgentCandidatesAsync(ct);
        var assignment = selector.Select(agents, context.Category);

        return new TriageDecision(
            priority.Priority,
            priority.Reason,
            window.Duration,
            window.Reason,
            nowUtc + window.Duration,
            assignment.AgentId,
            assignment.Reason);
    }
}
