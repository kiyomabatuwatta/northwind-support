using System.Security.Cryptography;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Infrastructure.Services;

/// <summary>
/// PBKDF2 with HMAC-SHA256 from the .NET runtime (Rfc2898DeriveBytes): a random 16-byte salt
/// per password and 600,000 iterations, the OWASP-recommended work factor for this algorithm.
/// The stored value is <c>pbkdf2-sha256$iterations$salt$key</c>, so the work factor can be
/// raised later without breaking existing hashes.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Scheme = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;

    // Checked against when the account has no hash, so a missing account costs the same time.
    private static readonly Lazy<string> TimingDummy = new(() => CreateHash("timing-dummy-not-a-real-password"));

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        return CreateHash(password);
    }

    public bool Verify(string? storedHash, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var hasStoredHash = !string.IsNullOrWhiteSpace(storedHash);
        var parts = (hasStoredHash ? storedHash! : TimingDummy.Value).Split('$');

        if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected) && hasStoredHash;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string CreateHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }
}
