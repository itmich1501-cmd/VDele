namespace Osnovanie.Modules.Auth.Abstractions.Services;

public interface ISmsSender
{
    Task SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}