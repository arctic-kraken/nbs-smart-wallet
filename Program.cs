using Microsoft.EntityFrameworkCore;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    //.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json")
    .AddJsonFile($"appsettings.Development.json")
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true);

var revolutConfig = builder.Configuration.GetSection("RevolutProxyConfig").Get<RevolutProxyConfig>()
    ?? throw new Exception("Failed to load Revolut Proxy Config");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("revolut", c => { })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = RevolutProxy.GetDefaultRevolutHandler(revolutConfig.pfx_content);
		return handler;
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

//builder.Services.AddDbContext<nbsDbContext>(options =>
//{
    
//    options.UseNpgsql(builder.Configuration["DefaultConnection"]);
//});


builder.Services.AddScoped<RevolutProxy>();

builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
