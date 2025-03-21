using TestKB.Repositories;
using TestKB.Services;
using TestKB.Services.Interfaces;
using TestKB.Services.Notification;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCaching();

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<INotificationService, DirectNotificationService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentManager, ContentManager>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPythonScript", policy =>
    {
        policy.WithOrigins("http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("TestKB.Services.DebugNotificationService", LogLevel.Debug);
builder.Logging.AddFilter("TestKB.Services", LogLevel.Debug);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("TestKB", LogLevel.Debug);
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseResponseCaching();

app.UseSession();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Content/Edit"))
    {
        context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        context.Response.Headers.Append("Pragma", "no-cache");
        context.Response.Headers.Append("Expires", "0");
    }

    await next();
});

app.UseCors("AllowPythonScript");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Content}/{action=DepartmentSelect}/{id?}");

app.MapControllerRoute(
    name: "notification",
    pattern: "notification/{action=Status}/{id?}",
    defaults: new { controller = "Notification" });

var dataDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data");
if (!Directory.Exists(dataDirectory))
{
    try
    {
        Directory.CreateDirectory(dataDirectory);
        app.Logger.LogInformation("Created App_Data directory: {Directory}", dataDirectory);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to create App_Data directory: {Directory}", dataDirectory);
    }
}

app.Run();
app.Run();