namespace Osnovanie.Modules.Auth.Contracts;

public interface ISmsSender
{
    Task SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}