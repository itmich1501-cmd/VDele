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

public record RegisterVDeleCustomerByPhoneRequest(
    string Phone,
    string Password,
    string FirstName,
    Guid CityId);

public sealed class RegisterVDeleCustomerByPhoneValidator
    : AbstractValidator<RegisterVDeleCustomerByPhoneRequest>
{
    public RegisterVDeleCustomerByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .Phone();

        RuleFor(x => x.Password)
            .Password();

        RuleFor(x => x.FirstName)
            .FirstName();
    }
}

public sealed class RegisterVDeleCustomerByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/vdele/customer/register-by-phone", async (
            [FromServices] RegisterVDeleCustomerByPhoneHandler handler,
            [FromBody] RegisterVDeleCustomerByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterVDeleCustomerByPhoneHandler
{
    private readonly IValidator<RegisterVDeleCustomerByPhoneRequest> _validator;
    private readonly PhoneRegistrationService _phoneRegistrationService;

    public RegisterVDeleCustomerByPhoneHandler(
        IValidator<RegisterVDeleCustomerByPhoneRequest> validator,
        PhoneRegistrationService phoneRegistrationService)
    {
        _validator = validator;
        _phoneRegistrationService = phoneRegistrationService;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterVDeleCustomerByPhoneRequest? request,
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
            ApplicationCodes.VDele,
            RoleCodes.Customer,
            request.CityId);

        return await _phoneRegistrationService.Register(command, cancellationToken);
    }
}