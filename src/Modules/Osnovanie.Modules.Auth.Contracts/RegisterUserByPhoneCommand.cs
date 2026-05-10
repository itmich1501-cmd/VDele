namespace Osnovanie.Modules.Auth.Contracts;

public sealed record RegisterUserByPhoneCommand(
    string Phone,
    string Code,
    string? Password,
    string? Email,
    string ApplicationCode,
    string RoleCode);