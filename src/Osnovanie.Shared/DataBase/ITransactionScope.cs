using CSharpFunctionalExtensions;

namespace Osnovanie.Shared.DataBase;

public interface ITransactionScope : IAsyncDisposable
{
    Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken);
    Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken);
}