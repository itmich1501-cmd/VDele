using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osnovanie.Modules.Auth.Domain;

namespace Osnovanie.Modules.Auth.Infrastructure.Configuration;

public class PhoneVerificationCodeConfiguration : IEntityTypeConfiguration<PhoneVerificationCode>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationCode> builder)
    {
        builder.ToTable("phone_verification_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .HasColumnName("is_used")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(x => x.Phone)
            .HasDatabaseName("ix_phone_verification_codes_phone");

        builder.HasIndex(x => new { x.Phone, x.CreatedAtUtc })
            .HasDatabaseName("ix_phone_verification_codes_phone_created_at");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_phone_verification_codes_expires_at_utc");
    }
}