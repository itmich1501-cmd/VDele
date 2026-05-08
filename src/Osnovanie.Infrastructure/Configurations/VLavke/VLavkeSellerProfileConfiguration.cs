using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Configurations.VLavke;

public sealed class VLavkeSellerProfileConfiguration
    : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("seller_profiles", "vlavke");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.MainCityId)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.CompanyName)
            .HasMaxLength(300);

        builder.Property(x => x.Inn)
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.MainCityId);
    }
}