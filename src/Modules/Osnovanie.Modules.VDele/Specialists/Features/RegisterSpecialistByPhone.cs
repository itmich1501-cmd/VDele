using CSharpFunctionalExtensions;
using FluentValidation;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public record RegisterVDeleSpecialistByPhoneRequest(
    string Phone,
    string Password,
    string FirstName,
    Guid CityId,
    string? Email);

public sealed class RegisterVDeleSpecialistByPhoneValidator
    : AbstractValidator<RegisterVDeleSpecialistByPhoneRequest>
{
    public RegisterVDeleSpecialistByPhoneValidator()
    {
        RuleFor(x => x.Phone)
            .Phone();

        RuleFor(x => x.Password)
            .Password();

        RuleFor(x => x.FirstName)
            .FirstName();
    }
}

public sealed class RegisterVDeleSpecialistByPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/vdele/specialist/register-by-phone", async (
            [FromServices] RegisterVDeleSpecialistByPhoneHandler handler,
            [FromBody] RegisterVDeleSpecialistByPhoneRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(request, cancellationToken);

            return new EndpointResult<Guid>(result);
        });
    }
}

public sealed class RegisterVDeleSpecialistByPhoneHandler
{
    private readonly IValidator<RegisterVDeleSpecialistByPhoneRequest> _validator;
    private readonly PhoneRegistrationService _phoneRegistrationService;

    public RegisterVDeleSpecialistByPhoneHandler(
        IValidator<RegisterVDeleSpecialistByPhoneRequest> validator,
        PhoneRegistrationService phoneRegistrationService)
    {
        _validator = validator;
        _phoneRegistrationService = phoneRegistrationService;
    }

    public async Task<Result<Guid, Errors>> Handle(
        RegisterVDeleSpecialistByPhoneRequest? request,
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
            RoleCodes.Specialist,
            request.CityId);

        return await _phoneRegistrationService.Register(command, cancellationToken);
    }
}