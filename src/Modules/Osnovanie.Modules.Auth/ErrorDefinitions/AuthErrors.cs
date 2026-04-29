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
}