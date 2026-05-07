using CSharpFunctionalExtensions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Specialists.Domain;

public sealed class SpecialistProfile
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public Guid CityId { get; private set; }

    public string? Email { get; private set; }

    public string? About { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private SpecialistProfile()
    {
    }

    private SpecialistProfile(
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

    public static Result<SpecialistProfile, Error> Create(
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

        return new SpecialistProfile(
            userId,
            fullName.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());
    }
}