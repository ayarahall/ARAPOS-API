using System.Security.Claims;
using System.Text;
using Ayapos.Api.Data;
using Ayapos.Api.Options;
using Ayapos.Api.Security;
using Ayapos.Api.Services.Expenses;
using Ayapos.Api.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; 

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
                      new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// DbContext
builder.Services.AddDbContext<AyaposDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("AyaposDb")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:AyaposDb");
    cs = LocalDbConnectionStringResolver.Resolve(cs);
    opt.UseSqlServer(cs);
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
builder.Services.AddScoped<WorkforceSchemaBootstrapService>();
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
    var workforceBootstrap = scope.ServiceProvider.GetRequiredService<WorkforceSchemaBootstrapService>();
    await workforceBootstrap.EnsureSchemaAsync();

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

app.MapControllers();
app.Run();
