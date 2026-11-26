using Kitaphane.Application.Auth;
using Kitaphane.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kitaphane.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.Name).IsRequired().HasMaxLength(AuthRules.NameMaxLength);
        builder.Property(user => user.ProfileImage).HasMaxLength(2048);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("PendingRegistrations");
        builder.HasKey(registration => registration.Id);
        builder.Property(registration => registration.Email).IsRequired().HasMaxLength(AuthRules.EmailMaxLength);
        builder.Property(registration => registration.NormalizedEmail).IsRequired().HasMaxLength(AuthRules.EmailMaxLength);
        builder.Property(registration => registration.Name).IsRequired().HasMaxLength(AuthRules.NameMaxLength);
        builder.Property(registration => registration.PasswordHash).IsRequired();
        builder.Property(registration => registration.CodeHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(registration => registration.NormalizedEmail).IsUnique();
    }
}

internal sealed class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
    {
        builder.ToTable("PasswordResetCodes");
        builder.HasKey(reset => reset.Id);
        builder.Property(reset => reset.CodeHash).HasMaxLength(64);
        builder.Property(reset => reset.ResetTokenHash).HasMaxLength(64);
        builder.HasIndex(reset => reset.UserId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(reset => reset.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
