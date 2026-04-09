using Microsoft.EntityFrameworkCore;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Db;

public sealed class PlateGuardDbContext(DbContextOptions<PlateGuardDbContext> options) : DbContext(options)
{
    public DbSet<VehicleEntity> Vehicles => Set<VehicleEntity>();
    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<PromotionUsageEntity> PromotionUsages => Set<PromotionUsageEntity>();
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureVehicles(modelBuilder.Entity<VehicleEntity>());
        ConfigurePromotions(modelBuilder.Entity<PromotionEntity>());
        ConfigurePromotionUsages(modelBuilder.Entity<PromotionUsageEntity>());
        ConfigureSettings(modelBuilder.Entity<SettingsEntity>());
    }

    private static void ConfigureVehicles(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<VehicleEntity> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(vehicle => vehicle.Id);

        builder.Property(vehicle => vehicle.VehicleNumberRaw)
            .IsRequired();

        builder.Property(vehicle => vehicle.VehicleNumberNormalized)
            .IsRequired();

        builder.Property(vehicle => vehicle.PhoneNumber)
            .IsRequired();

        builder.Property(vehicle => vehicle.CreatedAt)
            .IsRequired();

        builder.HasIndex(vehicle => vehicle.VehicleNumberNormalized)
            .IsUnique();

        builder.HasIndex(vehicle => vehicle.PhoneNumber);
        builder.HasIndex(vehicle => vehicle.OwnerName);
    }

    private static void ConfigurePromotions(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PromotionEntity> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(promotion => promotion.Id);

        builder.Property(promotion => promotion.PromotionName)
            .IsRequired();

        builder.Property(promotion => promotion.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(promotion => promotion.CreatedAt)
            .IsRequired();

        builder.HasIndex(promotion => promotion.IsActive);
    }

    private static void ConfigurePromotionUsages(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PromotionUsageEntity> builder)
    {
        builder.ToTable("PromotionUsages");

        builder.HasKey(usage => usage.Id);

        builder.Property(usage => usage.ServiceDate)
            .IsRequired();

        builder.Property(usage => usage.CreatedAt)
            .IsRequired();

        builder.HasIndex(usage => usage.ServiceDate);
        builder.HasIndex(usage => usage.VehicleId);
        builder.HasIndex(usage => usage.PromotionId);

        builder.HasIndex(usage => new { usage.VehicleId, usage.PromotionId })
            .IsUnique();

        builder.HasOne(usage => usage.Vehicle)
            .WithMany(vehicle => vehicle.PromotionUsages)
            .HasForeignKey(usage => usage.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(usage => usage.Promotion)
            .WithMany(promotion => promotion.PromotionUsages)
            .HasForeignKey(usage => usage.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSettings(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SettingsEntity> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .ValueGeneratedNever();

        builder.Property(settings => settings.DeletePasswordHash)
            .IsRequired();

        builder.Property(settings => settings.CreatedAt)
            .IsRequired();
    }
}
