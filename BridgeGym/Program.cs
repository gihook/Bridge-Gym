using System.Globalization;
using System.IO;
using System.Linq;
using BridgeGym.Data;
using BridgeGym.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews().AddViewLocalization();

builder.Services.AddDbContext<BridgeGymContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder
    .Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
    })
    .AddEntityFrameworkStores<BridgeGymContext>();

builder.Services.AddScoped<IExerciseService, ExerciseService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

builder.Services.AddHangfire(config =>
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("DefaultConnection")
            )
        )
);

builder.Services.AddHangfireServer();

// Dynamically discover supported cultures from the Resources folder
var resourcesPath = Path.Combine(builder.Environment.ContentRootPath, "Resources");
var supportedCultures = new List<string>();

if (Directory.Exists(resourcesPath))
{
    var files = Directory.GetFiles(resourcesPath, "*.json");
    foreach (var file in files)
    {
        var cultureCode = Path.GetFileNameWithoutExtension(file);
        supportedCultures.Add(cultureCode);
    }
}

// Fallback to "en" if none found
if (!supportedCultures.Any())
{
    supportedCultures.Add("en");
}

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var validCultures = new List<CultureInfo>();
    foreach (var c in supportedCultures)
    {
        try
        {
            validCultures.Add(new CultureInfo(c));
        }
        catch { }
    }

    if (!validCultures.Any())
        validCultures.Add(new CultureInfo("en"));

    options.DefaultRequestCulture = new RequestCulture(validCultures[0].Name);
    options.SupportedCultures = validCultures;
    options.SupportedUICultures = validCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

app.MapRazorPages();
app.MapControllerRoute(name: "default", pattern: "{controller=Exercise}/{action=Index}/{id?}");

app.Run();
