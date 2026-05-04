using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Osnovanie.Modules.Auth.Domain;
using Osnovanie.Modules.Auth.Domain.Constants;

namespace Osnovanie.Modules.Auth.Infrastructure.Configuration;

public class UserAccessConfiguration : IEntityTypeConfiguration<UserAccess>
{
    public void Configure(EntityTypeBuilder<UserAccess> builder)
    {
        builder.ToTable("user_access");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Id,
                value => new UserAccessId(value)
            );

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.ApplicationCode)
            .HasColumnName("application_code")
            .IsRequired()
            .HasMaxLength(MaxLengths.ApplicationCode);

        builder.Property(x => x.RoleCode)
            .HasColumnName("role_code")
            .IsRequired()
            .HasMaxLength(MaxLengths.RoleCode);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => new { x.UserId, x.ApplicationCode, x.RoleCode })
            .IsUnique();
    }
}