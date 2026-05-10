using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.VLavke.Customers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Customers.Features;

public sealed record VLavkeLoginCustomerByPhoneRequest(string Phone, string Code);

public sealed class VLavkeLoginCustomerByPhoneValidator
  : AbstractValidator<VLavkeLoginCustomerByPhoneRequest>
{
  public VLavkeLoginCustomerByPhoneValidator()
  {
      RuleFor(x => x.Phone)
          .NotEmpty()
          .WithError(VLavkeCustomerValidationErrors.PhoneIsEmpty());

      RuleFor(x => x.Code)
          .NotEmpty()
          .Matches(@"^\d{4}$")
          .WithError(VLavkeCustomerValidationErrors.CodeIsInvalid());
  }
}

public sealed class VLavkeLoginCustomerByPhoneEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
      app.MapPost("vlavke/customers/login-by-phone", async (
          VLavkeLoginCustomerByPhoneHandler handler,
          VLavkeLoginCustomerByPhoneRequest request,
          CancellationToken cancellationToken) =>
      {
          var result = await handler.Handle(request, cancellationToken);
          return new EndpointResult<string>(result);
      });
  }
}

public sealed class VLavkeLoginCustomerByPhoneHandler
{
  private readonly IAuthLoginService _authLoginService;
  private readonly IValidator<VLavkeLoginCustomerByPhoneRequest> _validator;

  public VLavkeLoginCustomerByPhoneHandler(
      IAuthLoginService authLoginService,
      IValidator<VLavkeLoginCustomerByPhoneRequest> validator)
  {
      _authLoginService = authLoginService;
      _validator = validator;
  }

  public async Task<Result<string, Errors>> Handle(
      VLavkeLoginCustomerByPhoneRequest request,
      CancellationToken cancellationToken)
  {
      var validationResult = await _validator.ValidateAsync(request, cancellationToken);
      if (!validationResult.IsValid)
          return validationResult.ToErrors();

      return await _authLoginService.LoginByPhone(
          request.Phone,
          request.Code,
          ApplicationCodes.VLavke,
          RoleCodes.Customer,
          cancellationToken);
  }
}