using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Triage;
using SupportDesk.UnitTests.TestDoubles;
using Xunit;

namespace SupportDesk.UnitTests.Handlers;

public class EscalateTicketCommandHandlerTests
{
    private static readonly CategoryInfo Technical = new(3, "Technical", false);

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ITriageData> _triageData = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEscalationQueries> _escalations = new();
    private readonly Mock<ITicketQueries> _ticketQueries = new();
    private readonly FixedClock _clock = new();

    public EscalateTicketCommandHandlerTests()
    {
        _triageData
            .Setup(d => d.GetContextAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TriageContext(false, false, Technical));

        _triageData
            .Setup(d => d.GetAgentCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentCandidate> { new(1, true, 8, 2, new HashSet<int>()) });

        _ticketQueries
            .Setup(q => q.GetDetailAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketDetailDto());

        _escalations
            .Setup(e => e.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EscalationDto(
                1, 0, TicketPriority.High, TicketPriority.Critical, null, null, null, null,
                "reason", "alex.lead", _clock.UtcNow));
    }

    private EscalateTicketCommandHandler CreateHandler()
    {
        var sla = new SlaCalculator(new SlaOptions());

        return new EscalateTicketCommandHandler(
            _tickets.Object,
            _triageData.Object,
            new EscalationPolicy(sla, new AgentSelector()),
            sla,
            _unitOfWork.Object,
            _clock,
            _escalations.Object,
            new GetTicketQueryHandler(_ticketQueries.Object),
            NullLogger<EscalateTicketCommandHandler>.Instance);
    }

    private Ticket GivenTicket(TicketPriority priority, TicketStatus status)
    {
        var ticket = new TicketBuilder().WithPriority(priority).WithStatus(status).AssignedTo(1).Build();
        ticket.ApplyTriage(1, _clock.UtcNow.AddHours(8), _clock.UtcNow);

        _tickets
            .Setup(t => t.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        return ticket;
    }

    [Fact]
    public async Task Escalating_records_a_history_row_with_the_before_and_after_values()
    {
        var ticket = GivenTicket(TicketPriority.High, TicketStatus.InProgress);
        var previousDue = ticket.DueAtUtc;

        await CreateHandler().HandleAsync(
            1, new EscalateTicketRequest("  The whole site is down  ", "alex.lead"), CancellationToken.None);

        var row = Assert.Single(ticket.Escalations);
        Assert.Equal(TicketPriority.High, row.FromPriority);
        Assert.Equal(TicketPriority.Critical, row.ToPriority);
        Assert.Equal(1, row.FromAgentId);
        Assert.Equal(1, row.ToAgentId);
        Assert.Equal(previousDue, row.FromDueAtUtc);
        Assert.Equal(_clock.UtcNow.AddHours(4), row.ToDueAtUtc);
        Assert.Equal("The whole site is down", row.Reason);
        Assert.Equal("alex.lead", row.EscalatedBy);
        Assert.Equal(_clock.UtcNow, row.EscalatedAtUtc);

        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, TicketStatus.InProgress, "already Critical")]
    [InlineData(TicketPriority.High, TicketStatus.Resolved, "Resolved")]
    [InlineData(TicketPriority.High, TicketStatus.Closed, "Closed")]
    public async Task Escalation_that_is_not_allowed_is_rejected_and_nothing_is_saved(
        TicketPriority priority, TicketStatus status, string expectedInMessage)
    {
        var ticket = GivenTicket(priority, status);

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            CreateHandler().HandleAsync(
                1, new EscalateTicketRequest("A perfectly good reason", "alex.lead"), CancellationToken.None));

        Assert.Contains(expectedInMessage, exception.Message);
        Assert.Empty(ticket.Escalations);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_ticket_is_not_found()
    {
        _tickets
            .Setup(t => t.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().HandleAsync(
                99, new EscalateTicketRequest("A perfectly good reason", "alex.lead"), CancellationToken.None));
    }
}
