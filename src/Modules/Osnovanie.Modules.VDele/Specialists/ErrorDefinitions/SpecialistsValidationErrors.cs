using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;

public static class SpecialistValidationErrors
{
    public static Error PhoneIsEmpty() =>
        Error.Validation(
            "vdele.specialist.phone.empty",
            "Телефон обязателен",
            "phone");

    public static Error PasswordIsEmpty() =>
        Error.Validation(
            "vdele.specialist.password.empty",
            "Пароль обязателен",
            "password");

    public static Error PasswordIsTooShort() =>
        Error.Validation(
            "vdele.specialist.password.too_short",
            "Пароль должен содержать минимум 6 символов",
            "password");

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

    public static Error EmailIsInvalid() =>
        Error.Validation(
            "vdele.specialist.email.invalid",
            "Некорректный email",
            "email");

    public static Error AboutIsTooLong() =>
        Error.Validation(
            "vdele.specialist.about.too_long",
            "Описание не должно превышать 2000 символов",
            "about");

    public static Error RequestIsEmpty() =>
        Error.Validation(
            "vdele.specialist.register.request.empty",
            "Тело запроса обязательно",
            "request");
}