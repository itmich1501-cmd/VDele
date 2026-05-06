using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Osnovanie.Framework.EndpointResult;
using Osnovanie.Framework.EndpointSettings;
using Osnovanie.Modules.ReferenceData.DataBase;
using Osnovanie.Shared;

namespace Osnovanie.Modules.ReferenceData.Cities.Features;

public record CityResponse(
    Guid Id,
    string Name,
    string RegionName,
    string TimeZoneId);

public class GetCities : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("reference-data/cities", async Task<EndpointResult<IReadOnlyList<CityResponse>>> (
            GetCitiesHandler handler,
            CancellationToken cancellationToken
        ) =>
        {
            return await handler.Handle(cancellationToken);
        });
    }
}

public class GetCitiesHandler
{
    private readonly IReferenceDataReadDbContext _referenceDataReadDbContext;

    public GetCitiesHandler(IReferenceDataReadDbContext referenceDataReadDbContext)
    {
        _referenceDataReadDbContext = referenceDataReadDbContext;
    }

    public async Task<Result<IReadOnlyList<CityResponse>, Errors>> Handle(
        CancellationToken cancellationToken)
    {
        var cities = await _referenceDataReadDbContext.CitiesRead
            .Where(c => c.IsVisible)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CityResponse(
                c.Id,
                c.Name,
                c.RegionName,
                c.TimeZoneId))
            .ToListAsync(cancellationToken);

        return cities;
    }
}