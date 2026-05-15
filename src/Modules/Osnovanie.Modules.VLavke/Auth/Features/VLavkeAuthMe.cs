using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.Auth.Contracts;
using Osnovanie.Modules.VLavke.Customers.Contracts;
using Osnovanie.Modules.VLavke.Sellers.Contracts;
using Osnovanie.Shared;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Osnovanie.Modules.VLavke.Auth.Features;

public sealed record VLavkeAuthMeResponse(
    Guid UserId,
    string Phone,
    IReadOnlyList<string> Roles,
    bool HasCustomerProfile,
    bool HasSellerProfile);

public sealed class VLavkeAuthMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("vlavke/auth/me", async (
                ClaimsPrincipal currentUser,
                [FromServices] VLavkeAuthMeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userIdString = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                var result = await handler.Handle(userId, cancellationToken);
                return (IResult)new EndpointResult<VLavkeAuthMeResponse>(result);
            })
            .RequireAuthorization();
    }
}

public sealed class VLavkeAuthMeHandler
{
    private readonly IAuthUserQueryService _authUserQueryService;
    private readonly IVLavkeCustomersReadDbContext _customersReadDb;
    private readonly IVLavkeSellersReadDbContext _sellersReadDb;

    public VLavkeAuthMeHandler(
        IAuthUserQueryService authUserQueryService,
        IVLavkeCustomersReadDbContext customersReadDb,
        IVLavkeSellersReadDbContext sellersReadDb)
    {
        _authUserQueryService = authUserQueryService;
        _customersReadDb = customersReadDb;
        _sellersReadDb = sellersReadDb;
    }

    public async Task<Result<VLavkeAuthMeResponse, Errors>> Handle(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userInfoResult = await _authUserQueryService.GetUserInfo(
            userId,
            ApplicationCodes.VLavke,
            cancellationToken);

        if (userInfoResult.IsFailure)
            return userInfoResult.Error;

        var userInfo = userInfoResult.Value;

        var hasCustomer = await _customersReadDb.CustomerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        var hasSeller = await _sellersReadDb.SellerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        return new VLavkeAuthMeResponse(
            userInfo.UserId,
            userInfo.Phone,
            userInfo.Roles,
            hasCustomer,
            hasSeller);
    }
}
