using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Abstractions;

/// <summary>The read side of escalation history.</summary>
public interface IEscalationQueries
{
    /// <summary>A ticket's escalations, newest first. Null when the ticket does not exist.</summary>
    Task<IReadOnlyList<EscalationDto>?> GetForTicketAsync(int ticketId, CancellationToken ct);

    Task<EscalationDto?> GetAsync(int id, CancellationToken ct);
}
