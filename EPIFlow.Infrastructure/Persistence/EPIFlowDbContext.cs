using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Application.Common.Interfaces;
using EPIFlow.Domain.Common;
using EPIFlow.Domain.Entities;
using EPIFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EPIFlow.Infrastructure.Persistence;

public class EPIFlowDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly Guid? _tenantId;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EPIFlowDbContext(
        DbContextOptions<EPIFlowDbContext> options,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : base(options)
    {
        _tenantId = tenantProvider.GetTenantId();
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EpiType> EpiTypes => Set<EpiType>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<EpiDelivery> EpiDeliveries => Set<EpiDelivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<TenantPayment> TenantPayments => Set<TenantPayment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<Tenant>().HasQueryFilter(t => !t.IsDeleted);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        ApplyGlobalQueryFilters(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var currentUser = _currentUserService.GetUserName() ?? "system";
        var currentUserId = _currentUserService.GetUserId();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (_tenantId.HasValue && entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = _tenantId.Value;
                    }

                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }
    }

    private void ApplyGlobalQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.ClrType == typeof(Tenant))
            {
                continue;
            }

            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(EPIFlowDbContext)
                .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, new object[] { builder });
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : AuditableEntity
    {
        Guid? tenantId = _tenantId;

        if (tenantId.HasValue)
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == tenantId.Value);
        }
        else
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
