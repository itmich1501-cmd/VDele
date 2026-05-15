using CSharpFunctionalExtensions;
  using FluentValidation;
  using Microsoft.AspNetCore.Builder;
  using Microsoft.AspNetCore.Identity;
  using Microsoft.AspNetCore.Routing;
  using Microsoft.EntityFrameworkCore;
  using Osnovanie.Framework.EndpointResult;
  using Osnovanie.Framework.EndpointSettings;
  using Osnovanie.Modules.Auth.DataBase;
  using Osnovanie.Modules.Auth.Domain;
  using Osnovanie.Modules.Auth.ErrorDefinitions;
  using Osnovanie.Modules.Auth.Validation;
  using Osnovanie.Shared;
  using Osnovanie.Shared.Validation;

  namespace Osnovanie.Modules.Auth.Features;

  public record CheckPhoneExistsRequest(string Phone, string ApplicationCode);

  public record CheckPhoneExistsResponse(IReadOnlyList<string> Roles);

  public class CheckPhoneExistsRequestValidator : AbstractValidator<CheckPhoneExistsRequest>
  {
      public CheckPhoneExistsRequestValidator()
      {
          RuleFor(x => x.Phone).Phone();

          RuleFor(x => x.ApplicationCode)
              .NotEmpty()
              .WithError(Error.Validation(
                  "auth.application.empty",
                  "ApplicationCode is required",
                  "applicationCode"));
      }
  }

  public class CheckPhoneExists : IEndpoint
  {
      public void MapEndpoint(IEndpointRouteBuilder app)
      {
          app.MapGet("auth/phone/exists", async (
              CheckPhoneExistsHandler handler,
              string phone,
              string applicationCode,
              CancellationToken cancellationToken) =>
          {
              var result = await handler.Handle(
                  new CheckPhoneExistsRequest(phone, applicationCode),
                  cancellationToken);
              return new EndpointResult<CheckPhoneExistsResponse>(result);
          });
      }
  }

  public class CheckPhoneExistsHandler
  {
      private readonly UserManager<User> _userManager;
      private readonly IAuthReadDbConnection _readDb;
      private readonly IValidator<CheckPhoneExistsRequest> _validator;

      public CheckPhoneExistsHandler(
          UserManager<User> userManager,
          IAuthReadDbConnection readDb,
          IValidator<CheckPhoneExistsRequest> validator)
      {
          _userManager = userManager;
          _readDb = readDb;
          _validator = validator;
      }

      public async Task<Result<CheckPhoneExistsResponse, Errors>> Handle(
          CheckPhoneExistsRequest request,
          CancellationToken cancellationToken)
      {
          var validationResult = await _validator.ValidateAsync(request, cancellationToken);
          if (!validationResult.IsValid)
              return validationResult.ToErrors();

          var user = await _userManager.Users
              .FirstOrDefaultAsync(u => u.PhoneNumber == request.Phone, cancellationToken);

          if (user == null)
              return AuthErrors.UserNotFoundByPhone().ToErrors();

          var roles = await _readDb.UserAccessesRead
              .Where(x => x.UserId == user.Id && x.ApplicationCode == request.ApplicationCode)
              .Select(x => x.RoleCode)
              .ToListAsync(cancellationToken);

          return new CheckPhoneExistsResponse(roles);
      }
  }