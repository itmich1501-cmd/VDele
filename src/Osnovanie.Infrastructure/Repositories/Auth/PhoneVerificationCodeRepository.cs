using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Infrastructure.Database;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Repositories.Auth;

public class PhoneVerificationCodeRepository : IPhoneVerificationCodeRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PhoneVerificationCodeRepository> _logger;

    public PhoneVerificationCodeRepository(AppDbContext dbContext, ILogger<PhoneVerificationCodeRepository> logger)
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