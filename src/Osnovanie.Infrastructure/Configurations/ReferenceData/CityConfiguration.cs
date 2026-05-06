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
        
        var yakutsk = City.Create(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Якутск",
            "Республика Саха (Якутия)",
            "Asia/Yakutsk",
            fiasId: "d0e68d1f-0f0c-4f7d-9c87-68c4b2c2c6a1",
            oktmo: "98701000",
            sortOrder: 3).Value;

        var vladivostok = City.Create(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Владивосток",
            "Приморский край",
            "Asia/Vladivostok",
            fiasId: "1b8a7d3d-2b9f-4c1f-8f2a-1c5f6c7a9e2b",
            oktmo: "05701000",
            sortOrder: 4).Value;

        builder.HasData(yakutsk, vladivostok);
    }
}