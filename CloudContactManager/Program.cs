using CloudContactManager.Data;
using CloudContactManager.Services;
using CloudContactManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseMySql(defaultConnection, ServerVersion.AutoDetect(defaultConnection));
    }
    else
    {
        options.UseSqlServer(defaultConnection);
    }
});

// ============================================================================
// Notification Service Registration
// ============================================================================
// LOCAL  -> LocalNotificationService  (no AWS needed, logs to console)
// AWS    -> AwsNotificationService    (requires IAM Role or AWS credentials)
// Switch by checking ASP.NET Core Environment
// ============================================================================

if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonSimpleEmailService>();

    builder.Services.AddHttpClient();

    // ✅ FIX CHỖ NÀY
    builder.Services.AddScoped<INotificationService, NotificationService>();

    Console.WriteLine("Using AWS SES for Email and Speed SMS for SMS");
}
else
{
    // Môi trường Local Simulation
    builder.Services.AddScoped<INotificationService, LocalNotificationService>();
    Console.WriteLine("Using Local Notification Service (console simulation)");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();