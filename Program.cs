//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using System.Security.Cryptography;
using nbs_smart_wallet.Services;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("revolut", c => { })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var certificateWithKey = X509Certificate2.CreateFromPemFile(@"C:\Users\JakubKiepas\transport.pem", @"C:\Users\JakubKiepas\private.key");
		// netcore is retarded, turns out I have to turn the pem and pk into pfx and load that one for it to auth
		var cert = new X509Certificate2(@"C:\Users\JakubKiepas\transport.pfx");
 

        handler.ClientCertificates.Add(cert);
        handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.AllowAutoRedirect = true;
        handler.MaxAutomaticRedirections = 1;

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
