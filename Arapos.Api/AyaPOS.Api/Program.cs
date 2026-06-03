using System.Security.Claims;
using System.Text;
using Ayapos.Api.Data;
using Ayapos.Api.Options;
using Ayapos.Api.Security;
using Ayapos.Api.Services.Expenses;
using Ayapos.Api.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; 

// Npgsql: treat DateTime.Kind=Unspecified as UTC (avoids "cannot write Unspecified to timestamptz" errors)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                      {
                          var host = new Uri(origin).Host;
                          return host == "localhost"
                              || host == "127.0.0.1"
                              || host.EndsWith(".vercel.app")
                              || host.EndsWith(".onrender.com");
                      })
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// DbContext — SQLSERVER_URL takes priority (Azure SQL on Render, won't be overridden by Render's managed postgres).
// Falls back to DATABASE_URL (Render-managed postgres://) then appsettings.
builder.Services.AddDbContext<AyaposDbContext>(opt =>
{
    var raw = Environment.GetEnvironmentVariable("SQLSERVER_URL")
              ?? Environment.GetEnvironmentVariable("DATABASE_URL")
              ?? builder.Configuration.GetConnectionString("AyaposDb")
              ?? throw new InvalidOperationException("Missing DB connection string.");

    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(raw);
        var parts = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(parts[0]);
        var pass = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        var cs = $"Host={uri.Host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";

        opt.UseNpgsql(cs);
        opt.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        return;
    }

    opt.UseSqlServer(raw);
    opt.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Tenant context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<OwnerBootstrapOptions>(builder.Configuration.GetSection("OwnerBootstrap"));
builder.Services.Configure<OpenAiVisionOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PasswordHasherService>();
builder.Services.AddScoped<OwnerBootstrapService>();
builder.Services.AddHttpClient<IExpenseReceiptAnalyzer, OpenAiExpenseReceiptAnalyzer>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing Jwt config.");

if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SigningKey)),

            // 👇 هذول مهمين جدًا
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

// Authorization Policies (OWNER/ADMIN/CASHIER + platform/tenant)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.PlatformOnly, policy =>
    {
        policy.RequireClaim("scope", "platform");
    });

    options.AddPolicy(AuthPolicies.PlatformOwner, policy =>
    {
        policy.RequireClaim("scope", "platform");
        policy.RequireRole("OWNER");
    });

    options.AddPolicy(AuthPolicies.TenantCashier, policy =>
    {
        policy.RequireClaim("scope", "tenant");
        policy.RequireRole("CASHIER", "HR", "BRANCH_MANAGER", "TENANT", "ADMIN", "OWNER");
    });

    options.AddPolicy(AuthPolicies.TenantAdmin, policy =>
    {
        policy.RequireClaim("scope", "tenant");
        policy.RequireRole("TENANT", "ADMIN", "OWNER");
    });

    options.AddPolicy(AuthPolicies.TenantOwner, policy =>
    {
        policy.RequireClaim("scope", "tenant");
        policy.RequireRole("OWNER");
    });

    options.AddPolicy(AuthPolicies.TenantOnly, policy =>
    {
        policy.RequireClaim("scope", "tenant");
    });
});

