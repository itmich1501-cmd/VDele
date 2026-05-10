using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Sellers.Features;

public sealed record VLavkeLoginSellerByPhoneRequest(string Phone, string Code);

public sealed class VLavkeLoginSellerByPhoneValidator
  : AbstractValidator<VLavkeLoginSellerByPhoneRequest>
{
  public VLavkeLoginSellerByPhoneValidator()
  {
      RuleFor(x => x.Phone)
          .NotEmpty()
          .WithError(VLavkeSellerValidationErrors.PhoneIsEmpty());

      RuleFor(x => x.Code)
          .NotEmpty()
          .Matches(@"^\d{4}$")
          .WithError(VLavkeSellerValidationErrors.CodeIsInvalid());
  }
}

public sealed class VLavkeLoginSellerByPhoneEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
      app.MapPost("vlavke/sellers/login-by-phone", async (
          VLavkeLoginSellerByPhoneHandler handler,
          VLavkeLoginSellerByPhoneRequest request,
          CancellationToken cancellationToken) =>
      {
          var result = await handler.Handle(request, cancellationToken);
          return new EndpointResult<string>(result);
      });
  }
}

public sealed class VLavkeLoginSellerByPhoneHandler
{
  private readonly IAuthLoginService _authLoginService;
  private readonly IValidator<VLavkeLoginSellerByPhoneRequest> _validator;

  public VLavkeLoginSellerByPhoneHandler(
      IAuthLoginService authLoginService,
      IValidator<VLavkeLoginSellerByPhoneRequest> validator)
  {
      _authLoginService = authLoginService;
      _validator = validator;
  }

  public async Task<Result<string, Errors>> Handle(
      VLavkeLoginSellerByPhoneRequest request,
      CancellationToken cancellationToken)
  {
      var validationResult = await _validator.ValidateAsync(request, cancellationToken);
      if (!validationResult.IsValid)
          return validationResult.ToErrors();

      return await _authLoginService.LoginByPhone(
          request.Phone,
          request.Code,
          ApplicationCodes.VLavke,
          RoleCodes.Seller,
          cancellationToken);
  }
}