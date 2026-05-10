using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Shared;

namespace Osnovanie.Infrastructure.Services.Auth;

public sealed class FakeSmsSender : ISmsSender
{
    private readonly ILogger<FakeSmsSender> _logger;

    public FakeSmsSender(ILogger<FakeSmsSender> logger)
    {
        _logger = logger;
    }

    public Task<UnitResult<Error>> SendAsync(string phone, string message, CancellationToken ct)
    {
        _logger.LogInformation("[FAKE SMS] {Phone}: {Message}", phone, message);
        return Task.FromResult(UnitResult.Success<Error>());
    }
}