using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.VDele.Specialists.Contracts;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.Features;

public sealed record VDeleGetSpecialistMeResponse(
    Guid UserId,
    string FullName,
    Guid CityId,
    string? Email,
    string? About,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed class VDeleGetSpecialistMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("vdele/specialists/me", async Task<EndpointResult<VDeleGetSpecialistMeResponse>> (
                ClaimsPrincipal currentUser,
                [FromServices] VDeleGetSpecialistMeHandler handler,
                CancellationToken cancellationToken) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return await handler.Handle(userId, cancellationToken);
            })
            .RequireAuthorization();
    }
}

public sealed class VDeleGetSpecialistMeHandler
{
    private readonly IVDeleSpecialistsReadDbContext _specialistsReadDb;

    public VDeleGetSpecialistMeHandler(IVDeleSpecialistsReadDbContext specialistsReadDb)
    {
        _specialistsReadDb = specialistsReadDb;
    }

    public async Task<Result<VDeleGetSpecialistMeResponse, Errors>> Handle(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await _specialistsReadDb.SpecialistProfilesRead
            .Where(x => x.UserId == userId)
            .Select(x => new VDeleGetSpecialistMeResponse(
                x.UserId,
                x.FullName,
                x.CityId,
                x.Email,
                x.About,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return VDeleSpecialistErrors.ProfileNotFound(userId).ToErrors();

        return profile;
    }
}
