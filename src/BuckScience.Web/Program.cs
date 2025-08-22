using BuckScience.Application.Abstractions.Auth;
using BuckScience.Infrastructure;
using BuckScience.Web.Auth;
using BuckScience.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);

// Register CurrentUserService for dependency injection
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Authentication: set defaults, then bind from AzureADB2C section
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureADB2C"));

// Enrich identity with app_user_id at sign-in (applies to your default OIDC handler)
builder.Services.PostConfigureAll<OpenIdConnectOptions>(OidcClaimsEnricher.Configure);

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    cookie =>
    {
        cookie.Cookie.Name = ".BuckScience.Auth";
        cookie.Cookie.HttpOnly = true;
        cookie.SlidingExpiration = true;
        cookie.Cookie.SameSite = SameSiteMode.None;
        cookie.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(20);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Top-of-pipeline diagnostics: logs every request that reaches this process
app.Use(async (ctx, next) =>
{
    Console.WriteLine($"[Top] -> {ctx.Request.Method} {ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}{ctx.Request.QueryString}");
    try
    {
        await next();
    }
    finally
    {
        Console.WriteLine($"[Top] <- {ctx.Response.StatusCode} for {ctx.Request.Path}");
    }
});

app.UseRouting();

// Route diagnostics: see the selected endpoint before/after the rest of the pipeline
app.Use(async (ctx, next) =>
{
    var epBefore = ctx.GetEndpoint();
    Console.WriteLine($"[RouteDiag:before] {ctx.Request.Method} {ctx.Request.Path} -> {epBefore?.DisplayName ?? "(no endpoint yet)"}");
    await next();
    var epAfter = ctx.GetEndpoint();
    Console.WriteLine($"[RouteDiag:after]  {ctx.Request.Method} {ctx.Request.Path} -> {epAfter?.DisplayName ?? "(no endpoint)"}");
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Resolve DB user id once per request (must be BEFORE SetupFlow)
app.UseResolveCurrentUser();

// Enforce onboarding after current user is resolved
app.UseMiddleware<SetupFlowMiddleware>();

// Place these after app.UseAuthorization() and before app.Run()
app.MapGet("/__resolve/geo", ([FromServices] NetTopologySuite.Geometries.GeometryFactory g) =>
{
    return Results.Text($"GeometryFactory: {g.GetType().FullName}");
}).AllowAnonymous();

app.MapGet("/__resolve/appdb", ([FromServices] BuckScience.Application.Abstractions.IAppDbContext db) =>
{
    // Do not query. Just resolving should be instant.
    return Results.Text($"IAppDbContext: {db.GetType().FullName}");
}).AllowAnonymous();

app.MapGet("/__resolve/current", ([FromServices] BuckScience.Application.Abstractions.Auth.ICurrentUserService u) =>
{
    // Do not access HttpContext here; just verify it resolves.
    return Results.Text($"ICurrentUserService: {u.GetType().FullName}");
}).AllowAnonymous();

// Optional: resolve all in a fresh scope (helps catch cycles)
app.MapGet("/__resolve/all", (IServiceProvider sp) =>
{
    using var scope = sp.CreateScope();
    var s = scope.ServiceProvider;
    var g = s.GetRequiredService<NetTopologySuite.Geometries.GeometryFactory>();
    var db = s.GetRequiredService<BuckScience.Application.Abstractions.IAppDbContext>();
    var u = s.GetRequiredService<BuckScience.Application.Abstractions.Auth.ICurrentUserService>();
    return Results.Text("Resolved all three successfully");
}).AllowAnonymous();

app.MapGet("/__resolve/userid", (ICurrentUserService u) =>
    Results.Text($"IsAuth={u.IsAuthenticated}, Id={u.Id}, External={u.AzureEntraB2CId}"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();