using CSharpFunctionalExtensions;
using Osnovanie.Modules.VLavke.Sellers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Sellers.Domain;

public sealed class VLavkeSellerProfile
{
    private VLavkeSellerProfile(
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

    public static Result<VLavkeSellerProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid mainCityId,
        string? email)
    {
        if (userId == Guid.Empty)
            return VLavkeSellerErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return VLavkeSellerErrors.FullNameIsEmpty();

        if (fullName.Length > 200)
            return VLavkeSellerErrors.FullNameIsTooLong();

        if (mainCityId == Guid.Empty)
            return VLavkeSellerErrors.MainCityIdIsEmpty();

        var sellerProfile = new VLavkeSellerProfile(
            Guid.NewGuid(),
            userId,
            fullName.Trim(),
            mainCityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());

        return sellerProfile;
    }

    public UnitResult<Error> Update(
        string fullName,
        Guid mainCityId,
        string? email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return VLavkeSellerErrors.FullNameIsEmpty();

        if (fullName.Length > 200)
            return VLavkeSellerErrors.FullNameIsTooLong();

        if (mainCityId == Guid.Empty)
            return VLavkeSellerErrors.MainCityIdIsEmpty();

        FullName = fullName.Trim();
        MainCityId = mainCityId;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        UpdatedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }
}