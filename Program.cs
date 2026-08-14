//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using System.Security.Cryptography;
using nbs_smart_wallet.Services;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("revolut", c => { })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var certificateWithKey = X509Certificate2.CreateFromPemFile(@"C:\Users\JakubKiepas\transport.pem", @"C:\Users\JakubKiepas\private.key");
        handler.ClientCertificates.Add(certificateWithKey);

#if DEBUG
		handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif

		return handler;
    });

builder.Services.AddScoped<RevolutProxy>();

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(jwtOptions =>
//    {
//        jwtOptions.Authority = "";
//        jwtOptions.Authority = "";
//    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
