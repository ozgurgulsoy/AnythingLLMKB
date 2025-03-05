// Program.cs
using TestKB.Middleware;
using TestKB.Repositories;
using TestKB.Services;
using Microsoft.Extensions.Caching.Memory;
using TestKB.Extensions;
using TestKB.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add response caching services
builder.Services.AddResponseCaching();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpClient services for notification
builder.Services.AddHttpClient();

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register memory cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Register our content services.
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentManager, ContentManager>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();

// Register notification service
builder.Services.AddScoped<INotificationService, ContentChangeNotificationService>();

// Register error handling service
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
// Add this after other service registrations in Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPythonScript", policy =>
    {
        policy.WithOrigins("http://localhost:5000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add this before app.UseRouting() in the middleware pipeline

// Configure logging - higher detail level for development
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Set logging level
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("TestKB", LogLevel.Debug);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Use custom global exception handler in production
    app.UseGlobalExceptionHandler();
    app.UseHsts();
}
else
{
    // In development, show detailed error page
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Add response caching middleware
app.UseResponseCaching();

// Use session before MVC
app.UseSession();

// Add middleware to always set cache control headers
app.Use(async (context, next) =>
{
    // For all edit pages, prevent caching
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

app.Run();