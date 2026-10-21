using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Auth.GetCurrentUser;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Presentation.Auth;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Signs an agent in. 401 with one message for any bad credentials.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<LoginResponse> Login(
        LoginRequest request,
        [FromServices] LoginCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(request, ct);

    /// <summary>The signed-in agent, or 401.</summary>
    [HttpGet("me")]
    public Task<CurrentUserDto> Me([FromServices] GetCurrentUserQueryHandler handler, CancellationToken ct)
    {
        var subject = User.FindFirst(CurrentUser.IdClaim)?.Value;

        if (!int.TryParse(subject, NumberStyles.Integer, CultureInfo.InvariantCulture, out var agentId))
        {
            throw new UnauthorizedException("The session is no longer valid.");
        }

        return handler.HandleAsync(agentId, ct);
    }
}
