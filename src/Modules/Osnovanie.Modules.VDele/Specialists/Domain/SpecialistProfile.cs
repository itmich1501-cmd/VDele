using CSharpFunctionalExtensions;
using Osnovanie.Modules.VDele.Specialists.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.Domain;

public sealed class SpecialistProfile
{
    private SpecialistProfile(
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

    public static Result<SpecialistProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid cityId,
        string? email,
        string? about = null)
    {
        if (userId == Guid.Empty)
            return SpecialistErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return SpecialistErrors.FullNameIsEmpty();

        if (fullName.Length > 200)
            return SpecialistErrors.FullNameIsTooLong();

        if (cityId == Guid.Empty)
            return SpecialistErrors.CityIdIsEmpty();

        if (!string.IsNullOrWhiteSpace(about) && about.Length > 2000)
            return SpecialistErrors.AboutIsTooLong();

        var specialistProfile = new SpecialistProfile(
            Guid.NewGuid(),
            userId,
            fullName.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(about) ? null : about.Trim());

        return specialistProfile;
    }
}