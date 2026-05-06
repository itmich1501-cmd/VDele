using CSharpFunctionalExtensions;
using Osnovanie.Modules.Auth.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.Auth.Domain;

public sealed class VDeleSpecialistProfile
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public Guid CityId { get; private set; }

    public string? Email { get; private set; }

    public string? About { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private VDeleSpecialistProfile()
    {
    }

    private VDeleSpecialistProfile(
        Guid userId,
        string fullName,
        Guid cityId,
        string? email)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FullName = fullName;
        CityId = cityId;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<VDeleSpecialistProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid cityId,
        string? email)
    {
        if (userId == Guid.Empty)
            return SpecialistErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return SpecialistErrors.FullNameIsEmpty();

        if (cityId == Guid.Empty)
            return SpecialistErrors.CityIdIsEmpty();

        return new VDeleSpecialistProfile(
            userId,
            fullName.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());
    }
}