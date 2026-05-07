namespace Osnovanie.Modules.Auth.Contracts.Services;

public interface ISmsSender
{
    Task SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}