using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Contracts;

namespace Osnovanie.Modules.Auth.Infrastructure;

public sealed class FakeSmsSender : ISmsSender
{
    private readonly ILogger<FakeSmsSender> _logger;

    public FakeSmsSender(ILogger<FakeSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FAKE SMS to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}