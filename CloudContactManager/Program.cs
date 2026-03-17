using CloudContactManager.Data;
using CloudContactManager.Services;
using CloudContactManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Amazon.SimpleEmail;

var builder = WebApplication.CreateBuilder(args);

// 1. THAY ĐỔI: Chỉ sử dụng AddControllers cho Web API, không dùng View
builder.Services.AddControllers(); // Đã sửa lỗi chính tả

// 2. THÊM MỚI: Cấu hình CORS để Frontend (như React/Vue) gọi được API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// 3. THÊM MỚI: Đăng ký Swagger để sinh tài liệu và test API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonSimpleEmailService>();
    builder.Services.AddHttpClient();

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

// 4. THAY ĐỔI: Kích hoạt Swagger UI cho tất cả các môi trường để phục vụ chấm đồ án
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// 5. THÊM MỚI: Khai báo sử dụng CORS trước Authorization và Routing
app.UseCors("AllowAll");

app.UseAuthorization();

// 6. THAY ĐỔI: Dùng MapControllers thay vì MapControllerRoute (MVC)
app.MapControllers();

app.Run();