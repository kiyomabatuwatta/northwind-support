using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Abstractions;

/// <summary>What the triage rules need to know about a ticket's customer and category.</summary>
public sealed record TriageContext(
    bool IsPremiumCustomer,
    bool ForcesCriticalPriority,
    CategoryInfo Category);

/// <summary>Read-only facts the triage and escalation rules decide from.</summary>
public interface ITriageData
{
    /// <summary>Null when the customer or the category does not exist.</summary>
    Task<TriageContext?> GetContextAsync(int customerId, int categoryId, CancellationToken ct);

    /// <summary>Every agent with their current open-ticket count and specializations.</summary>
    Task<IReadOnlyList<AgentCandidate>> GetAgentCandidatesAsync(CancellationToken ct);
}
