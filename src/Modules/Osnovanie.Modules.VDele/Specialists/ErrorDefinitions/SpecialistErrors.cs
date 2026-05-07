using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;

public static class SpecialistErrors
{
    public static Error UserIdIsEmpty() =>
        Error.Validation(
            "vdele.specialist.user_id.empty",
            "UserId обязателен",
            "userId");

    public static Error FullNameIsEmpty() =>
        Error.Validation(
            "vdele.specialist.full_name.empty",
            "ФИО обязательно",
            "fullName");

    public static Error CityIdIsEmpty() =>
        Error.Validation(
            "vdele.specialist.city_id.empty",
            "Город обязателен",
            "cityId");
}