using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

/// <summary>
/// Runs the real query against an in-memory Sqlite database, so these tests prove that the
/// filters are translated to SQL and that the count and paging describe the filtered set.
/// </summary>
public sealed class TicketQueriesFilterTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly SupportDbContext _db;
    private readonly TicketQueries _queries;

    private Customer _contoso = null!;
    private Customer _fabrikam = null!;
    private Category _general = null!;
    private Category _billing = null!;
    private Agent _alex = null!;

    public TicketQueriesFilterTests()
    {
        _connection.Open();

        var options = new DbContextOptionsBuilder<SupportDbContext>().UseSqlite(_connection).Options;
        _db = new SupportDbContext(options);
        _db.Database.EnsureCreated();

        _queries = new TicketQueries(_db, new FixedClock());

        SeedReferenceData();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Priority_IsAnExactMatch()
    {
        AddTicket("TCK-0001", "Printer jam", _contoso, _general, TicketPriority.High);
        AddTicket("TCK-0002", "Printer offline", _contoso, _general, TicketPriority.Low);
        AddTicket("TCK-0003", "Printer slow", _fabrikam, _general, TicketPriority.High);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(new TicketQuery { Priority = TicketPriority.High }, default);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(TicketPriority.High, item.Priority));
    }

    [Fact]
    public async Task Filters_CombineWithAnd()
    {
        AddTicket("TCK-0001", "Invoice wrong", _contoso, _billing, TicketPriority.High);
        AddTicket("TCK-0002", "Invoice late", _contoso, _general, TicketPriority.High);
        AddTicket("TCK-0003", "Invoice missing", _fabrikam, _billing, TicketPriority.High);
        AddTicket("TCK-0004", "Invoice duplicate", _contoso, _billing, TicketPriority.Low);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(
            new TicketQuery
            {
                CustomerId = _contoso.Id,
                CategoryId = _billing.Id,
                Priority = TicketPriority.High,
                Search = "Invoice"
            },
            default);

        var only = Assert.Single(result.Items);
        Assert.Equal("TCK-0001", only.Reference);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Search_MatchesTheCustomerName()
    {
        AddTicket("TCK-0001", "Cannot log in", _contoso, _general, TicketPriority.Medium);
        AddTicket("TCK-0002", "Cannot log in", _fabrikam, _general, TicketPriority.Medium);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(new TicketQuery { Search = "Fabrik" }, default);

        var only = Assert.Single(result.Items);
        Assert.Equal("TCK-0002", only.Reference);
    }

    [Fact]
    public async Task Search_MatchesTheReference_AndIsTrimmed()
    {
        AddTicket("TCK-0042", "Something", _contoso, _general, TicketPriority.Medium);
        AddTicket("TCK-0043", "Something else", _contoso, _general, TicketPriority.Medium);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(new TicketQuery { Search = "  TCK-0042  " }, default);

        Assert.Equal("TCK-0042", Assert.Single(result.Items).Reference);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankSearch_IsIgnored(string? search)
    {
        AddTicket("TCK-0001", "One", _contoso, _general, TicketPriority.Medium);
        AddTicket("TCK-0002", "Two", _contoso, _general, TicketPriority.Medium);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(new TicketQuery { Search = search }, default);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task UnassignedOnly_ReturnsOnlyTicketsWithoutAnAgent()
    {
        AddTicket("TCK-0001", "Assigned", _contoso, _general, TicketPriority.Medium, _alex);
        AddTicket("TCK-0002", "Waiting", _contoso, _general, TicketPriority.Medium);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(new TicketQuery { UnassignedOnly = true }, default);

        Assert.Equal("TCK-0002", Assert.Single(result.Items).Reference);
    }

    [Fact]
    public async Task AssignedAgent_AndStatus_AreExactMatches()
    {
        AddTicket("TCK-0001", "Mine", _contoso, _general, TicketPriority.Medium, _alex, TicketStatus.InProgress);
        AddTicket("TCK-0002", "Mine too", _contoso, _general, TicketPriority.Medium, _alex);
        AddTicket("TCK-0003", "Nobody's", _contoso, _general, TicketPriority.Medium, status: TicketStatus.InProgress);
        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(
            new TicketQuery { AssignedAgentId = _alex.Id, Status = TicketStatus.InProgress },
            default);

        Assert.Equal("TCK-0001", Assert.Single(result.Items).Reference);
    }

    [Fact]
    public async Task CountAndPaging_DescribeTheFilteredSet_NotTheWholeTable()
    {
        for (var i = 1; i <= 5; i++)
        {
            AddTicket($"TCK-01{i:00}", "Match", _contoso, _general, TicketPriority.High);
        }

        for (var i = 1; i <= 7; i++)
        {
            AddTicket($"TCK-02{i:00}", "Other", _fabrikam, _general, TicketPriority.Low);
        }

        await _db.SaveChangesAsync();

        var result = await _queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High, PageSize = 2, Page = 3 },
            default);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Single(result.Items);
    }

    private void SeedReferenceData()
    {
        var now = FixedClock.DefaultNow;

        _contoso = new Customer("Contoso", "help@contoso.test", null, CustomerTier.Premium, now);
        _fabrikam = new Customer("Fabrikam", "help@fabrikam.test", null, CustomerTier.Standard, now);
        _general = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        _billing = new Category("Billing", requiresSpecialist: true, forcesCriticalPriority: false);
        _alex = new Agent("Alex Turner", "alex@northwind.test", 10, now);

        _db.AddRange(_contoso, _fabrikam, _general, _billing, _alex);
        _db.SaveChanges();
    }

    private void AddTicket(
        string reference,
        string title,
        Customer customer,
        Category category,
        TicketPriority priority,
        Agent? agent = null,
        TicketStatus status = TicketStatus.New)
    {
        var now = FixedClock.DefaultNow;
        var ticket = Ticket.Raise(reference, title, "A description long enough to be realistic.", customer.Id, category.Id, priority, now);

        if (agent is not null)
        {
            ticket.AssignTo(agent.Id, now);
        }

        if (status != TicketStatus.New)
        {
            ticket.ChangeStatus(status, now);
        }

        _db.Add(ticket);
    }
}
