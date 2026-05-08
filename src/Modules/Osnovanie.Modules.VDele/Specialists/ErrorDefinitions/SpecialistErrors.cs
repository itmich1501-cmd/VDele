using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;

public static class SpecialistErrors
{
    public static Error AlreadyExists(Guid userId) =>
        Error.Conflict(
            "vdele.specialist.already_exists",
            $"Профиль специалиста для пользователя {userId} уже существует");

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

    public static Error FullNameIsTooLong() =>
        Error.Validation(
            "vdele.specialist.full_name.too_long",
            "ФИО не должно превышать 200 символов",
            "fullName");

    public static Error CityIdIsEmpty() =>
        Error.Validation(
            "vdele.specialist.city_id.empty",
            "Город обязателен",
            "cityId");

    public static Error AboutIsTooLong() =>
        Error.Validation(
            "vdele.specialist.about.too_long",
            "Описание не должно превышать 2000 символов",
            "about");
}