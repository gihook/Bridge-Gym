using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;

namespace BridgeGym.Services;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IWebHostEnvironment _env;

    public JsonStringLocalizerFactory(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IStringLocalizer Create(Type resourceSource) => CreateLocalizer();

    public IStringLocalizer Create(string baseName, string location) => CreateLocalizer();

    private IStringLocalizer CreateLocalizer()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var path = Path.Combine(_env.ContentRootPath, "Resources", $"{culture}.json");
        
        if (!File.Exists(path))
        {
            // Fallback to two letter ISO language (e.g. en-US -> en.json)
            path = Path.Combine(_env.ContentRootPath, "Resources", $"{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.json");
        }
        
        return new JsonStringLocalizer(path);
    }
}
