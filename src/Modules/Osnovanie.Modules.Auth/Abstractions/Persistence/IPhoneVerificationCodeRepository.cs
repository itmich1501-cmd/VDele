using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Abstractions.Persistence;

public interface IPhoneVerificationCodeRepository
{
    Task<Guid> Add(PhoneVerificationCode code, CancellationToken ct);
    
    Task<PhoneVerificationCode?> GetLatestActiveByPhone(
        string phone,
        CancellationToken ct);
}