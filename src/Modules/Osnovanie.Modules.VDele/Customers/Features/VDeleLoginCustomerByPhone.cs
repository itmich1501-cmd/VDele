using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.VDele.Customers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Customers.Features;

public sealed record VDeleLoginCustomerByPhoneRequest(string Phone, string Code);

public sealed class VDeleLoginCustomerByPhoneValidator
  : AbstractValidator<VDeleLoginCustomerByPhoneRequest>
{
  public VDeleLoginCustomerByPhoneValidator()
  {
      RuleFor(x => x.Phone)
          .NotEmpty()
          .WithError(VDeleCustomerValidationErrors.PhoneIsEmpty());

      RuleFor(x => x.Code)
          .NotEmpty()
          .Matches(@"^\d{4}$")
          .WithError(VDeleCustomerValidationErrors.CodeIsInvalid());
  }
}

public sealed class VDeleLoginCustomerByPhoneEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
      app.MapPost("vdele/customers/login-by-phone", async (
          VDeleLoginCustomerByPhoneHandler handler,
          VDeleLoginCustomerByPhoneRequest request,
          CancellationToken cancellationToken) =>
      {
          var result = await handler.Handle(request, cancellationToken);
          return new EndpointResult<string>(result);
      });
  }
}

public sealed class VDeleLoginCustomerByPhoneHandler
{
  private readonly IAuthLoginService _authLoginService;
  private readonly IValidator<VDeleLoginCustomerByPhoneRequest> _validator;

  public VDeleLoginCustomerByPhoneHandler(
      IAuthLoginService authLoginService,
      IValidator<VDeleLoginCustomerByPhoneRequest> validator)
  {
      _authLoginService = authLoginService;
      _validator = validator;
  }

  public async Task<Result<string, Errors>> Handle(
      VDeleLoginCustomerByPhoneRequest request,
      CancellationToken cancellationToken)
  {
      var validationResult = await _validator.ValidateAsync(request, cancellationToken);
      if (!validationResult.IsValid)
          return validationResult.ToErrors();

      return await _authLoginService.LoginByPhone(
          request.Phone,
          request.Code,
          ApplicationCodes.VDele,
          RoleCodes.Customer,
          cancellationToken);
  }
}