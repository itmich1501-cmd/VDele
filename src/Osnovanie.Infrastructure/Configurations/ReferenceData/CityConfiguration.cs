using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osnovanie.Modules.ReferenceData.Cities.Domain;

namespace Osnovanie.Infrastructure.Configurations.ReferenceData;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities", "reference_data");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.FiasId)
            .HasMaxLength(50);

        builder.Property(x => x.Oktmo)
            .HasMaxLength(20);

        builder.Property(x => x.RegionName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.RegionFiasId)
            .HasMaxLength(50);

        builder.Property(x => x.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsVisible)
            .IsRequired();

        builder.HasIndex(x => x.NormalizedName);
        builder.HasIndex(x => x.IsVisible);
        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.FiasId);
        builder.HasIndex(x => x.Oktmo);
    }
}