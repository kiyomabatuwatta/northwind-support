namespace SupportDesk.Presentation.Auth;

/// <summary>The claim names used in the access token (inbound claim mapping is switched off).</summary>
public static class CurrentUser
{
    public const string IdClaim = "sub";
    public const string NameClaim = "name";
    public const string EmailClaim = "email";
}
