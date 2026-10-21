using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

/// <summary>A signed access token and the moment it stops being valid.</summary>
public sealed record IssuedToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Creates the access token an agent presents on later requests.</summary>
public interface ITokenIssuer
{
    IssuedToken Issue(Agent agent);
}
