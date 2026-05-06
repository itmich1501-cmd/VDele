using CSharpFunctionalExtensions;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Domain;

public sealed class VLavkeSellerProfile
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public Guid MainCityId { get; private set; }

    public string? Email { get; private set; }

    public string? CompanyName { get; private set; }

    public string? Inn { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private VLavkeSellerProfile()
    {
    }

    private VLavkeSellerProfile(
        Guid userId,
        string fullName,
        Guid mainCityId,
        string? email)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FullName = fullName;
        MainCityId = mainCityId;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<VLavkeSellerProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid mainCityId,
        string? email)
    {
        if (userId == Guid.Empty)
            return SellerErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return SellerErrors.FullNameIsEmpty();

        if (mainCityId == Guid.Empty)
            return SellerErrors.MainCityIdIsEmpty();

        return new VLavkeSellerProfile(
            userId,
            fullName.Trim(),
            mainCityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());
    }
}