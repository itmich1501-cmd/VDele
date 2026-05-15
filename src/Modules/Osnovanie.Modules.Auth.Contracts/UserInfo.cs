namespace Osnovanie.Modules.Auth.Contracts;

public sealed record UserInfo(
    Guid UserId,
    string Phone,
    IReadOnlyList<string> Roles);
