using System.Text.Json;
using AppRunner;

var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Configuration file not found: {configPath}");
    return 1;
}

var json = File.ReadAllText(configPath);
var config = JsonSerializer.Deserialize<AppRunnerConfig>(
    json,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (config is null)
{
    Console.Error.WriteLine("Failed to deserialize appsettings.json.");
    return 1;
}

config.ExpandEnvironmentVariables();

new ApplicationRunner(config).Run();
return 0;
