using CSharpFunctionalExtensions;
using Osnovanie.Modules.VDele.Customers.ErrorDefinitions;
using Osnovanie.Shared;

namespace Osnovanie.Modules.VDele.Customers.Domain;

public sealed class CustomerProfile
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public Guid CityId { get; private set; }

    public string? Email { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private CustomerProfile()
    {
    }

    private CustomerProfile(
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

    public static Result<CustomerProfile, Error> Create(
        Guid userId,
        string fullName,
        Guid cityId,
        string? email)
    {
        if (userId == Guid.Empty)
            return CustomerErrors.UserIdIsEmpty();

        if (string.IsNullOrWhiteSpace(fullName))
            return CustomerErrors.FullNameIsEmpty();

        if (cityId == Guid.Empty)
            return CustomerErrors.CityIdIsEmpty();

        return new CustomerProfile(
            userId,
            fullName.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(email) ? null : email.Trim());
    }
}