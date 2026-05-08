using CSharpFunctionalExtensions;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.Domain;

public sealed class VDeleSpecialistProfile
{
    private VDeleSpecialistProfile(
        Guid id,
        Guid userId,
        string fullName,
        Guid cityId,
        string? email,
        string? about)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        CityId = cityId;
        Email = email;
        About = about;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; }

    public Guid CityId { get; private set; }

    public string? Email { get; private set; }

    public string? About { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Result<VDeleSpecialistProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid cityId,
        string? email,
        string? about = null)
    {
        if (userId == Guid.Empty)
            return VDeleSpecialistErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return VDeleSpecialistErrors.FullNameIsEmpty();

        if (fullName.Length > 200)
            return VDeleSpecialistErrors.FullNameIsTooLong();

        if (cityId == Guid.Empty)
            return VDeleSpecialistErrors.CityIdIsEmpty();

        if (!string.IsNullOrWhiteSpace(about) && about.Length > 2000)
            return VDeleSpecialistErrors.AboutIsTooLong();

        var specialistProfile = new VDeleSpecialistProfile(
            Guid.NewGuid(),
            userId,
            fullName.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(about) ? null : about.Trim());

        return specialistProfile;
    }
}