using CloudContactManager.Data;
using CloudContactManager.Services;
using CloudContactManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Amazon.SimpleEmail;
using Amazon.SimpleNotificationService;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Controllers và Views
builder.Services.AddControllersWithViews();

// 2. Cấu hình Database (Hỗ trợ MySql cho AWS RDS và SqlServer cho Local)
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

// 3. Đăng ký Notification Service (QUAN TRỌNG: Chỉ dùng một khối If duy nhất)
if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    // Cấu hình AWS để dùng cho việc gửi Email (SES)
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
    builder.Services.AddAWSService<IAmazonSimpleEmailService>();

    // ĐĂNG KÝ SPEED SMS TẠI ĐÂY (Thay thế hoàn toàn AWS cho SMS)
    builder.Services.AddScoped<INotificationService, SpeedSmsService>();

    // In ra log để xác nhận khi app khởi động
    Console.WriteLine("-----------------------------------------");
    Console.WriteLine("SYSTEM: Using SpeedSMS Notification Service");
    Console.WriteLine("-----------------------------------------");
}
else
{
    // Chế độ Local: Ghi log ra Console thay vì gửi tin thật
    builder.Services.AddScoped<INotificationService, LocalNotificationService>();
    Console.WriteLine("SYSTEM: Using Local Notification Service");
}

var app = builder.Build();

// 4. Tự động tạo Database nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 5. Cấu hình HTTP Request Pipeline
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