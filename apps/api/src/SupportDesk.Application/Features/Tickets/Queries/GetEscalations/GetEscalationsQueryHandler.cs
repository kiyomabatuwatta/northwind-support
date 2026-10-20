using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;

namespace SupportDesk.Application.Features.Tickets.Queries.GetEscalations;

/// <summary>A ticket's escalation history, newest first.</summary>
public sealed class GetEscalationsQueryHandler(IEscalationQueries escalations)
{
    /// <exception cref="NotFoundException">No ticket has this id.</exception>
    public async Task<IReadOnlyList<EscalationDto>> HandleAsync(int ticketId, CancellationToken ct) =>
        await escalations.GetForTicketAsync(ticketId, ct) ?? throw new NotFoundException("Ticket", ticketId);
}
