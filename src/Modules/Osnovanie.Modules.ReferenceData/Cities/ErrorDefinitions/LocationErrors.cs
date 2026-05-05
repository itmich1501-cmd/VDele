using Osnovanie.Shared;

namespace Osnovanie.Modules.ReferenceData.Cities.ErrorDefinitions;

public static class LocationErrors
{
    public static class City
    {
        public static Error IdIsEmpty() =>
            Error.Validation(
                "location.city.id.empty",
                "CityId is empty");

        public static Error NameIsEmpty() =>
            Error.Validation(
                "location.city.name.empty",
                "City name is empty");

        public static Error RegionNameIsEmpty() =>
            Error.Validation(
                "location.city.region.empty",
                "Region name is empty");

        public static Error TimeZoneIsEmpty() =>
            Error.Validation(
                "location.city.timezone.empty",
                "TimeZoneId is empty");

        public static Error NotFound(Guid cityId) =>
            Error.NotFound(
                "location.city.not_found",
                $"City with id {cityId} not found");

        public static Error NotVisible(Guid cityId) =>
            Error.Validation(
                "location.city.not_visible",
                $"City with id {cityId} is not visible");
    }
}