using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;

public static class VLavkeSellerValidationErrors
{
    public static Error PhoneIsEmpty() =>
        Error.Validation(
            "vlavke.seller.phone.empty",
            "Телефон обязателен",
            "phone");

    public static Error PasswordIsEmpty() =>
        Error.Validation(
            "vlavke.seller.password.empty",
            "Пароль обязателен",
            "password");

    public static Error PasswordIsTooShort() =>
        Error.Validation(
            "vlavke.seller.password.too_short",
            "Пароль должен содержать минимум 6 символов",
            "password");

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

    public static Error EmailIsInvalid() =>
        Error.Validation(
            "vlavke.seller.email.invalid",
            "Некорректный email",
            "email");

    public static Error RequestIsEmpty() =>
        Error.Validation(
            "vlavke.seller.register.request.empty",
            "Тело запроса обязательно",
            "request");
}