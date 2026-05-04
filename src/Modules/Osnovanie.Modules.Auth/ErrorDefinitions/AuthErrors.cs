using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.ErrorDefinitions;

public static class AuthErrors
{
    public static Error EmailSendFailed() =>
        Error.Failure(
            "auth.email.send_failed",
            "Не удалось отправить письмо. Попробуйте позже.");
    
    public static Error UserNotFound(Guid userId) =>
        Error.NotFound(
            "auth.user.not_found",
            $"Пользователь с id {userId} не найден");
    
    public static Error InvalidCredentials() =>
        Error.Authentication(
            "auth.invalid_credentials",
            "Неверный email или пароль");
    
    public static Error UserIdIsEmpty() =>
        Error.Validation("auth.user.id.empty", 
            "UserId is empty");

    public static Error ApplicationCodeIsEmpty() =>
        Error.Validation("auth.application.empty", 
            "ApplicationCode is empty");

    public static Error RoleCodeIsEmpty() =>
        Error.Validation("auth.role.empty", 
            "RoleCode is empty");
    
    public static class PhoneVerificationCode
    {
        public static Error PhoneRequired() =>
            Error.Validation(
                "auth.phone_code.phone_required",
                "Phone is required");

        public static Error InvalidLifetime() =>
            Error.Validation(
                "auth.phone_code.invalid_lifetime",
                "Lifetime must be greater than zero");

        public static Error AlreadyUsed() =>
            Error.Validation(
                "auth.phone_code.already_used",
                "Code already used");

        public static Error Expired() =>
            Error.Validation(
                "auth.phone_code.expired",
                "Code expired");

        public static Error InvalidCode() =>
            Error.Validation(
                "auth.phone_code.invalid_code",
                "Invalid verification code");
        
        public static Error NotFound() =>
            Error.NotFound(
                "auth.phone_code.not_found",
                "Verification code not found");
    }
}