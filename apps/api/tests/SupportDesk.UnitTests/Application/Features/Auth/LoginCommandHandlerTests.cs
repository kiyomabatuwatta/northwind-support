using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;
using SupportDesk.Infrastructure.Services;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Auth;

public class LoginCommandHandlerTests
{
    private const string Email = "alex.turner@northwind-support.example";
    private const string Password = "Dev-Password-1!";

    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly Mock<IAgentRepository> _agents = new();
    private readonly Mock<ITokenIssuer> _tokens = new();
    private readonly FixedClock _clock = new();

    public LoginCommandHandlerTests()
    {
        _tokens
            .Setup(t => t.Issue(It.IsAny<Agent>()))
            .Returns(() => new IssuedToken("signed-token", _clock.UtcNow.AddHours(1)));
    }

    private LoginCommandHandler CreateHandler() => new(_agents.Object, _hasher, _tokens.Object);

    private Agent GivenAgent(bool active = true)
    {
        var agent = new Agent("Alex Turner", Email, 10, _clock.UtcNow);
        agent.SetPasswordHash(_hasher.Hash(Password));

        if (!active)
        {
            agent.Deactivate();
        }

        _agents
            .Setup(a => a.GetByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        return agent;
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenExpiryAndUser()
    {
        GivenAgent();

        var response = await CreateHandler().HandleAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal("signed-token", response.Token);
        Assert.Equal(_clock.UtcNow.AddHours(1), response.ExpiresAtUtc);
        Assert.Equal("Alex Turner", response.User.FullName);
        Assert.Equal(Email, response.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsRejectedAndIssuesNoToken()
    {
        GivenAgent();

        var failure = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().HandleAsync(new LoginRequest(Email, "wrong"), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.FailureMessage, failure.Message);
        _tokens.Verify(t => t.Issue(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_GivesTheSameMessageAsAWrongPassword()
    {
        GivenAgent();
        _agents
            .Setup(a => a.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agent?)null);

        var unknown = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().HandleAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().HandleAsync(new LoginRequest(Email, "wrong"), CancellationToken.None));

        Assert.Equal(wrongPassword.Message, unknown.Message);
    }

    [Fact]
    public async Task Login_AsAnInactiveAgent_IsRejectedEvenWithTheRightPassword()
    {
        GivenAgent(active: false);

        var failure = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().HandleAsync(new LoginRequest(Email, Password), CancellationToken.None));

        Assert.Equal(LoginCommandHandler.FailureMessage, failure.Message);
        _tokens.Verify(t => t.Issue(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task Login_AsAnAgentWithoutAPassword_IsRejected()
    {
        var agent = new Agent("No Password", Email, 5, _clock.UtcNow);
        _agents
            .Setup(a => a.GetByEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().HandleAsync(new LoginRequest(Email, Password), CancellationToken.None));
    }
}
