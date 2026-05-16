using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Sellers.Features;

public sealed record VLavkeGetSellerMeResponse(
    Guid UserId,
    string FullName,
    Guid MainCityId,
    string? Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed class VLavkeGetSellerMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("vlavke/sellers/me", async Task<EndpointResult<VLavkeGetSellerMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VLavkeGetSellerMeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VLavkeGetSellerMeHandler
{
    private readonly IVLavkeSellersReadDbContext _sellersReadDb;

    public VLavkeGetSellerMeHandler(IVLavkeSellersReadDbContext sellersReadDb)
    {
        _sellersReadDb = sellersReadDb;
    }

    public async Task<Result<VLavkeGetSellerMeResponse, Errors>> Handle(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await _sellersReadDb.SellerProfilesRead
            .Where(x => x.UserId == userId)
            .Select(x => new VLavkeGetSellerMeResponse(
                x.UserId,
                x.FullName,
                x.MainCityId,
                x.Email,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return VLavkeSellerErrors.ProfileNotFound(userId).ToErrors();

        return profile;
    }
}
