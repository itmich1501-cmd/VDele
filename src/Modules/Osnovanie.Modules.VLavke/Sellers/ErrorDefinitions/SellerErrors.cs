using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;

public static class SellerErrors
{
    public static Error AlreadyExists(Guid userId) =>
        Error.Conflict(
            "vlavke.seller.already_exists",
            $"Профиль продавца для пользователя {userId} уже существует");

    public static Error UserIdIsEmpty() =>
        Error.Validation(
            "vlavke.seller.user_id.empty",
            "UserId обязателен",
            "userId");

    public static Error FullNameIsEmpty() =>
        Error.Validation(
            "vlavke.seller.full_name.empty",
            "ФИО обязательно",
            "fullName");

    public static Error FullNameIsTooLong() =>
        Error.Validation(
            "vlavke.seller.full_name.too_long",
            "ФИО не должно превышать 200 символов",
            "fullName");

    public static Error MainCityIdIsEmpty() =>
        Error.Validation(
            "vlavke.seller.main_city_id.empty",
            "Основной город обязателен",
            "mainCityId");
}