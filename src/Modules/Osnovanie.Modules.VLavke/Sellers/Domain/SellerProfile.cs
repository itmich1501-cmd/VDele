using CSharpFunctionalExtensions;
using Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Sellers.Domain;

public sealed class SellerProfile
{
    private SellerProfile(
        Guid id,
        Guid userId,
        string fullName,
        Guid mainCityId,
        string? email)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        MainCityId = mainCityId;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; }

    public Guid MainCityId { get; private set; }

    public string? Email { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Result<SellerProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid mainCityId,
        string? email)
    {
        if (userId == Guid.Empty)
            return SellerErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return SellerErrors.FullNameIsEmpty();

        if (fullName.Length > 200)
            return SellerErrors.FullNameIsTooLong();

        if (mainCityId == Guid.Empty)
            return SellerErrors.MainCityIdIsEmpty();

        var sellerProfile = new SellerProfile(
            Guid.NewGuid(),
            userId,
            fullName.Trim(),
            mainCityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());

        return sellerProfile;
    }
}