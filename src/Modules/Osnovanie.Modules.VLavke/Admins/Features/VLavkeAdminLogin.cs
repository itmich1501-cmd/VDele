using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VLavke.Admins.Features;

public sealed record VLavkeAdminLoginRequest(string Username, string Password);

public sealed class VLavkeAdminLoginValidator : AbstractValidator<VLavkeAdminLoginRequest>
{
  public VLavkeAdminLoginValidator()
  {
      RuleFor(x => x.Username)
          .NotEmpty()
          .WithError(Error.Validation("vlavke.admin.username.empty", "Логин обязателен", "username"));

      RuleFor(x => x.Password)
          .NotEmpty()
          .WithError(Error.Validation("vlavke.admin.password.empty", "Пароль обязателен", "password"));
  }
}

public sealed class VLavkeAdminLoginEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
      app.MapPost("vlavke/admin/login", async (
          VLavkeAdminLoginHandler handler,
          VLavkeAdminLoginRequest request,
          CancellationToken cancellationToken) =>
      {
          var result = await handler.Handle(request, cancellationToken);
          return new EndpointResult<string>(result);
      });
  }
}

public sealed class VLavkeAdminLoginHandler
{
  private readonly IAuthLoginService _authLoginService;
  private readonly IValidator<VLavkeAdminLoginRequest> _validator;

  public VLavkeAdminLoginHandler(
      IAuthLoginService authLoginService,
      IValidator<VLavkeAdminLoginRequest> validator)
  {
      _authLoginService = authLoginService;
      _validator = validator;
  }

  public async Task<Result<string, Errors>> Handle(
      VLavkeAdminLoginRequest request,
      CancellationToken cancellationToken)
  {
      var validationResult = await _validator.ValidateAsync(request, cancellationToken);
      if (!validationResult.IsValid)
          return validationResult.ToErrors();

      return await _authLoginService.LoginByUsername(
          request.Username,
          request.Password,
          ApplicationCodes.VLavke,
          RoleCodes.Admin,
          cancellationToken);
  }
}