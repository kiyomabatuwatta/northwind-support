using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// What the client sends to POST /api/tickets/{id}/escalate. Who is escalating is deliberately
/// not part of it: it comes from the signed-in agent's token.
/// </summary>
public sealed record EscalateTicketBody(string Reason);

/// <summary>The escalation the handler works on: the client's reason plus the actor from the token.</summary>
public sealed record EscalateTicketRequest(string Reason, string EscalatedBy);

/// <summary>One row of a ticket's escalation history, with the before and after values.</summary>
public sealed record EscalationDto(
    int Id,
    int TicketId,
    TicketPriority FromPriority,
    TicketPriority ToPriority,
    AgentSummaryDto? FromAgent,
    AgentSummaryDto? ToAgent,
    DateTime? FromDueAtUtc,
    DateTime? ToDueAtUtc,
    string Reason,
    string EscalatedBy,
    DateTime EscalatedAtUtc);

/// <summary>Response of an escalation: the updated ticket and the history row that was recorded.</summary>
public sealed record EscalationResultDto(TicketDetailDto Ticket, EscalationDto Escalation);
