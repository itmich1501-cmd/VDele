using System.Data;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Infrastructure.Database;

public class EfTransactionManager<TDbContext> : ITransactionManager
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<EfTransactionManager<TDbContext>> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public EfTransactionManager(
        TDbContext dbContext,
        ILogger<EfTransactionManager<TDbContext>> logger,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<Result<ITransactionScope, Error>> BeginTransactionAsync(
        CancellationToken cancellationToken = default,
        IsolationLevel? isolationLevel = null)
    {
        try
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(
                isolationLevel ?? IsolationLevel.ReadCommitted,
                cancellationToken);

            var scopeLogger = _loggerFactory.CreateLogger<EfTransactionScope>();

            return new EfTransactionScope(transaction, scopeLogger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction");

            return Error.Failure("database.transaction.begin", "Failed to begin transaction");
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes");

            return Error.Failure("database.save_changes", "Failed to save changes");
        }
    }
}