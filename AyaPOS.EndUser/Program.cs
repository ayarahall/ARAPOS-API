using Ayapos.EndUser.Components;
using Ayapos.EndUser.Services.Auth;
using Ayapos.EndUser.Services.Appointments;
using Ayapos.EndUser.Services.Cashier;
using Ayapos.EndUser.Services.Customers;
using Ayapos.EndUser.Services.Expenses;
using Ayapos.EndUser.Services.Invoices;
using Ayapos.EndUser.Services.Owner;
using Ayapos.EndUser.Services.Products;
using Ayapos.EndUser.Services.Services;
using Ayapos.EndUser.Services.TenantAdmin;
using Ayapos.EndUser.Services.Ui;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient<AuthApiClient>();
builder.Services.AddHttpClient<AppointmentsApiClient>();
builder.Services.AddHttpClient<TenantLookupApiClient>();
builder.Services.AddHttpClient<ProductsApiClient>();
builder.Services.AddHttpClient<ServicesApiClient>();
builder.Services.AddHttpClient<InvoicesApiClient>();
builder.Services.AddHttpClient<CustomersApiClient>();
builder.Services.AddHttpClient<CashierApiClient>();
builder.Services.AddHttpClient<ExpensesApiClient>();
builder.Services.AddHttpClient<PlatformAdminApiClient>();
builder.Services.AddHttpClient<TenantAdminApiClient>();
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<OwnerSessionState>();
builder.Services.AddScoped<UiLanguageState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
