using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Shared;
using Osnovanie.Shared.Validation;

namespace Osnovanie.Modules.VDele.Admins.Features;

public sealed record VDeleAdminLoginRequest(string Username, string Password);

public sealed class VDeleAdminLoginValidator : AbstractValidator<VDeleAdminLoginRequest>
{
  public VDeleAdminLoginValidator()
  {
      RuleFor(x => x.Username)
          .NotEmpty()
          .WithError(Error.Validation("vdele.admin.username.empty", "Логин обязателен", "username"));

      RuleFor(x => x.Password)
          .NotEmpty()
          .WithError(Error.Validation("vdele.admin.password.empty", "Пароль обязателен", "password"));
  }
}

public sealed class VDeleAdminLoginEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
      app.MapPost("vdele/admin/login", async (
          VDeleAdminLoginHandler handler,
          VDeleAdminLoginRequest request,
          CancellationToken cancellationToken) =>
      {
          var result = await handler.Handle(request, cancellationToken);
          return new EndpointResult<string>(result);
      });
  }
}

public sealed class VDeleAdminLoginHandler
{
  private readonly IAuthLoginService _authLoginService;
  private readonly IValidator<VDeleAdminLoginRequest> _validator;

  public VDeleAdminLoginHandler(
      IAuthLoginService authLoginService,
      IValidator<VDeleAdminLoginRequest> validator)
  {
      _authLoginService = authLoginService;
      _validator = validator;
  }

  public async Task<Result<string, Errors>> Handle(
      VDeleAdminLoginRequest request,
      CancellationToken cancellationToken)
  {
      var validationResult = await _validator.ValidateAsync(request, cancellationToken);
      if (!validationResult.IsValid)
          return validationResult.ToErrors();

      return await _authLoginService.LoginByUsername(
          request.Username,
          request.Password,
          ApplicationCodes.VDele,
          RoleCodes.Admin,
          cancellationToken);
  }
}