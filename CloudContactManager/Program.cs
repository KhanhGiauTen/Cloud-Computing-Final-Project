using Amazon.SimpleEmail;
using CloudContactManager.Data;
using CloudContactManager.Services;
using CloudContactManager.Services.Interfaces;
// THÊM MỚI: Các thư viện dùng cho JWT và Swagger
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Chỉ sử dụng AddControllers cho Web API, không dùng View
builder.Services.AddControllers();

// 2. Cấu hình CORS để Frontend (như React/Vue/HTML thuần) gọi được API
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

// 3. Đăng ký Swagger và cấu hình thêm NÚT Ổ KHÓA (Authorize)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CloudContactManager API", Version = "v1" });

    // Cấu hình UI cho phép nhập Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Token theo cú pháp: Bearer {token_của_bạn}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// THÊM MỚI: Cấu hình mã hóa và xác thực JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing in appsettings.json");
var key = Encoding.UTF8.GetBytes(keyString);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

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
    // Apply any pending EF Core migrations on startup
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        // In design-time tools (dotnet ef) or when DB is unreachable,
        // skip automatic migration but log the issue.
        Console.WriteLine($"Database migration failed: {ex.Message}");
    }
}

// 4. Kích hoạt Swagger UI cho tất cả các môi trường để phục vụ chấm đồ án
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// 5. Khai báo sử dụng CORS trước Authorization và Routing
app.UseCors("AllowAll");

// THÊM MỚI: app.UseAuthentication() PHẢI NẰM TRƯỚC app.UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();

// 6. Dùng MapControllers thay vì MapControllerRoute (MVC)
app.MapControllers();

app.Run();