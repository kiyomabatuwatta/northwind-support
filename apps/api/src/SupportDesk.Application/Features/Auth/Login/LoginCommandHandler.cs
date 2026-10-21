using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Login;

/// <summary>
/// Signs an agent in. Unknown email, wrong password and inactive agent all fail with the same
/// message, so the response never says which accounts exist.
/// </summary>
public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer)
{
    public const string FailureMessage = "Invalid email or password.";

    /// <exception cref="UnauthorizedException">The credentials were not accepted.</exception>
    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var agent = await agents.GetByEmailAsync(request.Email.Trim(), ct);

        // Verify even when there is no agent: the work done is the same either way.
        var passwordMatches = passwordHasher.Verify(agent?.PasswordHash, request.Password);

        if (agent is null || !passwordMatches || !agent.IsActive)
        {
            throw new UnauthorizedException(FailureMessage);
        }

        var issued = tokenIssuer.Issue(agent);

        return new LoginResponse(
            issued.Token,
            issued.ExpiresAtUtc,
            new CurrentUserDto(agent.Id, agent.FullName, agent.Email));
    }
}
