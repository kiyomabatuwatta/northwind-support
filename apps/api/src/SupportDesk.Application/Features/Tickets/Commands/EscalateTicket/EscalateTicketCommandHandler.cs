using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

/// <summary>
/// Raises a ticket one priority level, recomputes its due date from now, re-evaluates its
/// owner and records an immutable history row. The rules live in <see cref="EscalationPolicy"/>.
/// </summary>
public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ITriageData triageData,
    EscalationPolicy policy,
    SlaCalculator sla,
    IUnitOfWork unitOfWork,
    IClock clock,
    IEscalationQueries escalations,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    /// <exception cref="NotFoundException">No ticket has this id.</exception>
    /// <exception cref="BusinessRuleViolationException">
    /// The ticket is already Critical, or is Resolved or Closed.
    /// </exception>
    public async Task<EscalationResultDto> HandleAsync(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);

        var context = await triageData.GetContextAsync(ticket.CustomerId, ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);

        var agents = await triageData.GetAgentCandidatesAsync(ct);

        var now = clock.UtcNow;
        var fromPriority = ticket.Priority;

        var plan = policy.Plan(
            ticket.Priority,
            ticket.Status,
            ticket.AssignedAgentId,
            context.IsPremiumCustomer,
            context.Category,
            agents,
            now);

        var escalation = ticket.Escalate(
            plan.ToPriority, plan.ToDueAtUtc, plan.ToAgentId, request.Reason, request.EscalatedBy, now);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated from {From} to {To} by {EscalatedBy}; agent {AgentId}.",
            ticket.Reference, fromPriority, plan.ToPriority, escalation.EscalatedBy, plan.ToAgentId);

        var detail = await getTicket.HandleAsync(id, ct);
        var window = sla.WindowFor(plan.ToPriority, context.IsPremiumCustomer);

        detail.Triage = new TriageDto(
            plan.ToPriority,
            $"Escalated from {fromPriority} to {plan.ToPriority}",
            window.Duration.TotalHours,
            window.Reason,
            plan.AssignmentReason);

        var recorded = await escalations.GetAsync(escalation.Id, ct)
            ?? throw new InvalidOperationException("The escalation that was just saved could not be read back.");

        return new EscalationResultDto(detail, recorded);
    }
}
