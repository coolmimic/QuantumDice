var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// 配置静态文件
app.UseDefaultFiles();
app.UseStaticFiles();

// SPA 路由回退
app.MapFallbackToFile("index.html");

app.Run();
