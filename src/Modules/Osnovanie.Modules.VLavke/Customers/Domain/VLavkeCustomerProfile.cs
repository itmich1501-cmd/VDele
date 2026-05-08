using CSharpFunctionalExtensions;
using Osnovanie.Modules.VLavke.Customers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VLavke.Customers.Domain;

public sealed class VLavkeCustomerProfile
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public Guid CityId { get; private set; }

    public string? Email { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private VLavkeCustomerProfile()
    {
    }

    private VLavkeCustomerProfile(
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

    public static Result<VLavkeCustomerProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid cityId,
        string? email)
    {
        if (userId == Guid.Empty)
            return VLavkeCustomerErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return VLavkeCustomerErrors.FullNameIsEmpty();

        if (cityId == Guid.Empty)
            return VLavkeCustomerErrors.CityIdIsEmpty();

        return new VLavkeCustomerProfile(
            userId,
            fullName.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());
    }
}