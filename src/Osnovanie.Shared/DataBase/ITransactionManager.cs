using System.Data;
using CSharpFunctionalExtensions;

namespace Osnovanie.Shared.DataBase;

public interface ITransactionManager
{
    Task<Result<ITransactionScope, Error>> BeginTransactionAsync(
        CancellationToken cancellationToken = default,
        IsolationLevel? isolationLevel = null);

    Task<UnitResult<Error>> SaveChangesAsync(
        CancellationToken cancellationToken);
}