// Swagger + Bearer


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AYAPOS API", Version = "v1" });

    // 🔹 Bearer definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token only"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 🔹 Branch header (الذي أضفته سابقاً)
    c.AddSecurityDefinition("X-Branch-Id", new OpenApiSecurityScheme
    {
        Name = "X-Branch-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Branch Id GUID"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-Branch-Id"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
app.UseCors("AllowReact");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AyaposDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.CanConnectAsync();
        logger.LogInformation("Database connected. Provider: {Provider}", db.Database.ProviderName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connection failed at startup — check SQLSERVER_URL or DATABASE_URL. Provider: {Provider}", db.Database.ProviderName);
        // Do not crash — individual requests will fail with a clear error
    }

    // Apply any pending EF Core migrations automatically on startup
    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "EF Core migration failed — schema may be out of date.");
    }

    var isPostgres = db.Database.ProviderName?.Contains("Npgsql") == true;
    if (isPostgres)
    {
        // RowVersion triggers — EF migration scaffolded bytea NOT NULL but no trigger/default.
        // gen_random_bytes(8) produces a unique value on every INSERT/UPDATE.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE FUNCTION update_rowversion() RETURNS trigger AS $$
            BEGIN
                NEW."RowVersion" := substring(
                    sha256(CAST(clock_timestamp() AS text)::bytea || CAST(random() AS text)::bytea)
                    FROM 1 FOR 8
                );
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DO $do$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'cashiersessions_rowversion_trg') THEN
                    CREATE TRIGGER cashiersessions_rowversion_trg
                    BEFORE INSERT OR UPDATE ON "CashierSessions"
                    FOR EACH ROW EXECUTE FUNCTION update_rowversion();
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'refunds_rowversion_trg') THEN
                    CREATE TRIGGER refunds_rowversion_trg
                    BEFORE INSERT OR UPDATE ON "Refunds"
                    FOR EACH ROW EXECUTE FUNCTION update_rowversion();
                END IF;
            END;
            $do$;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "BranchSettings"
                ADD COLUMN IF NOT EXISTS "AppointmentsRequireCustomer" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "AppointmentsPreventOverlap" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "AppointmentsAutoNoShow" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "AppointmentsCheckInCreatesInvoice" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "AppointmentsAllowNoShow" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "AppointmentsAllowCancel" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "ExpensesRequireApproval" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "ExpensesDeductCash" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "ExpensesNotifyApprovers" boolean NOT NULL DEFAULT true,
                ADD COLUMN IF NOT EXISTS "ExpensesAllowAiAssist" boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS "PosRequirePaymentReference" boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS "PosRequireAppointment" boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS "PosAutoPrintReceipt" boolean NOT NULL DEFAULT false,
                ADD COLUMN IF NOT EXISTS "PosAllowMultipleInvoiceTabs" boolean NOT NULL DEFAULT true;
            """);

        // Link invoices to appointments for employee revenue reporting
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "Invoices"
                ADD COLUMN IF NOT EXISTS "AppointmentId" uuid NULL;
            """);
    }
    else
    {
        foreach (var (col, def) in new (string, string)[]
        {
            ("AppointmentsRequireCustomer", "1"), ("AppointmentsPreventOverlap", "1"),
            ("AppointmentsAutoNoShow", "1"), ("AppointmentsCheckInCreatesInvoice", "1"),
            ("AppointmentsAllowNoShow", "1"), ("AppointmentsAllowCancel", "1"),
            ("ExpensesRequireApproval", "1"), ("ExpensesDeductCash", "1"),
            ("ExpensesNotifyApprovers", "1"), ("ExpensesAllowAiAssist", "0"),
            ("PosRequirePaymentReference", "0"), ("PosRequireAppointment", "0"),
            ("PosAutoPrintReceipt", "0"), ("PosAllowMultipleInvoiceTabs", "1"),
        })
        {
            await db.Database.ExecuteSqlRawAsync(
                $"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BranchSettings') AND name = '{col}') " +
                $"ALTER TABLE BranchSettings ADD [{col}] BIT NOT NULL DEFAULT {def};");
        }
    }

    var ownerBootstrap = scope.ServiceProvider.GetRequiredService<OwnerBootstrapService>();
    await ownerBootstrap.EnsureOwnerAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();

// Tenant routing middleware (مرة واحدة فقط)
app.UseMiddleware<TenantRouteMiddleware>();

app.UseAuthorization();

// Branch middleware (مرة واحدة فقط) - إذا عندك BranchMiddleware فعلاً استخدمي واحد فقط
app.UseMiddleware<BranchMiddleware>();

app.MapGet("/api/health", () => Results.Ok(new { ok = true, nowUtc = DateTime.UtcNow }))
   .AllowAnonymous();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new { status = "ok", service = "AyaPOS API" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
