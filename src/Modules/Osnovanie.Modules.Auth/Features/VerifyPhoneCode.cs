using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts.Persistence;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Features;

public record VerifyPhoneCodeRequest(string Phone, string Code);

public class VerifyPhoneCodeRequestValidator 
    : AbstractValidator<VerifyPhoneCodeRequest>
{
    public VerifyPhoneCodeRequestValidator()
    {
        RuleFor(x => x.Phone).Phone();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{6}$")
            .WithMessage("Code must contain exactly 6 digits");
    }
}

public class VerifyPhoneCode : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/phone/verify-code", async Task<EndpointResult> (
            VerifyPhoneCodeHandler handler,
            VerifyPhoneCodeRequest request,
            CancellationToken cancellationToken
        ) =>
        {
            return await handler.Handle(request, cancellationToken);
        });
    }
}

public class VerifyPhoneCodeHandler
{
    private readonly IValidator<VerifyPhoneCodeRequest> _validator;
    private readonly ILogger<VerifyPhoneCodeHandler> _logger;
    private readonly ITransactionManager _transactionManager;
    private readonly IPhoneVerificationCodeRepository _repository;

    public VerifyPhoneCodeHandler( 
        IValidator<VerifyPhoneCodeRequest> validator,
        ILogger<VerifyPhoneCodeHandler> logger,
        ITransactionManager transactionManager,
        IPhoneVerificationCodeRepository repository)
    {
        _repository = repository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UnitResult<Errors>> Handle(VerifyPhoneCodeRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();
            
        var verificationCode = await _repository.GetLatestActiveByPhone(
            request.Phone,
            cancellationToken);

        if (verificationCode is null)
            return AuthErrors.PhoneVerificationCode.NotFound().ToErrors();

        var verifyResult = verificationCode.Verify(request.Code);
        if (verifyResult.IsFailure)
        {
            return verifyResult.Error.ToErrors();
        }
            
        var markResult = verificationCode.MarkAsUsed();

        if (markResult.IsFailure)
            return markResult.Error.ToErrors();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error.ToErrors();

        return UnitResult.Success<Errors>();
    }
}