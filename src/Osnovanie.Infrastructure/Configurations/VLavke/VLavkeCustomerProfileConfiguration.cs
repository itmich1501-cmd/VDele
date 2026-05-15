using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osnovanie.Modules.VLavke.Customers.Domain;

namespace Osnovanie.Infrastructure.Configurations.VLavke;

public sealed class VLavkeCustomerProfileConfiguration
    : IEntityTypeConfiguration<VLavkeCustomerProfile>
{
    public void Configure(EntityTypeBuilder<VLavkeCustomerProfile> builder)
    {
        builder.ToTable("customer_profiles", "vlavke");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CityId)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.CityId);
    }
}
