using TestKB.Middleware;
using TestKB.Repositories;
using TestKB.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register our content services.
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IContentManager, ContentManager>();
builder.Services.AddScoped<IContentRepository, ContentRepository>();
// Register error handling service
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Content}/{action=DepartmentSelect}/{id?}");

app.Run();