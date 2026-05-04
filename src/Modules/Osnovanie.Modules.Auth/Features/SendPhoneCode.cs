using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Jwt;
using Osnovanie.Modules.Auth.Repositories;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Features;

public record SendPhoneCodeRequest(string Phone);

public class SendPhoneCodeRequestValidator : AbstractValidator<SendPhoneCodeRequest>
{
    public SendPhoneCodeRequestValidator()
    {
        RuleFor(x => x.Phone).Phone();
    }
}

public class SendPhoneCode : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/phone/send-code", async Task<EndpointResult> (
            SendPhoneCodeHandler handler,
            SendPhoneCodeRequest request,
            CancellationToken cancellationToken
        ) =>
        {
            return await handler.Handle(request, cancellationToken);
        });
    }
}

public class SendPhoneCodeHandler
{
    private readonly IPhoneVerificationCodeRepository _repository;
    private readonly ITransactionManager _transactionManager;
    private readonly ISmsSender _smsSender;
    private readonly PhoneVerificationOptions _options;
    private readonly IValidator<SendPhoneCodeRequest> _validator;
    private readonly ILogger<RegisterUserHandler> _logger;

    public SendPhoneCodeHandler(ISmsSender smsSender, 
        IValidator<SendPhoneCodeRequest> validator,
        ILogger<RegisterUserHandler> logger,
        IPhoneVerificationCodeRepository repository,
        ITransactionManager transactionManager,
        IOptions<PhoneVerificationOptions> options)
    {
        _repository = repository;
        _transactionManager = transactionManager;
        _options = options.Value;
        _smsSender = smsSender;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UnitResult<Errors>> Handle(
        SendPhoneCodeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var codeResult = PhoneVerificationCode.Create(
            request.Phone,
            TimeSpan.FromSeconds(_options.CodeLifetimeSeconds));

        if (codeResult.IsFailure)
            return codeResult.Error.ToErrors();

        var entity = codeResult.Value.Entity;
        var code = codeResult.Value.Code;

        await _repository.Add(entity, cancellationToken);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error.ToErrors();

        await _smsSender.SendAsync(
            request.Phone,
            $"Ваш код: {code}",
            cancellationToken);

        return UnitResult.Success<Errors>();
    }
}