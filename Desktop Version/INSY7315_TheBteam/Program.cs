using INSY7315_TheBteam.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Firebase service (interface -> implementation). Only register once.
builder.Services.AddSingleton<IFirebaseService, FirebaseService>();

// Configure Session (dev-friendly settings)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // dev-friendly; change to Always in prod
    options.Cookie.SameSite = SameSiteMode.Lax;                    // Lax avoids redirect issues
    options.Cookie.Name = ".INSY7315.Session";                     // easier to debug
});

// If behind a reverse proxy (IIS/Nginx), forwarded headers are required to detect original scheme
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Optionally restrict KnownNetworks/KnownProxies for production
});

var app = builder.Build();

// Error pages
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// If behind a reverse proxy, ensure this runs early
app.UseForwardedHeaders();

// Static + routing
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Temporary diagnostic middleware: prints session info for each request and sets response header
app.Use(async (context, next) =>
{
    try
    {
        var sid = context.Session?.Id ?? "(no-session)";
        var user = context.Session?.GetString("User") ?? "(no-user)";
        var email = context.Session?.GetString("Email") ?? "(no-email)";
        var role = context.Session?.GetString("Role") ?? "(no-role)";
        var path = context.Request.Path;

        System.Diagnostics.Debug.WriteLine($"[SessionDebug] Path={path} SessionId={sid} User={user} Email={email} Role={role}");

        // Add header so you can inspect in browser network tab
        if (!context.Response.HasStarted)
        {
            context.Response.Headers["X-Session-Debug"] = $"sid={sid};user={user};email={email};role={role}";
        }
    }
    catch { /* swallow for diagnostics */ }

    await next();
});

// MUST be before endpoints that rely on session
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
