using CSharpFunctionalExtensions;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Repositories;

public interface IPhoneVerificationCodeRepository
{
    Task<Guid> Add(PhoneVerificationCode code, CancellationToken ct);
    
    Task<PhoneVerificationCode?> GetLatestActiveByPhone(
        string phone,
        CancellationToken ct);
}