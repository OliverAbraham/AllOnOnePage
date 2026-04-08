namespace AppRunner;

/// <summary>
/// Configuration loaded from appsettings.json.
/// Derived path properties are computed from Root and Destination.
/// </summary>
public class AppRunnerConfig
{
    // --- Configurable in appsettings.json ---

    /// <summary>Network share root, e.g. \\server1\Dashboard5</summary>
    public string Root { get; set; } = "";

    /// <summary>Local destination folder, e.g. C:\Dashboard</summary>
    public string Destination { get; set; } = "";

    /// <summary>Local settings folder; supports %USERPROFILE% and other env vars.</summary>
    public string SettingsDestination { get; set; } = "";

    /// <summary>File pattern to delete inside SettingsDestination before update, e.g. *.hjson</summary>
    public string SettingsDestinationDeletePattern { get; set; } = "*.hjson";

    /// <summary>Executable file name to launch, e.g. AllOnOnePage.exe</summary>
    public string StartName { get; set; } = "";

    /// <summary>Process name used to detect/kill the running app (without .exe extension).</summary>
    public string ProcessName { get; set; } = "";

    // --- Derived paths ---

    public string Communication   => Path.Combine(Root, "ipc");
    public string BinSource       => Path.Combine(Root, "bin");
    public string SettingsSource  => Path.Combine(Root, "settings");
    public string BinDestination  => Path.Combine(Destination, "bin");

    /// <summary>Expands environment variables in all path properties.</summary>
    public void ExpandEnvironmentVariables()
    {
        Root                 = Environment.ExpandEnvironmentVariables(Root);
        Destination          = Environment.ExpandEnvironmentVariables(Destination);
        SettingsDestination  = Environment.ExpandEnvironmentVariables(SettingsDestination);
    }
}
