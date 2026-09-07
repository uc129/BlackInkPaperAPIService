namespace Infrastructure.Contracts.Services;

public interface IOtpCodeHasher
{
    string GenerateCode();

    /// <summary>Hashes a code for storage, salted with the number it was issued to.</summary>
    string Hash(string phoneE164, string code);

    bool Verify(string phoneE164, string code, string expectedHash);
}
