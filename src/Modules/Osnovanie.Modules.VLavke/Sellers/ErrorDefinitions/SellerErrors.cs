using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;

public static class SellerErrors
{
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

    public static Error MainCityIdIsEmpty() =>
        Error.Validation(
            "vlavke.seller.main_city_id.empty",
            "Город обязателен",
            "mainCityId");
}