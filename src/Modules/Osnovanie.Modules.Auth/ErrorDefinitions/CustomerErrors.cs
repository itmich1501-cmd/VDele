using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.ErrorDefinitions;

public static class CustomerErrors
{
    public static Error UserIdIsEmpty() =>
        Error.Validation(
            "vdele.customer.user_id.empty",
            "UserId обязателен",
            "userId");

    public static Error FullNameIsEmpty() =>
        Error.Validation(
            "vdele.customer.full_name.empty",
            "ФИО обязательно",
            "fullName");

    public static Error CityIdIsEmpty() =>
        Error.Validation(
            "vdele.customer.city_id.empty",
            "Город обязателен",
            "cityId");
}