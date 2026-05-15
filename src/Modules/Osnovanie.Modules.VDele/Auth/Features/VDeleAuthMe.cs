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
using Osnovanie.Modules.VDele.Customers.Contracts;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Shared;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Osnovanie.Modules.VDele.Auth.Features;

public sealed record VDeleAuthMeResponse(
    Guid UserId,
    string Phone,
    IReadOnlyList<string> Roles,
    bool HasCustomerProfile,
    bool HasSpecialistProfile);

public sealed class VDeleAuthMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("vdele/auth/me", async (
                ClaimsPrincipal currentUser,
                [FromServices] VDeleAuthMeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userIdString = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                    return Results.Unauthorized();

                var result = await handler.Handle(userId, cancellationToken);
                return (IResult)new EndpointResult<VDeleAuthMeResponse>(result);
            })
            .RequireAuthorization();
    }
}

public sealed class VDeleAuthMeHandler
{
    private readonly IAuthUserQueryService _authUserQueryService;
    private readonly IVDeleCustomersReadDbContext _customersReadDb;
    private readonly IVDeleSpecialistsReadDbContext _specialistsReadDb;

    public VDeleAuthMeHandler(
        IAuthUserQueryService authUserQueryService,
        IVDeleCustomersReadDbContext customersReadDb,
        IVDeleSpecialistsReadDbContext specialistsReadDb)
    {
        _authUserQueryService = authUserQueryService;
        _customersReadDb = customersReadDb;
        _specialistsReadDb = specialistsReadDb;
    }

    public async Task<Result<VDeleAuthMeResponse, Errors>> Handle(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userInfoResult = await _authUserQueryService.GetUserInfo(
            userId,
            ApplicationCodes.VDele,
            cancellationToken);

        if (userInfoResult.IsFailure)
            return userInfoResult.Error;

        var userInfo = userInfoResult.Value;

        var hasCustomer = await _customersReadDb.CustomerProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        var hasSpecialist = await _specialistsReadDb.SpecialistProfilesRead
            .AnyAsync(x => x.UserId == userId, cancellationToken);

        return new VDeleAuthMeResponse(
            userInfo.UserId,
            userInfo.Phone,
            userInfo.Roles,
            hasCustomer,
            hasSpecialist);
    }
}
