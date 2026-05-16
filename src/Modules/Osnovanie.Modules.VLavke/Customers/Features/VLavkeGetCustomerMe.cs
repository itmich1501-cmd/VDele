using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Customers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Customers.Features;

public sealed record VLavkeGetCustomerMeResponse(
    Guid UserId,
    string FullName,
    Guid CityId,
    string? Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed class VLavkeGetCustomerMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("vlavke/customers/me", async Task<EndpointResult<VLavkeGetCustomerMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VLavkeGetCustomerMeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VLavkeGetCustomerMeHandler
{
    private readonly IVLavkeCustomersReadDbContext _customersReadDb;

    public VLavkeGetCustomerMeHandler(IVLavkeCustomersReadDbContext customersReadDb)
    {
        _customersReadDb = customersReadDb;
    }

    public async Task<Result<VLavkeGetCustomerMeResponse, Errors>> Handle(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await _customersReadDb.CustomerProfilesRead
            .Where(x => x.UserId == userId)
            .Select(x => new VLavkeGetCustomerMeResponse(
                x.UserId,
                x.FullName,
                x.CityId,
                x.Email,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return VLavkeCustomerErrors.ProfileNotFound(userId).ToErrors();

        return profile;
    }
}
