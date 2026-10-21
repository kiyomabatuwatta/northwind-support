using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

/// <summary>Validates what the client sends; the actor is added later from the token.</summary>
public sealed class EscalateTicketBodyValidator : AbstractValidator<EscalateTicketBody>
{
    public EscalateTicketBodyValidator()
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A reason is required.")
            .Must(reason => reason.Trim().Length >= TicketEscalation.MinReasonLength)
            .WithMessage($"The reason must be at least {TicketEscalation.MinReasonLength} characters.")
            .Must(reason => reason.Trim().Length <= TicketEscalation.MaxReasonLength)
            .WithMessage($"The reason must be at most {TicketEscalation.MaxReasonLength} characters.");
    }
}
