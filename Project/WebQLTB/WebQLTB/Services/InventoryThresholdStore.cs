using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace WebQLTB.Services;

public sealed class InventoryThresholdStore
{
    private readonly string _settingsPath;
    private readonly int _fallback;
    private readonly object _sync = new();

    public InventoryThresholdStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _settingsPath = Path.Combine(environment.ContentRootPath, "App_Data", "inventory-settings.json");
        _fallback = Math.Clamp(configuration.GetValue<int?>("Inventory:LowStockThreshold") ?? 3, 0, 100000);
    }

    public int GetThreshold()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(_settingsPath)) return _fallback;
                var settings = JsonSerializer.Deserialize<InventorySettings>(File.ReadAllText(_settingsPath));
                return Math.Clamp(settings?.LowStockThreshold ?? _fallback, 0, 100000);
            }
            catch (IOException)
            {
                return _fallback;
            }
            catch (JsonException)
            {
                return _fallback;
            }
        }
    }

    public void SaveThreshold(int threshold)
    {
        if (threshold is < 0 or > 100000)
            throw new ArgumentOutOfRangeException(nameof(threshold));

        lock (_sync)
        {
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);
            var tempPath = _settingsPath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(new InventorySettings(threshold)));
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
    }

    private sealed record InventorySettings(int LowStockThreshold);
}
