using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Domain.Constants;
using Osnovanie.Modules.Auth.Services;
using Osnovanie.Modules.Auth.Validation;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Features;

public record RegisterVLavkeSellerByPhoneRequest(
    string Phone,
    string Password,
    string FirstName,
    Guid CityId,
    string? Email);

public sealed class RegisterVLavkeSellerByPhoneValidator
    : AbstractValidator<RegisterVLavkeSellerByPhoneRequest>
{
    public RegisterVLavkeSellerByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .Phone();

        RuleFor(x => x.Password)
            .Password();

        RuleFor(x => x.FirstName)
            .FirstName();
    }
}

public sealed class RegisterVLavkeSellerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/vlavke/seller/register-by-phone", async (
            [FromServices] RegisterVLavkeSellerByPhoneHandler handler,
            [FromBody] RegisterVLavkeSellerByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterVLavkeSellerByPhoneHandler
{
    private readonly IValidator<RegisterVLavkeSellerByPhoneRequest> _validator;
    private readonly PhoneRegistrationService _phoneRegistrationService;

    public RegisterVLavkeSellerByPhoneHandler(
        IValidator<RegisterVLavkeSellerByPhoneRequest> validator,
        PhoneRegistrationService phoneRegistrationService)
    {
        _validator = validator;
        _phoneRegistrationService = phoneRegistrationService;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterVLavkeSellerByPhoneRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Error.Validation(
                "auth.register.request.empty",
                "Тело запроса обязательно",
                "request").ToErrors();
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var command = new PhoneRegistrationCommand(
            request.Phone,
            request.Password,
            request.FirstName,
            ApplicationCodes.VLavke,
            RoleCodes.Seller,
            request.CityId);

        return await _phoneRegistrationService.Register(command, cancellationToken);
    }
}