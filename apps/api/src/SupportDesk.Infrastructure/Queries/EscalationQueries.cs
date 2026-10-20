using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Queries;

/// <summary>Escalation history reads: untracked, projected in SQL, agent names joined in.</summary>
public sealed class EscalationQueries(SupportDbContext db) : IEscalationQueries
{
    public async Task<IReadOnlyList<EscalationDto>?> GetForTicketAsync(int ticketId, CancellationToken ct)
    {
        var exists = await db.Set<Ticket>().AsNoTracking().AnyAsync(t => t.Id == ticketId, ct);

        if (!exists)
        {
            return null;
        }

        // Served by IX_TicketEscalations_TicketId_EscalatedAtUtc.
        var rows = await Project(
                db.Set<TicketEscalation>()
                    .AsNoTracking()
                    .Where(e => e.TicketId == ticketId)
                    .OrderByDescending(e => e.EscalatedAtUtc)
                    .ThenByDescending(e => e.Id))
            .ToListAsync(ct);

        return rows.Select(ToDto).ToList();
    }

    public async Task<EscalationDto?> GetAsync(int id, CancellationToken ct)
    {
        var row = await Project(db.Set<TicketEscalation>().AsNoTracking().Where(e => e.Id == id))
            .FirstOrDefaultAsync(ct);

        return row is null ? null : ToDto(row);
    }

    private IQueryable<Row> Project(IQueryable<TicketEscalation> source) =>
        source.Select(e => new Row(
            e.Id,
            e.TicketId,
            e.FromPriority,
            e.ToPriority,
            e.FromAgentId,
            db.Set<Agent>().Where(a => a.Id == e.FromAgentId).Select(a => a.FullName).FirstOrDefault(),
            e.ToAgentId,
            db.Set<Agent>().Where(a => a.Id == e.ToAgentId).Select(a => a.FullName).FirstOrDefault(),
            e.FromDueAtUtc,
            e.ToDueAtUtc,
            e.Reason,
            e.EscalatedBy,
            e.EscalatedAtUtc));

    private static EscalationDto ToDto(Row row) => new(
        row.Id,
        row.TicketId,
        row.FromPriority,
        row.ToPriority,
        ToAgent(row.FromAgentId, row.FromAgentName),
        ToAgent(row.ToAgentId, row.ToAgentName),
        row.FromDueAtUtc,
        row.ToDueAtUtc,
        row.Reason,
        row.EscalatedBy,
        row.EscalatedAtUtc);

    private static AgentSummaryDto? ToAgent(int? id, string? name) =>
        id is { } agentId && name is not null ? new AgentSummaryDto(agentId, name) : null;

    private sealed record Row(
        int Id,
        int TicketId,
        TicketPriority FromPriority,
        TicketPriority ToPriority,
        int? FromAgentId,
        string? FromAgentName,
        int? ToAgentId,
        string? ToAgentName,
        DateTime? FromDueAtUtc,
        DateTime? ToDueAtUtc,
        string Reason,
        string EscalatedBy,
        DateTime EscalatedAtUtc);
}
