using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;
using Xunit;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

/// <remarks>
/// Runs against SQLite, which cannot run SQL Server's DATEDIFF, so the at-risk and within-SLA
/// filters are not covered here; the evaluator tests and a run against SQL Server cover those.
/// </remarks>
public sealed class TicketQueriesSlaFilterTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FixedClock _clock = new();
    private readonly SupportDbContext _db;
    private int _customerId;
    private int _categoryId;

    public TicketQueriesSlaFilterTests()
    {
        _connection.Open();

        _db = new SupportDbContext(
            new DbContextOptionsBuilder<SupportDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        Seed();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private void Seed()
    {
        var now = _clock.UtcNow;

        var customer = new Customer("Contoso", "support@contoso.example", null, CustomerTier.Standard, now);
        var category = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        _db.AddRange(customer, category, new Agent("Alex Turner", "alex@example.test", 10, now));
        _db.SaveChanges();

        _customerId = customer.Id;
        _categoryId = category.Id;

        Add("TCK-0001", TicketPriority.High, due: now.AddHours(-1), resolved: null);               // open, past due
        Add("TCK-0002", TicketPriority.Medium, due: now.AddHours(-5), resolved: now.AddHours(-2)); // resolved late
        Add("TCK-0003", TicketPriority.Medium, due: now.AddHours(-2), resolved: now.AddHours(-3)); // resolved on time
        Add("TCK-0004", TicketPriority.Medium, due: null, resolved: null);                         // no SLA
        Add("TCK-0005", TicketPriority.Medium, due: now.AddHours(20), resolved: null);             // open, not yet due
        _db.SaveChanges();
    }

    private void Add(string reference, TicketPriority priority, DateTime? due, DateTime? resolved)
    {
        var created = _clock.UtcNow.AddDays(-2);

        var ticket = Ticket.Raise(
            reference, "A title long enough", "A description long enough.", _customerId, _categoryId, priority, created);

        if (due is { } dueAt)
        {
            ticket.ApplyTriage(null, dueAt, created);
        }

        if (resolved is { } resolvedAt)
        {
            ticket.ChangeStatus(TicketStatus.Resolved, resolvedAt);
        }

        _db.Add(ticket);
    }

    private Task<PagedResult<TicketListItemDto>> Run(TicketQuery query) =>
        new TicketQueries(_db, _clock).GetPagedAsync(query.Normalized(), CancellationToken.None);

    [Fact]
    public async Task Breached_returns_open_past_due_and_late_resolved_tickets_only()
    {
        var result = await Run(new TicketQuery { SlaStatus = SlaStatus.Breached });

        Assert.Equal(new[] { "TCK-0001", "TCK-0002" }, result.Items.Select(i => i.Reference).Order().ToArray());
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(SlaStatus.Breached, item.SlaStatus));
    }

    [Fact]
    public async Task Met_returns_only_tickets_resolved_on_time()
    {
        var result = await Run(new TicketQuery { SlaStatus = SlaStatus.Met });

        Assert.Equal("TCK-0003", Assert.Single(result.Items).Reference);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task NotApplicable_returns_tickets_without_a_due_date()
    {
        var result = await Run(new TicketQuery { SlaStatus = SlaStatus.NotApplicable });

        Assert.Equal("TCK-0004", Assert.Single(result.Items).Reference);
    }

    [Fact]
    public async Task Sla_filter_combines_with_other_filters()
    {
        var result = await Run(new TicketQuery { SlaStatus = SlaStatus.Breached, Priority = TicketPriority.High });

        Assert.Equal("TCK-0001", Assert.Single(result.Items).Reference);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Count_and_pages_describe_the_filtered_set()
    {
        var result = await Run(new TicketQuery { SlaStatus = SlaStatus.Breached, PageSize = 1 });

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }
}
