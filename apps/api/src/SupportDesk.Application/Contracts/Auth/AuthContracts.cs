namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Body of POST /api/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>The signed-in agent, as shown in the UI.</summary>
public sealed record CurrentUserDto(int Id, string FullName, string Email);

/// <summary>Response of a successful login.</summary>
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, CurrentUserDto User);
