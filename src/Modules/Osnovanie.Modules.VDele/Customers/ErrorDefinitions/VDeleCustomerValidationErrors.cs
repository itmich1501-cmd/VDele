using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Customers.ErrorDefinitions;

public static class VDeleCustomerValidationErrors
{
    public static Error PhoneIsEmpty() =>
        Error.Validation(
            "vdele.customer.phone.empty",
            "Телефон обязателен",
            "phone");

    public static Error PasswordIsEmpty() =>
        Error.Validation(
            "vdele.customer.password.empty",
            "Пароль обязателен",
            "password");

    public static Error PasswordIsTooShort() =>
        Error.Validation(
            "vdele.customer.password.too_short",
            "Пароль должен содержать минимум 6 символов",
            "password");

    public static Error FullNameIsEmpty() =>
        Error.Validation(
            "vdele.customer.full_name.empty",
            "ФИО обязательно",
            "fullName");

    public static Error FullNameIsTooLong() =>
        Error.Validation(
            "vdele.customer.full_name.too_long",
            "ФИО не должно превышать 200 символов",
            "fullName");

    public static Error CityIdIsEmpty() =>
        Error.Validation(
            "vdele.customer.city_id.empty",
            "Город обязателен",
            "cityId");

    public static Error EmailIsInvalid() =>
        Error.Validation(
            "vdele.customer.email.invalid",
            "Некорректный email",
            "email");

    public static Error RequestIsEmpty() =>
        Error.Validation(
            "vdele.customer.register.request.empty",
            "Тело запроса обязательно",
            "request");
}