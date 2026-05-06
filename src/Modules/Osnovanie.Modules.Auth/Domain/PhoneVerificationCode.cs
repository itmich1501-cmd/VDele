using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Domain;

public class PhoneVerificationCode
{
    public Guid Id { get; private set; }

    public string Phone { get; private set; } = null!;

    public string CodeHash { get; private set; } = null!;

    public DateTime ExpiresAtUtc { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public bool IsConfirmed { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private PhoneVerificationCode() { }

    private PhoneVerificationCode(
        string phone,
        string codeHash,
        DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        Phone = phone;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        IsUsed = false;
        IsConfirmed = false;
    }

    public static Result<(PhoneVerificationCode Entity, string Code), Error> Create(
        string phone,
        TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return AuthErrors.PhoneVerificationCode.PhoneRequired();

        if (lifetime <= TimeSpan.Zero)
            return AuthErrors.PhoneVerificationCode.InvalidLifetime();

        var code = GenerateCode();
        var hash = Hash(code);

        var entity = new PhoneVerificationCode(
            phone,
            hash,
            DateTime.UtcNow.Add(lifetime));

        return (entity, code);
    }

    public UnitResult<Error> Verify(string code)
    {
        if (IsUsed)
            return AuthErrors.PhoneVerificationCode.AlreadyUsed();

        if (DateTime.UtcNow > ExpiresAtUtc)
            return AuthErrors.PhoneVerificationCode.Expired();

        var hash = Hash(code);

        if (hash != CodeHash)
            return AuthErrors.PhoneVerificationCode.InvalidCode();

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Confirm(string code)
    {
        if (IsConfirmed)
            return UnitResult.Success<Error>();

        var verifyResult = Verify(code);

        if (verifyResult.IsFailure)
            return verifyResult.Error;

        IsConfirmed = true;
        ConfirmedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsUsed()
    {
        if (!IsConfirmed)
            return AuthErrors.PhoneVerificationCode.NotConfirmed();

        if (IsUsed)
            return AuthErrors.PhoneVerificationCode.AlreadyUsed();

        IsUsed = true;
        UsedAtUtc = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    private static string GenerateCode()
    {
        return RandomNumberGenerator
            .GetInt32(100000, 999999)
            .ToString();
    }

    private static string Hash(string input)
    {
        using var sha256 = SHA256.Create();

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);

        return Convert.ToBase64String(hash);
    }
}