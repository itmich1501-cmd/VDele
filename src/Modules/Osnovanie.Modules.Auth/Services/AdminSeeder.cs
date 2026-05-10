using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Osnovanie.Modules.Auth.Abstractions.Persistence;
using Osnovanie.Modules.Auth.Configuration;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Shared.DataBase;

namespace Osnovanie.Modules.Auth.Services;

public class AdminSeeder
  {
      private readonly UserManager<User> _userManager;
      private readonly IUserAccessRepository _userAccessRepository;
      private readonly ITransactionManager _transactionManager;
      private readonly AdminSeedOptions _options;
      private readonly ILogger<AdminSeeder> _logger;

      public AdminSeeder(
          UserManager<User> userManager,
          IUserAccessRepository userAccessRepository,
          ITransactionManager transactionManager,
          IOptions<AdminSeedOptions> options,
          ILogger<AdminSeeder> logger)
      {
          _userManager = userManager;
          _userAccessRepository = userAccessRepository;
          _transactionManager = transactionManager;
          _options = options.Value;
          _logger = logger;
      }

      public async Task SeedAsync(CancellationToken cancellationToken = default)
      {
          foreach (var admin in _options.Admins)
          {
              var existing = await _userManager.FindByNameAsync(admin.Username);
              if (existing != null)
              {
                  _logger.LogInformation("Admin {Username} already exists, skipping", admin.Username);
                  continue;
              }

              var user = new User
              {
                  Id = Guid.NewGuid(),
                  UserName = admin.Username,
                  NormalizedUserName = admin.Username.ToUpperInvariant()
              };

              var createResult = await _userManager.CreateAsync(user, admin.Password);
              if (!createResult.Succeeded)
              {
                  _logger.LogError(
                      "Failed to seed admin {Username}: {Errors}",
                      admin.Username,
                      string.Join(", ", createResult.Errors.Select(x => x.Description)));
                  continue;
              }

              var userAccessResult = UserAccess.Create(user.Id, admin.ApplicationCode, admin.RoleCode);
              if (userAccessResult.IsFailure)
              {
                  _logger.LogError("Failed to create UserAccess for {Username}", admin.Username);
                  continue;
              }

              await _userAccessRepository.Add(userAccessResult.Value!, cancellationToken);
              await _transactionManager.SaveChangesAsync(cancellationToken);

              _logger.LogInformation("Admin {Username} seeded successfully", admin.Username);
          }
      }
  }