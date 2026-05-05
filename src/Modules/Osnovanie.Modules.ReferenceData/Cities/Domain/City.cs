using CSharpFunctionalExtensions;
using Osnovanie.Modules.ReferenceData.Cities.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.ReferenceData.Cities.Domain;

public class City
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;

    public string? FiasId { get; private set; }
    public string? Oktmo { get; private set; }

    public string RegionName { get; private set; } = null!;
    public string? RegionFiasId { get; private set; }

    public int SortOrder { get; private set; }
    public bool IsVisible { get; private set; }

    public string TimeZoneId { get; private set; } = null!;

    private City() { }

    public static Result<City, Error> Create(
        Guid id,
        string name,
        string regionName,
        string timeZoneId,
        string? fiasId = null,
        string? oktmo = null,
        string? regionFiasId = null,
        int sortOrder = 0,
        bool isVisible = true)
    {
        if (id == Guid.Empty)
            return LocationErrors.City.IdIsEmpty();

        if (string.IsNullOrWhiteSpace(name))
            return LocationErrors.City.NameIsEmpty();

        if (string.IsNullOrWhiteSpace(regionName))
            return LocationErrors.City.RegionNameIsEmpty();

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return LocationErrors.City.TimeZoneIsEmpty();

        return new City
        {
            Id = id,
            Name = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            RegionName = regionName.Trim(),
            TimeZoneId = timeZoneId.Trim(),
            FiasId = string.IsNullOrWhiteSpace(fiasId) ? null : fiasId.Trim(),
            Oktmo = string.IsNullOrWhiteSpace(oktmo) ? null : oktmo.Trim(),
            RegionFiasId = string.IsNullOrWhiteSpace(regionFiasId) ? null : regionFiasId.Trim(),
            SortOrder = sortOrder,
            IsVisible = isVisible
        };
    }

    public UnitResult<Error> Hide()
    {
        IsVisible = false;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Show()
    {
        IsVisible = true;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        return UnitResult.Success<Error>();
    }
}