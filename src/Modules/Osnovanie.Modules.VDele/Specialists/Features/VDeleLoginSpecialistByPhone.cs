using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public sealed record VDeleLoginSpecialistByPhoneRequest(string Phone, string Code);

public sealed class VDeleLoginSpecialistByPhoneValidator
  : AbstractValidator<VDeleLoginSpecialistByPhoneRequest>
{
  public VDeleLoginSpecialistByPhoneValidator()
  {
      RuleFor(x => x.Phone)
          .NotEmpty()
          .WithError(VDeleSpecialistValidationErrors.PhoneIsEmpty());

      RuleFor(x => x.Code)
          .NotEmpty()
          .Matches(@"^\d{4}$")
          .WithError(VDeleSpecialistValidationErrors.CodeIsInvalid());
  }
}

public sealed class VDeleLoginSpecialistByPhoneEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
      app.MapPost("vdele/specialists/login-by-phone", async (
          VDeleLoginSpecialistByPhoneHandler handler,
          VDeleLoginSpecialistByPhoneRequest request,
          CancellationToken cancellationToken) =>
      {
          var result = await handler.Handle(request, cancellationToken);
          return new EndpointResult<string>(result);
      });
  }
}

public sealed class VDeleLoginSpecialistByPhoneHandler
{
  private readonly IAuthLoginService _authLoginService;
  private readonly IValidator<VDeleLoginSpecialistByPhoneRequest> _validator;

  public VDeleLoginSpecialistByPhoneHandler(
      IAuthLoginService authLoginService,
      IValidator<VDeleLoginSpecialistByPhoneRequest> validator)
  {
      _authLoginService = authLoginService;
      _validator = validator;
  }

  public async Task<Result<string, Errors>> Handle(
      VDeleLoginSpecialistByPhoneRequest request,
      CancellationToken cancellationToken)
  {
      var validationResult = await _validator.ValidateAsync(request, cancellationToken);
      if (!validationResult.IsValid)
          return validationResult.ToErrors();

      return await _authLoginService.LoginByPhone(
          request.Phone,
          request.Code,
          ApplicationCodes.VDele,
          RoleCodes.Specialist,
          cancellationToken);
  }
}
