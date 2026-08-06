using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Db;

public sealed class PlateGuardDbContext(DbContextOptions<PlateGuardDbContext> options) : DbContext(options)
{
    public DbSet<VehicleEntity> Vehicles => Set<VehicleEntity>();
    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<PromotionUsageEntity> PromotionUsages => Set<PromotionUsageEntity>();
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    public DbSet<SyncConflictEntity> SyncConflicts => Set<SyncConflictEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureVehicles(modelBuilder.Entity<VehicleEntity>());
        ConfigurePromotions(modelBuilder.Entity<PromotionEntity>());
        ConfigurePromotionUsages(modelBuilder.Entity<PromotionUsageEntity>());
        ConfigureSettings(modelBuilder.Entity<SettingsEntity>());
        ConfigureSyncConflicts(modelBuilder.Entity<SyncConflictEntity>());
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

        builder.Property(vehicle => vehicle.SyncId)
            .IsRequired();

        builder.Property(vehicle => vehicle.IsDirty)
            .IsRequired();

        builder.Property(vehicle => vehicle.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(vehicle => vehicle.VehicleNumberNormalized)
            .IsUnique();

        builder.HasIndex(vehicle => vehicle.SyncId)
            .IsUnique();

        builder.HasIndex(vehicle => vehicle.PhoneNumber);
        builder.HasIndex(vehicle => vehicle.OwnerName);

        builder.HasQueryFilter(vehicle => !vehicle.IsDeleted);
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

        builder.Property(promotion => promotion.SyncId)
            .IsRequired();

        builder.Property(promotion => promotion.IsDirty)
            .IsRequired();

        builder.Property(promotion => promotion.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(promotion => promotion.IsActive);

        builder.HasIndex(promotion => promotion.SyncId)
            .IsUnique();

        builder.HasQueryFilter(promotion => !promotion.IsDeleted);
    }

    private static void ConfigurePromotionUsages(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PromotionUsageEntity> builder)
    {
        builder.ToTable("PromotionUsages");

        builder.HasKey(usage => usage.Id);

        builder.Property(usage => usage.ServiceDate)
            .IsRequired();

        builder.Property(usage => usage.CreatedAt)
            .IsRequired();

        builder.Property(usage => usage.SyncId)
            .IsRequired();

        builder.Property(usage => usage.IsDirty)
            .IsRequired();

        builder.Property(usage => usage.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(usage => usage.ServiceDate);
        builder.HasIndex(usage => usage.VehicleId);
        builder.HasIndex(usage => usage.PromotionId);

        builder.HasIndex(usage => new { usage.VehicleId, usage.PromotionId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

        builder.HasIndex(usage => usage.SyncId)
            .IsUnique();

        builder.HasOne(usage => usage.Vehicle)
            .WithMany(vehicle => vehicle.PromotionUsages)
            .HasForeignKey(usage => usage.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(usage => usage.Promotion)
            .WithMany(promotion => promotion.PromotionUsages)
            .HasForeignKey(usage => usage.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(usage => !usage.IsDeleted);
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

    private static void ConfigureSyncConflicts(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SyncConflictEntity> builder)
    {
        builder.ToTable("SyncConflicts");
        builder.HasKey(conflict => conflict.Id);
        builder.Property(conflict => conflict.Kind).IsRequired();
        builder.Property(conflict => conflict.Details).IsRequired();
        builder.Property(conflict => conflict.DetectedAtUtc).IsRequired();
        builder.Property(conflict => conflict.IsAcknowledged).IsRequired().HasDefaultValue(false);
    }
}
