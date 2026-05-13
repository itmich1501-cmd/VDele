using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Abstractions.Persistence;
using Osnovanie.Modules.Auth.Abstractions.Services;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;
using Osnovanie.Shared.Validation;

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
    private readonly ILogger<SendPhoneCodeHandler> _logger;

    public SendPhoneCodeHandler(ISmsSender smsSender, 
        IValidator<SendPhoneCodeRequest> validator,
        ILogger<SendPhoneCodeHandler> logger,
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

        var isTestPhone = _options.TestPhones.Contains(request.Phone);

        PhoneVerificationCode entity;

        if (isTestPhone)
        {
            var testCodeResult = PhoneVerificationCode.CreateWithCode(
                request.Phone,
                _options.TestPhoneFixedCode,
                TimeSpan.FromSeconds(_options.CodeLifetimeSeconds));

            if (testCodeResult.IsFailure)
                return testCodeResult.Error.ToErrors();

            entity = testCodeResult.Value;
        }
        else
        {
            var codeResult = PhoneVerificationCode.Create(
                request.Phone,
                TimeSpan.FromSeconds(_options.CodeLifetimeSeconds));

            if (codeResult.IsFailure)
                return codeResult.Error.ToErrors();

            entity = codeResult.Value.Entity;

            await _repository.Add(entity, cancellationToken);
            await _transactionManager.SaveChangesAsync(cancellationToken);

            var smsResult = await _smsSender.SendAsync(
                request.Phone,
                $"Код подтверждения OSNOVANIE: {codeResult.Value.Code}. Никому не сообщайте код.",
                cancellationToken);

            if (smsResult.IsFailure)
                return smsResult.Error.ToErrors();

            return UnitResult.Success<Errors>();
        }

        // Для тестовых номеров — только записать в БД, БЕЗ SMS
        await _repository.Add(entity, cancellationToken);
        await _transactionManager.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Errors>();
    }
}