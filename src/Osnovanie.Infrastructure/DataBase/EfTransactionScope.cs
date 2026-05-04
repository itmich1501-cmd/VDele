using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Infrastructure.DataBase;

public sealed class EfTransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;
    private readonly ILogger<EfTransactionScope> _logger;

    public EfTransactionScope(
        IDbContextTransaction transaction,
        ILogger<EfTransactionScope> logger)
    {
        _transaction = transaction;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _transaction.CommitAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction");
            return Error.Failure("database.transaction.commit", "Failed to commit transaction");
        }
    }

    public async Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction");
            return Error.Failure("database.transaction.rollback", "Failed to rollback transaction");
        }
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}