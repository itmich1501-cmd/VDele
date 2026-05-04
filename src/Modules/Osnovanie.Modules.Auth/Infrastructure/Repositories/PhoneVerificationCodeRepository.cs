using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Infrastructure.Repositories;

public class PhoneVerificationCodeRepository : IPhoneVerificationCodeRepository
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<PhoneVerificationCodeRepository> _logger;

    public PhoneVerificationCodeRepository(AuthDbContext dbContext, ILogger<PhoneVerificationCodeRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<Guid> Add(PhoneVerificationCode phoneVerificationCode, CancellationToken cancellationToken)
    {
        await _dbContext.PhoneVerificationCodes.AddAsync(
            phoneVerificationCode,
            cancellationToken);

        return phoneVerificationCode.Id;
    }

    public async Task<PhoneVerificationCode?> GetLatestActiveByPhone(string phone, CancellationToken ct)
    {
        return await _dbContext.PhoneVerificationCodes
            .Where(x => x.Phone == phone && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);   
    }
}