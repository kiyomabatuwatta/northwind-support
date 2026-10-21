using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Commands.AssignTicket;
using SupportDesk.Application.Features.Tickets.Commands.ChangeTicketStatus;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetEscalations;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Queries.SearchTickets;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Presentation.Auth;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public sealed class TicketsController : ControllerBase
{
    /// <summary>Searches tickets with filters, sorting and paging.</summary>
    [HttpGet]
    public Task<PagedResult<TicketListItemDto>> Search(
        [FromQuery] TicketQuery query,
        [FromServices] SearchTicketsQueryHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(query, ct);

    /// <summary>Returns one ticket.</summary>
    [HttpGet("{id:int}")]
    public Task<TicketDetailDto> Get(int id, [FromServices] GetTicketQueryHandler handler, CancellationToken ct) =>
        handler.HandleAsync(id, ct);

    /// <summary>Raises a new ticket and returns the triage decision with it.</summary>
    [HttpPost]
    public async Task<ActionResult<TicketDetailDto>> Create(
        CreateTicketRequest request,
        [FromServices] RaiseTicketCommandHandler handler,
        CancellationToken ct)
    {
        var created = await handler.HandleAsync(request, ct);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Moves a ticket to another status.</summary>
    [HttpPatch("{id:int}/status")]
    public Task<TicketDetailDto> UpdateStatus(
        int id,
        UpdateTicketStatusRequest request,
        [FromServices] ChangeTicketStatusCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, request, ct);

    /// <summary>Assigns the ticket to an agent, or unassigns it when no agent is given.</summary>
    [HttpPatch("{id:int}/assignment")]
    public Task<TicketDetailDto> Assign(
        int id,
        AssignTicketRequest request,
        [FromServices] AssignTicketCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, request, ct);

    /// <summary>
    /// Escalates a ticket one priority level. 409 when it is already Critical, or Resolved or Closed.
    /// </summary>
    [HttpPost("{id:int}/escalate")]
    public Task<EscalationResultDto> Escalate(
        int id,
        EscalateTicketBody body,
        [FromServices] EscalateTicketCommandHandler handler,
        CancellationToken ct)
    {
        // Who escalated comes from the signed-in agent's token, never from the request body.
        var actor = User.FindFirstValue(CurrentUser.NameClaim) ?? throw new UnauthorizedException("Not signed in.");

        if (actor.Length > TicketEscalation.MaxEscalatedByLength)
        {
            actor = actor[..TicketEscalation.MaxEscalatedByLength];
        }

        return handler.HandleAsync(id, new EscalateTicketRequest(body.Reason, actor), ct);
    }

    /// <summary>A ticket's escalation history, newest first.</summary>
    [HttpGet("{id:int}/escalations")]
    public Task<IReadOnlyList<EscalationDto>> GetEscalations(
        int id,
        [FromServices] GetEscalationsQueryHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(id, ct);
}
