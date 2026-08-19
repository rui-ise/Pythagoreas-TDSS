using System.IO;

namespace BinanceScannerApp.Services;

/// <summary>Loads a simple local .env file without adding a third-party dependency.</summary>
public static class EnvironmentLoader
{
    public static void Load()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }.Distinct())
        {
            var directory = new DirectoryInfo(start);
            for (var level = 0; directory is not null && level < 8; level++, directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, ".env");
                if (!File.Exists(path)) continue;
                foreach (var line in File.ReadLines(path))
                {
                    var text = line.Trim();
                    if (text.Length == 0 || text.StartsWith('#') || !text.Contains('=')) continue;
                    var pair = text.Split('=', 2);
                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(pair[0].Trim())))
                        Environment.SetEnvironmentVariable(pair[0].Trim(), pair[1].Trim().Trim('"'));
                }
                return;
            }
        }
    }
}
