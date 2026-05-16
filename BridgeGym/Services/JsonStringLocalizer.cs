using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace BridgeGym.Services;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly Dictionary<string, string> _localizationMaps;

    public JsonStringLocalizer(string filePath)
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            _localizationMaps =
                JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        else
        {
            _localizationMaps = new();
        }
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = _localizationMaps.TryGetValue(name, out var val) ? val : name;
            return new LocalizedString(name, value, val == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = _localizationMaps.TryGetValue(name, out var val) ? val : name;
            var value = string.Format(format, arguments);
            return new LocalizedString(name, value, val == null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (var kvp in _localizationMaps)
        {
            yield return new LocalizedString(kvp.Key, kvp.Value, false);
        }
    }
}
