using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Analytics;
using BuckScience.Application.Photos;
using BuckScience.Infrastructure;
using BuckScience.Web.Auth;
using BuckScience.Web.Middleware;
using BuckScience.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
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

// Register BuckLens Analytics Service
builder.Services.AddScoped<BuckLensAnalyticsService>();

// Register Azure Blob Storage service
var storageConnectionString = builder.Configuration.GetConnectionString("StorageConnectionString");
if (!string.IsNullOrEmpty(storageConnectionString))
{
    // Register the blob storage service directly as IBlobStorageService
    builder.Services.AddSingleton<IBlobStorageService>(provider => 
    {
        var logger = provider.GetRequiredService<ILogger<BlobStorageService>>();
        return new BlobStorageService(storageConnectionString, logger);
    });
}
else
{
    throw new InvalidOperationException("StorageConnectionString is required in configuration.");
}

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
        
        // Configure login path to prevent automatic redirects for subscription routes
        // This is especially important for AJAX calls and API endpoints
        cookie.LoginPath = "/Account/SignIn";
        cookie.AccessDeniedPath = "/Account/AccessDenied";
        
        // Add events to log authentication redirects for debugging
        cookie.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogWarning("CookieAuth: Redirecting to login for {Method} {Path} - RequestedWith: {RequestedWith}, User.IsAuthenticated: {IsAuth}", 
                    context.Request.Method, context.Request.Path, context.Request.Headers["X-Requested-With"].ToString(),
                    context.HttpContext.User.Identity?.IsAuthenticated);
                
                // For subscription routes, don't redirect to login - return 401 instead
                if (context.Request.Path.StartsWithSegments("/subscription", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Path.StartsWithSegments("/Subscription", StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogWarning("CookieAuth: BLOCKING login redirect for subscription path {Path} - returning 401 instead", context.Request.Path);
                    context.Response.StatusCode = 401;
                    context.Response.Headers["X-Auth-Bypass"] = "subscription-route";
                    return Task.CompletedTask;
                }
                
                logger?.LogInformation("CookieAuth: Proceeding with login redirect to {RedirectUri}", context.RedirectUri);
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                logger?.LogWarning("CookieAuth: Access denied for {Method} {Path}, User.IsAuthenticated: {IsAuth}", 
                    context.Request.Method, context.Request.Path, context.HttpContext.User.Identity?.IsAuthenticated);
                
                // For subscription routes, don't redirect - return 403 instead
                if (context.Request.Path.StartsWithSegments("/subscription", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Path.StartsWithSegments("/Subscription", StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogWarning("CookieAuth: BLOCKING access denied redirect for subscription path {Path} - returning 403 instead", context.Request.Path);
                    context.Response.StatusCode = 403;
                    context.Response.Headers["X-Auth-Bypass"] = "subscription-route";
                    return Task.CompletedTask;
                }
                
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    // Create a custom fallback policy that excludes subscription routes
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new SubscriptionRouteBypassRequirement())
        .Build();
});

// Register the custom authorization handler for subscription route bypass
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionRouteBypassHandler>();

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

// Enforce onboarding after current user is resolved and AFTER routing to access endpoint metadata
app.UseMiddleware<SetupFlowMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();