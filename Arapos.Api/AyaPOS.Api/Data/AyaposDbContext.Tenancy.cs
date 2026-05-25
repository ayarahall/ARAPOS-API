using System.Linq.Expressions;
using Ayapos.Api.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Ayapos.Api.Data;

public partial class AyaposDbContext
{
    private readonly ITenantContext? _tenant;

    public AyaposDbContext(DbContextOptions<AyaposDbContext> options, ITenantContext tenant)
        : this(options)
    {
        _tenant = tenant;
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ApplyTenantFilters(modelBuilder);
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        if (_tenant is null) return;

        foreach (var et in modelBuilder.Model.GetEntityTypes())
        {
            var tenantProp = et.FindProperty("TenantId");
            if (tenantProp is null) continue;
            if (tenantProp.ClrType != typeof(Guid)) continue;

            modelBuilder.Entity(et.ClrType).HasQueryFilter(BuildTenantFilter(et.ClrType));
        }
    }

    private LambdaExpression BuildTenantFilter(Type entityClrType)
    {
        var param = Expression.Parameter(entityClrType, "e");

        var tenantField = Expression.Field(Expression.Constant(this), "_tenant");
        var isPlatform = Expression.Property(tenantField, nameof(ITenantContext.IsPlatform));
        var tenantIdNullable = Expression.Property(tenantField, nameof(ITenantContext.TenantId));

        var hasTenantId = Expression.NotEqual(
            tenantIdNullable,
            Expression.Constant(null, typeof(Guid?))
        );

        var efTenantId = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            new[] { typeof(Guid) },
            param,
            Expression.Constant("TenantId")
        );

        var tenantIdValue = Expression.Property(tenantIdNullable, "Value");
        var equalsTenant = Expression.Equal(efTenantId, tenantIdValue);

        var tenantClause = Expression.AndAlso(hasTenantId, equalsTenant);
        var body = Expression.OrElse(isPlatform, tenantClause);

        return Expression.Lambda(body, param);
    }
}
