using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Triage;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Queries;

/// <summary>Narrow, untracked reads for the triage rules. Open counts are computed in SQL.</summary>
public sealed class TriageData(SupportDbContext db) : ITriageData
{
    public async Task<TriageContext?> GetContextAsync(int customerId, int categoryId, CancellationToken ct)
    {
        var tier = await db.Set<Customer>()
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => (CustomerTier?)c.Tier)
            .FirstOrDefaultAsync(ct);

        var category = await db.Set<Category>()
            .AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => new { c.Id, c.Name, c.RequiresSpecialist, c.ForcesCriticalPriority })
            .FirstOrDefaultAsync(ct);

        if (tier is null || category is null)
        {
            return null;
        }

        return new TriageContext(
            tier == CustomerTier.Premium,
            category.ForcesCriticalPriority,
            new CategoryInfo(category.Id, category.Name, category.RequiresSpecialist));
    }

    public async Task<IReadOnlyList<AgentCandidate>> GetAgentCandidatesAsync(CancellationToken ct)
    {
        var rows = await db.Set<Agent>()
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                a.IsActive,
                a.MaxOpenTickets,
                OpenTickets = db.Set<Ticket>().Count(t =>
                    t.AssignedAgentId == a.Id &&
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed),
                CategoryIds = a.Specializations.Select(s => s.CategoryId).ToList()
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new AgentCandidate(
                r.Id, r.IsActive, r.MaxOpenTickets, r.OpenTickets, r.CategoryIds.ToHashSet()))
            .ToList();
    }
}
