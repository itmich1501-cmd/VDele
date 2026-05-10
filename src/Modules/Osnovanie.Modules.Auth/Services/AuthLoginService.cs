using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Modules.Auth.Abstractions.Persistence;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.Auth.DataBase;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Services;

public sealed class AuthLoginService : IAuthLoginService
  {
      private readonly UserManager<User> _userManager;
      private readonly IPhoneVerificationCodeRepository _phoneCodeRepository;
      private readonly IAuthTokenService _authTokenService;
      private readonly IAuthReadDbConnection _readDb;
      private readonly ITransactionManager _transactionManager;

      public AuthLoginService(
          UserManager<User> userManager,
          IPhoneVerificationCodeRepository phoneCodeRepository,
          IAuthTokenService authTokenService,
          IAuthReadDbConnection readDb,
          ITransactionManager transactionManager)
      {
          _userManager = userManager;
          _phoneCodeRepository = phoneCodeRepository;
          _authTokenService = authTokenService;
          _readDb = readDb;
          _transactionManager = transactionManager;
      }

      public async Task<Result<string, Errors>> LoginByPhone(
          string phone,
          string code,
          string applicationCode,
          string roleCode,
          CancellationToken cancellationToken)
      {
          var user = await _userManager.Users
              .FirstOrDefaultAsync(x => x.PhoneNumber == phone, cancellationToken);

          if (user == null)
              return AuthErrors.InvalidCredentials().ToErrors();

          var hasAccess = await _readDb.UserAccessesRead
              .AnyAsync(x => x.UserId == user.Id
                          && x.ApplicationCode == applicationCode
                          && x.RoleCode == roleCode, cancellationToken);

          if (!hasAccess)
              return AuthErrors.InvalidCredentials().ToErrors();

          var verificationCode = await _phoneCodeRepository.GetLatestActiveByPhone(
              phone, cancellationToken);

          if (verificationCode == null)
              return AuthErrors.PhoneVerificationCode.NotFound().ToErrors();

          var confirmResult = verificationCode.Confirm(code);
          if (confirmResult.IsFailure)
              return confirmResult.Error.ToErrors();

          var markAsUsedResult = verificationCode.MarkAsUsed();
          if (markAsUsedResult.IsFailure)
              return markAsUsedResult.Error.ToErrors();

          var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
          if (saveResult.IsFailure)
              return saveResult.Error.ToErrors();

          return await _authTokenService.GenerateTokenForUser(user.Id, cancellationToken);
      }
      
      public async Task<Result<string, Errors>> LoginByUsername(
          string username,
          string password,
          string applicationCode,
          string roleCode,
          CancellationToken cancellationToken)
      {
          var user = await _userManager.FindByNameAsync(username);
          if (user == null)
              return AuthErrors.InvalidCredentials().ToErrors();

          var passwordValid = await _userManager.CheckPasswordAsync(user, password);
          if (!passwordValid)
              return AuthErrors.InvalidCredentials().ToErrors();

          var hasAccess = await _readDb.UserAccessesRead
              .AnyAsync(x => x.UserId == user.Id
                             && x.ApplicationCode == applicationCode
                             && x.RoleCode == roleCode, cancellationToken);

          if (!hasAccess)
              return AuthErrors.InvalidCredentials().ToErrors();

          return await _authTokenService.GenerateTokenForUser(user.Id, cancellationToken);
      }

  }