namespace SupportDesk.Application.Abstractions;

/// <summary>Hashes passwords for storage and checks a password against a stored hash.</summary>
public interface IPasswordHasher
{
    /// <summary>Returns a salted, slow hash that is safe to store. Never reversible.</summary>
    string Hash(string password);

    /// <summary>
    /// True when <paramref name="password"/> matches <paramref name="storedHash"/>. A null or
    /// malformed hash returns false, after doing the same amount of work as a real check so
    /// that "no such account" cannot be told apart from "wrong password" by timing.
    /// </summary>
    bool Verify(string? storedHash, string password);
}
