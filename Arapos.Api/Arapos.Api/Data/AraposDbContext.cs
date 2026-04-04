using System;
using System.Collections.Generic;
using Arapos.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arapos.Api.Data;

public partial class AraposDbContext : DbContext
{
    public AraposDbContext(DbContextOptions<AraposDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentItem> AppointmentItems { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<BranchSetting> BranchSettings { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerLicense> CustomerLicenses { get; set; }

    public virtual DbSet<InventoryMove> InventoryMoves { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<InvoiceSequence> InvoiceSequences { get; set; }

    public virtual DbSet<License> Licenses { get; set; }

    public virtual DbSet<LicenseActivation> LicenseActivations { get; set; }

    public virtual DbSet<ManagerApproval> ManagerApprovals { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductPrice> ProductPrices { get; set; }

    public virtual DbSet<ProductStockSnapshot> ProductStockSnapshots { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServicePrice> ServicePrices { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserPin> UserPins { get; set; }

    public virtual DbSet<UserPinsHistory> UserPinsHistories { get; set; }

    public virtual DbSet<vw_ProductActivePrice> vw_ProductActivePrices { get; set; }
    public DbSet<CashierSession> CashierSessions => Set<CashierSession>();
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Arabic_100_CI_AI_SC");

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key).HasMaxLength(50);
            entity.Property(e => e.Value).HasMaxLength(200);
        });

        modelBuilder.Entity<Invoice>()
    .Property(x => x.RowVersion)
    .IsRowVersion();

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(e => new { e.StaffId, e.StartAt }, "IX_Appointments_Staff_StartAt");

            entity.HasIndex(e => e.StartAt, "IX_Appointments_StartAt");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EndAt).HasPrecision(0);
            entity.Property(e => e.Notes).HasMaxLength(400);
            entity.Property(e => e.StartAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("scheduled");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Appointments_User");

            entity.HasOne(d => d.Customer).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Appointments_Customer");

            entity.HasOne(d => d.Staff).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_Appointments_Staff");
        });

        modelBuilder.Entity<AppointmentItem>(entity =>
        {
            entity.HasIndex(e => e.AppointmentId, "IX_AppointmentItems_AppointmentId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("AED")
                .IsFixedLength();
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.LineTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Name).HasMaxLength(120);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentItems)
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK_AppointmentItems_Appointment");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("BranchesHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => new { e.TenantId, e.Code }, "UQ_Branches_Tenant_Code").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "UQ_Branches_Tenant_Id").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Branches)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Branches_Tenants");
        });

        modelBuilder.Entity<BranchSetting>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.BranchId });

            entity.Property(e => e.AllowInvoiceDiscount).HasDefaultValue(true);
            entity.Property(e => e.AllowLineDiscount).HasDefaultValue(true);
            entity.Property(e => e.MaxCashierDiscountPct).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RequireManagerForPriceOverride).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Branch).WithOne(p => p.BranchSetting)
                .HasPrincipalKey<Branch>(p => new { p.TenantId, p.Id })
                .HasForeignKey<BranchSetting>(d => new { d.TenantId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchSettings_Branches");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.FullName, "IX_Customers_FullName");

            entity.HasIndex(e => e.Phone, "IX_Customers_Phone");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(120);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.Phone).HasMaxLength(30);
        });

        modelBuilder.Entity<CustomerLicense>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("CustomerLicensesHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => new { e.TenantId, e.CustomerId }, "IX_CustomerLicenses_Tenant_Customer");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LicenseType).HasMaxLength(80);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.CreatedInvoice).WithMany(p => p.CustomerLicenses)
                .HasForeignKey(d => d.CreatedInvoiceId)
                .HasConstraintName("FK_CustomerLicenses_Invoices");

            entity.HasOne(d => d.Tenant).WithMany(p => p.CustomerLicenses)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerLicenses_Tenants");
        });

        modelBuilder.Entity<InventoryMove>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.CreatedAt }, "IX_InvMoves_Tenant_Branch_CreatedAt");

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "IX_InventoryMoves_Product_Time").IsDescending(false, true);

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MoveType).HasMaxLength(30);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.RefType).HasMaxLength(30);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.InventoryMoves)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_InventoryMoves_User");

            entity.HasOne(d => d.Product).WithMany(p => p.InventoryMoves)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryMoves_Product");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("InvoicesHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.CreatedAt }, "IX_Invoices_Tenant_Branch_CreatedAt");

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.InvoiceNo }, "UQ_Invoices_Tenant_Branch_InvoiceNo").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.InvoiceNo }, "UQ_Invoices_Tenant_Branch_No").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Id }, "UQ_Invoices_Tenant_Id").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InvoiceCode).HasMaxLength(80);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ISSUED");

            entity.HasOne(d => d.Branch).WithMany(p => p.InvoiceBranches)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Branches");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Tenants");

            entity.HasOne(d => d.BranchNavigation).WithMany(p => p.InvoiceBranchNavigations)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_Branches_Tenant");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("InvoiceItemsHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => e.InvoiceId, "IX_InvoiceItems_InvoiceId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.DiscountPct).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiscountReason).HasMaxLength(200);
            entity.Property(e => e.ItemType).HasMaxLength(10);
            entity.Property(e => e.NameSnapshot).HasMaxLength(300);
            entity.Property(e => e.PriceOverrideReason).HasMaxLength(200);
            entity.Property(e => e.Qty).HasDefaultValue(1);

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_Invoices");

            entity.HasOne(d => d.Product).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_InvoiceItems_Products");

            entity.HasOne(d => d.Service).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_InvoiceItems_Services");

            entity.HasOne(d => d.Tenant).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_Tenants");
        });

        modelBuilder.Entity<InvoiceSequence>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.BranchId });

            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("InvoiceSequencesHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.Property(e => e.NextNumber).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Branch).WithMany(p => p.InvoiceSequenceBranches)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceSequences_Branches");

            entity.HasOne(d => d.Tenant).WithMany(p => p.InvoiceSequences)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceSequences_Tenants");

            entity.HasOne(d => d.BranchNavigation).WithOne(p => p.InvoiceSequenceBranchNavigation)
                .HasPrincipalKey<Branch>(p => new { p.TenantId, p.Id })
                .HasForeignKey<InvoiceSequence>(d => new { d.TenantId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceSequences_Branches_Tenant");
        });

        modelBuilder.Entity<License>(entity =>
        {
            entity.HasIndex(e => e.LicenseKey, "UX_Licenses_LicenseKey").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.LicenseKey).HasMaxLength(80);
            entity.Property(e => e.Notes).HasMaxLength(300);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
        });

        modelBuilder.Entity<LicenseActivation>(entity =>
        {
            entity.HasIndex(e => new { e.LicenseId, e.LastSeenAt }, "IX_LA_License_LastSeen").IsDescending(false, true);

            entity.HasIndex(e => new { e.LicenseId, e.DeviceId }, "UX_LA_License_Device").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ActivatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DeviceId).HasMaxLength(80);
            entity.Property(e => e.LastSeenAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.License).WithMany(p => p.LicenseActivations)
                .HasForeignKey(d => d.LicenseId)
                .HasConstraintName("FK_LA_License");
        });

        modelBuilder.Entity<ManagerApproval>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.RefType, e.RefId, e.ApprovedAt }, "IX_ManagerApprovals_Ref").IsDescending(false, false, false, false, true);

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ApprovalType).HasMaxLength(30);
            entity.Property(e => e.ApprovedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.RefType).HasMaxLength(30);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("PaymentsHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => e.InvoiceId, "IX_Payments_InvoiceId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Method).HasMaxLength(10);
            entity.Property(e => e.PaidAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Reference).HasMaxLength(100);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Payments)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Tenants");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.InvoiceId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Invoices_Tenant");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("ProductsHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => e.IsActive, "IX_Products_Active");

            entity.HasIndex(e => e.Barcode, "IX_Products_Barcode");

            entity.HasIndex(e => new { e.TenantId, e.BranchId }, "IX_Products_Tenant_Branch");

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.Barcode }, "UQ_Products_Tenant_Branch_Barcode")
                .IsUnique()
                .HasFilter("([Barcode] IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.Sku }, "UQ_Products_Tenant_Branch_Sku")
                .IsUnique()
                .HasFilter("([Sku] IS NOT NULL)");

            entity.HasIndex(e => e.Sku, "UX_Products_Sku_NotNull")
                .IsUnique()
                .HasFilter("([Sku] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Barcode).HasMaxLength(60);
            entity.Property(e => e.CostPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("AED")
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NameAr).HasMaxLength(120);
            entity.Property(e => e.NameEn).HasMaxLength(120);
            entity.Property(e => e.SellPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Sku).HasMaxLength(50);
            entity.Property(e => e.TrackInventory).HasDefaultValue(true);
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasDefaultValue("pcs");

            entity.HasOne(d => d.Branch).WithMany(p => p.Products)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Branches_Tenant");
        });

        modelBuilder.Entity<ProductPrice>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("ProductPricesHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ProductId, e.IsActive }, "IX_ProductPrices_Lookup");

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ProductId }, "UX_ProductPrices_Active")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ProductStockSnapshot>(entity =>
        {
            entity.HasKey(e => e.ProductId);

            entity.ToTable("ProductStockSnapshot");

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ProductId }, "UQ_Stock_Tenant_Branch_Product").IsUnique();

            entity.Property(e => e.ProductId).ValueGeneratedNever();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Product).WithOne(p => p.ProductStockSnapshot)
                .HasForeignKey<ProductStockSnapshot>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductStockSnapshot_Product");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_Services_Active");

            entity.HasIndex(e => new { e.TenantId, e.BranchId }, "IX_Services_Tenant_Branch");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("AED")
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NameAr).HasMaxLength(120);
            entity.Property(e => e.NameEn).HasMaxLength(120);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("ServicePricesHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.IsActive }, "IX_ServicePrices_Branch_Active");

            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.ServiceId }, "UQ_ServicePrices_Tenant_Branch_Service").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Service).WithMany(p => p.ServicePrices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServicePrices_Services");

            entity.HasOne(d => d.Tenant).WithMany(p => p.ServicePrices)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServicePrices_Tenants");

            entity.HasOne(d => d.Branch).WithMany(p => p.ServicePrices)
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .HasForeignKey(d => new { d.TenantId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServicePrices_Branches_Tenant");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasIndex(e => e.FullName, "IX_Staff_FullName");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JobTitle).HasMaxLength(40);
            entity.Property(e => e.Phone).HasMaxLength(30);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("TenantsHistory", "dbo");
                        ttb
                            .HasPeriodStart("SysStartTime")
                            .HasColumnName("SysStartTime");
                        ttb
                            .HasPeriodEnd("SysEndTime")
                            .HasColumnName("SysEndTime");
                    }));

            entity.HasIndex(e => e.Slug, "UQ_Tenants_Slug").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username, "UX_Users_Username").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PinHash).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<UserPin>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.TenantId }).HasName("PK_UserPins_UserTenant");

            entity.Property(e => e.Algo)
                .HasMaxLength(30)
                .HasDefaultValue("SHA2_256");
            entity.Property(e => e.Iterations).HasDefaultValue(1);
            entity.Property(e => e.PinHash).HasMaxLength(64);
            entity.Property(e => e.PinSalt).HasMaxLength(32);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.User).WithMany(p => p.UserPins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPins_Users");
        });

        modelBuilder.Entity<UserPinsHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("UserPinsHistory");

            entity.HasIndex(e => new { e.SysEndTime, e.SysStartTime }, "ix_UserPinsHistory").IsClustered();

            entity.Property(e => e.Algo).HasMaxLength(30);
            entity.Property(e => e.PinHash).HasMaxLength(64);
            entity.Property(e => e.PinSalt).HasMaxLength(32);
        });

        modelBuilder.Entity<vw_ProductActivePrice>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ProductActivePrices");

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
