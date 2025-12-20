using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot;
using QuantumDice.Api.Bot;
using QuantumDice.Api.Services;
using QuantumDice.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 配置数据库
builder.Services.AddDbContext<QuantumDiceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册业务服务
builder.Services.AddScoped<IDealerService, DealerService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBetParseService, BetParseService>();
builder.Services.AddScoped<IBetResultService, BetResultService>();

// 配置 JWT 认证
var jwtKey = builder.Configuration["Jwt:Key"] ?? "QuantumDice_SuperSecretKey_2024!@#$%^";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuantumDice",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "QuantumDice",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// 配置 Telegram Bot
var botToken = builder.Configuration["TelegramBot:Token"] ?? "";
if (!string.IsNullOrEmpty(botToken))
{
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
    builder.Services.AddScoped<BotUpdateHandler>();
    builder.Services.AddHostedService<TelegramBotService>();
    builder.Services.AddHostedService<GameSchedulerService>();
}

// 配置控制器
builder.Services.AddControllers();

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuantumDiceDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// 中间件配置
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 健康检查端点
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

// 启动时显示信息
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("QuantumDice API 已启动");
    logger.LogInformation("JWT 认证已配置");
    if (!string.IsNullOrEmpty(botToken))
    {
        logger.LogInformation("Telegram Bot 已配置");
    }
    else
    {
        logger.LogWarning("Telegram Bot Token 未配置, Bot 功能已禁用");
    }
});

app.Run();
