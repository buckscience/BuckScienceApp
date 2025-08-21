using BuckScience.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);

// Authentication: let Microsoft.Identity.Web register "Cookies" + "OpenIdConnect"
var auth = builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);
auth.AddMicrosoftIdentityWebApp(options =>
{
    // IMPORTANT: section name must match your appsettings key exactly
    builder.Configuration.Bind("AzureAdB2C", options);
});

// Configure the already-registered "Cookies" scheme instead of adding it again
builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.Name = ".BuckScience.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
    });

// Microsoft Identity UI endpoints (/Account/SignIn etc.)
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

// Authorization: require auth by default; keep your AllowAnonymous policy
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
    options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(_ => true));
});

// Sessions (optional)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// If you use session in auth events, keep session before auth
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();