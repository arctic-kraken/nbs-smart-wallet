using Microsoft.EntityFrameworkCore;
using nbs_smart_wallet.Models;
using nbs_smart_wallet.Services;
using NpgsqlTypes;
using Serilog;
using Serilog.Sinks.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();
    //.AddUserSecrets(Assembly.GetExecutingAssembly(), true);

var revolutConfig = Environment.GetEnvironmentVariable("RevolutProxyConfig");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("revolut", c => { })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var content = Environment.GetEnvironmentVariable("pfx_content") 
            ?? throw new Exception("pfx_content environment variable not set");
        var handler = RevolutProxy.GetDefaultRevolutHandler(content);
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

string? db_con_str = Environment.GetEnvironmentVariable("DefaultConnection");
if (String.IsNullOrEmpty(db_con_str))
    throw new Exception("Database connection string is null or empty");

builder.Services.AddDbContext<nbsDbContext>(options =>
{
    options.UseNpgsql(db_con_str);
});

builder.Services.AddScoped<RevolutProxy>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

//Used columns (Key is a column name) 
//Column type is writer's constructor parameter
IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
{
	{"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
	{"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
	{"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
	{"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
	{"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
	{"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
	{"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
	{"machine_name", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") }
};

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.PostgreSQL(db_con_str, "logs", columnWriters, 
                        schemaName: "public", needAutoCreateTable: true,
                        batchSizeLimit: 30, period: TimeSpan.FromMinutes(2))
    .CreateLogger();
builder.Host.UseSerilog();
Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

builder.Services.Configure<IHostApplicationLifetime>(options =>
{
	options.ApplicationStopping.Register(async () => {
        Log.Information("Application is Stopping");
        await Log.CloseAndFlushAsync(); 
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}")
    .WithStaticAssets();

app.Run();

