using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Customers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Customers.Features;

public sealed record VDeleGetCustomerMeResponse(
    Guid UserId,
    string FullName,
    Guid CityId,
    string? Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed class VDeleGetCustomerMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("vdele/customers/me", async Task<EndpointResult<VDeleGetCustomerMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VDeleGetCustomerMeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VDeleGetCustomerMeHandler
{
    private readonly IVDeleCustomersReadDbContext _customersReadDb;

    public VDeleGetCustomerMeHandler(IVDeleCustomersReadDbContext customersReadDb)
    {
        _customersReadDb = customersReadDb;
    }

    public async Task<Result<VDeleGetCustomerMeResponse, Errors>> Handle(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await _customersReadDb.CustomerProfilesRead
            .Where(x => x.UserId == userId)
            .Select(x => new VDeleGetCustomerMeResponse(
                x.UserId,
                x.FullName,
                x.CityId,
                x.Email,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return VDeleCustomerErrors.ProfileNotFound(userId).ToErrors();

        return profile;
    }
}
