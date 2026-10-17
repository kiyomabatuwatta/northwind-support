namespace SupportDesk.Domain.Triage;

public sealed record AgentCandidate(
    int AgentId,
    bool IsActive,
    int MaxOpenTickets,
    int OpenTickets,
    IReadOnlySet<int> SpecializationCategoryIds);

public sealed record CategoryInfo(int Id, string Name, bool RequiresSpecialist);

public sealed record AssignmentDecision(int? AgentId, string Reason);

/// <summary>BR-4 and BR-5. Nobody eligible is a normal outcome, not an exception.</summary>
public sealed class AgentSelector
{
    public AssignmentDecision Select(IEnumerable<AgentCandidate> candidates, CategoryInfo category)
    {
        var best = candidates
            .Where(agent => IsEligible(agent, category, ownsTicket: false))
            .OrderBy(agent => agent.OpenTickets)
            .ThenBy(agent => agent.AgentId)
            .FirstOrDefault();

        if (best is not null)
        {
            return new(best.AgentId, category.RequiresSpecialist
                ? $"Fewest open tickets among {category.Name} specialists"
                : "Fewest open tickets among available agents");
        }

        return new(null, category.RequiresSpecialist
            ? $"No available agent specialised in {category.Name}"
            : "No available agent (everyone is inactive or at their ticket limit)");
    }

    /// <summary>
    /// When the agent already owns the ticket, it is counted in their open total,
    /// so it is not held against their limit.
    /// </summary>
    public bool IsEligible(AgentCandidate agent, CategoryInfo category, bool ownsTicket)
    {
        if (!agent.IsActive)
        {
            return false;
        }

        if (category.RequiresSpecialist && !agent.SpecializationCategoryIds.Contains(category.Id))
        {
            return false;
        }

        var open = ownsTicket ? agent.OpenTickets - 1 : agent.OpenTickets;
        return open < agent.MaxOpenTickets;
    }
}
