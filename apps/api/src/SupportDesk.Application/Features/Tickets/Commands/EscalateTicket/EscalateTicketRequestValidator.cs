using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public EscalateTicketRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A reason is required.")
            .Must(reason => reason.Trim().Length >= TicketEscalation.MinReasonLength)
            .WithMessage($"The reason must be at least {TicketEscalation.MinReasonLength} characters.")
            .Must(reason => reason.Trim().Length <= TicketEscalation.MaxReasonLength)
            .WithMessage($"The reason must be at most {TicketEscalation.MaxReasonLength} characters.");

        RuleFor(x => x.EscalatedBy)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Who is escalating is required.")
            .Must(name => name.Trim().Length <= TicketEscalation.MaxEscalatedByLength)
            .WithMessage($"Must be at most {TicketEscalation.MaxEscalatedByLength} characters.");
    }
}
