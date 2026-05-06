using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Infrastructure.Configurations.VDele;

public sealed class VDeleSpecialistProfileConfiguration
    : IEntityTypeConfiguration<VDeleSpecialistProfile>
{
    public void Configure(EntityTypeBuilder<VDeleSpecialistProfile> builder)
    {
        builder.ToTable("specialist_profiles", "vdele");

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

        builder.Property(x => x.About)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.CityId);
    }
}