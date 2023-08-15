using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Runtime.Intrinsics.X86;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var configuration = builder.Configuration;
var cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>();

if (cacheSettings.UseMemoryCache)
{
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(cacheSettings.RedisConnectionString));
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

public class CacheSettings
{
    public bool UseMemoryCache { get; set; }
    public string RedisConnectionString { get; set; }
}