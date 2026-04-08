using System.Diagnostics;

namespace AppRunner;

/// <summary>
/// Ports the logic of runner.cmd: kills the managed application, copies updated
/// binaries and settings from a network share, starts the application and then
/// monitors it in a loop, handling force-reboot / force-close / restart signals.
/// </summary>
internal class ApplicationRunner
{
    private readonly AppRunnerConfig _config;

    public ApplicationRunner(AppRunnerConfig config) => _config = config;

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Outer loop (equivalent to startapp.cmd :loop).
    /// Copies updated runner files, then runs one full runner cycle, and repeats.
    /// </summary>
    public void Run()
    {
        PrintBanner();
        CreateDirectoriesIfFirstStart();

        while (true)
        {
            CopyUpdatedRunnerFiles();
            RunnerCycle();
        }
    }

    // -------------------------------------------------------------------------
    // Runner cycle  (equivalent to runner.cmd :update → :infinite_loop)
    // -------------------------------------------------------------------------

    private void RunnerCycle()
    {
        KillApplication();
        CopyNewFiles();
        StartApplication();
        InfiniteLoop();
    }

    private void InfiniteLoop()
    {
        while (true)
        {
            // Wait 60 seconds in 10-second intervals, each interruptible
            for (int i = 1; i <= 6; i++)
                Wait10Seconds();

            if (!HasCommunicationFiles())
            {
                bool reconnected = CheckConnectionAndReboot();
                if (reconnected)
                    continue;       // goto :infinite_loop
                return;             // reboot loop took over
            }

            if (File.Exists(Path.Combine(_config.Communication, "Force_reboot.dat")))
            {
                RebootComputer();
                return;
            }

            if (File.Exists(Path.Combine(_config.Communication, "Force_application_close.dat")))
                return;             // triggers a new update cycle in RunnerCycle

            if (File.Exists(Path.Combine(_config.BinDestination, "restart-request.dat")))
            {
                RestartApplication();
                return;             // triggers a new update cycle in RunnerCycle
            }

            if (!IsApplicationRunning())
                return;             // triggers a new update cycle in RunnerCycle
        }
    }

    // -------------------------------------------------------------------------
    // Signal handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Waits up to 30 seconds for the communication share to become available.
    /// Returns true if it came back (caller should continue the infinite loop),
    /// false if still unavailable after the grace period (triggers reboot).
    /// </summary>
    private bool CheckConnectionAndReboot()
    {
        Console.WriteLine("no network connection, reboot pending in 30 seconds...");
        Wait10Seconds();
        if (HasCommunicationFiles()) return true;

        Console.WriteLine("no network connection, reboot pending in 20 seconds...");
        Wait10Seconds();
        if (HasCommunicationFiles()) return true;

        Console.WriteLine("no network connection, reboot pending in 10 seconds...");
        Wait10Seconds();
        if (HasCommunicationFiles()) return true;

        Console.WriteLine("Rebooting now to restore network connection");
        RebootComputer();
        return false;
    }

    /// <summary>Issues a reboot and retries every 120 s until the OS shuts down this process.</summary>
    private void RebootComputer()
    {
        while (true)
        {
            Console.WriteLine("Reboot is being done...");
            LaunchProcess("shutdown", "-r");
            WriteFile(Path.Combine(_config.Communication, "rebooting"));
            DeleteFile(Path.Combine(_config.Communication, "Force_reboot.dat"));
            Console.WriteLine("waiting 120 seconds for reboot...");
            Thread.Sleep(120_000);
        }
    }

    /// <summary>Handles a restart-request.dat signal (equivalent to :restart_application → goto :update).</summary>
    private void RestartApplication()
    {
        Console.WriteLine("Application requested a hard restart.");
        DeleteFile(Path.Combine(_config.BinDestination, "restart-request.dat"));
        KillApplicationProcess();
        Console.WriteLine("waiting 10 seconds...");
        Thread.Sleep(10_000);
    }

    // -------------------------------------------------------------------------
    // Core operations
    // -------------------------------------------------------------------------

    private void KillApplication()
    {
        Console.WriteLine("Killing the application process...");
        Thread.Sleep(2_000);
        KillApplicationProcess();
        Console.WriteLine("waiting 10 seconds...");
        Thread.Sleep(10_000);
        WriteFile(Path.Combine(_config.Communication, "Application_is_closed.dat"));
    }

    private void CopyNewFiles()
    {
        Console.WriteLine();
        DeleteFile(Path.Combine(_config.Communication, "Force_reboot.dat"));
        DeleteFile(Path.Combine(_config.Communication, "rebooting"));

        if (!Directory.Exists(_config.BinSource) ||
            !Directory.EnumerateFileSystemEntries(_config.BinSource).Any())
        {
            Console.WriteLine("Copy error! No files in source bin directory or network is unreachable!");
            return;
        }

        Console.WriteLine("copying new files...");
        DeleteFilesRecursive(_config.BinDestination, "*");
        CopyDirectoryTree(_config.BinSource, _config.BinDestination);

        Console.WriteLine("copying new settings...");
        DeleteFilesRecursive(_config.SettingsDestination, _config.SettingsDestinationDeletePattern);
        CopyDirectoryTree(_config.SettingsSource, _config.SettingsDestination);

        WriteFile(Path.Combine(_config.Communication, "Application_is_updated.dat"));
        DeleteFile(Path.Combine(_config.Communication, "Force_application_close.dat"));
        DeleteFile(Path.Combine(_config.BinDestination, "restart-request.dat"));
    }

    private void StartApplication()
    {
        var exePath = Path.Combine(_config.BinDestination, _config.StartName);
        Console.WriteLine($"Starting {exePath}...");
        Process.Start(new ProcessStartInfo
        {
            FileName         = exePath,
            WorkingDirectory = _config.BinDestination,
            UseShellExecute  = true
        });
    }

    /// <summary>
    /// Waits 10 seconds but returns immediately if a Force_application_close or
    /// restart-request signal file is already present (mirrors :wait_10_seconds).
    /// </summary>
    private void Wait10Seconds()
    {
        if (File.Exists(Path.Combine(_config.Communication, "Force_application_close.dat")))
            return;
        if (File.Exists(Path.Combine(_config.BinDestination, "restart-request.dat")))
            return;
        Console.WriteLine("waiting 10 seconds...");
        Thread.Sleep(10_000);
    }

    // -------------------------------------------------------------------------
    // Startup helpers
    // -------------------------------------------------------------------------

    private void CreateDirectoriesIfFirstStart()
    {
        Directory.CreateDirectory(_config.Destination);
        Directory.CreateDirectory(_config.BinDestination);
        Directory.CreateDirectory(_config.SettingsDestination);
    }

    /// <summary>
    /// Copies *.cmd files from Root to Destination when the source is newer
    /// (equivalent to: xcopy %ROOT%\*.cmd %DESTINATION% /Y /D).
    /// </summary>
    private void CopyUpdatedRunnerFiles()
    {
        if (!Directory.Exists(_config.Root)) return;
        foreach (var sourceFile in Directory.GetFiles(_config.Root, "*.cmd"))
        {
            var destFile = Path.Combine(_config.Destination, Path.GetFileName(sourceFile));
            if (!File.Exists(destFile) ||
                File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
            {
                try   { File.Copy(sourceFile, destFile, overwrite: true); }
                catch (Exception ex) { Console.WriteLine($"Warning: could not copy {sourceFile}: {ex.Message}"); }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Process helpers
    // -------------------------------------------------------------------------

    private void KillApplicationProcess()
    {
        var processName = Path.GetFileNameWithoutExtension(_config.ProcessName);
        foreach (var p in Process.GetProcessesByName(processName))
        {
            try   { p.Kill(entireProcessTree: true); }
            catch (Exception ex) { Console.WriteLine($"Warning: could not kill {p.ProcessName}: {ex.Message}"); }
        }
    }

    private bool IsApplicationRunning()
    {
        var processName = Path.GetFileNameWithoutExtension(_config.ProcessName);
        return Process.GetProcessesByName(processName).Length > 0;
    }

    private static void LaunchProcess(string fileName, string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false });
        }
        catch (Exception ex) { Console.WriteLine($"Warning: could not launch '{fileName} {arguments}': {ex.Message}"); }
    }

    // -------------------------------------------------------------------------
    // File system helpers
    // -------------------------------------------------------------------------

    private bool HasCommunicationFiles() =>
        Directory.Exists(_config.Communication) &&
        Directory.EnumerateFileSystemEntries(_config.Communication).Any();

    /// <summary>
    /// Recursively copies a directory tree, only overwriting files where the
    /// source is newer (equivalent to xcopy /s /Y /D).
    /// </summary>
    private static void CopyDirectoryTree(string source, string destination)
    {
        if (!Directory.Exists(source)) return;
        foreach (var sourceFile in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, sourceFile);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            if (!File.Exists(destFile) ||
                File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destFile))
            {
                try   { File.Copy(sourceFile, destFile, overwrite: true); }
                catch (Exception ex) { Console.WriteLine($"Warning: could not copy {sourceFile}: {ex.Message}"); }
            }
        }
    }

    /// <summary>Deletes all files matching <paramref name="pattern"/> inside <paramref name="directory"/> (equivalent to del /S /Q).</summary>
    private static void DeleteFilesRecursive(string directory, string pattern)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var file in Directory.GetFiles(directory, pattern, SearchOption.AllDirectories))
        {
            try   { File.Delete(file); }
            catch (Exception ex) { Console.WriteLine($"Warning: could not delete {file}: {ex.Message}"); }
        }
    }

    private static void WriteFile(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, ".");
        }
        catch (Exception ex) { Console.WriteLine($"Warning: could not write {path}: {ex.Message}"); }
    }

    private static void DeleteFile(string path)
    {
        try   { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) { Console.WriteLine($"Warning: could not delete {path}: {ex.Message}"); }
    }

    // -------------------------------------------------------------------------
    // Banner
    // -------------------------------------------------------------------------

    private void PrintBanner()
    {
        Console.WriteLine("-----------------------------------------------------------------------------------");
        Console.WriteLine();
        Console.WriteLine("CIRIDATA DESKTOP APPLICATION RUNNER");
        Console.WriteLine();
        Console.WriteLine("Oliver Abraham 2023, mail@oliver-abraham.de");
        Console.WriteLine("This program is hosted at http://www.github.com/oliverabraham/desktopapplicationrunner");
        Console.WriteLine();
        Console.WriteLine("-----------------------------------------------------------------------------------");
        Console.WriteLine($"Root folder               : {_config.Root}");
        Console.WriteLine($"IPC folder                : {_config.Communication}");
        Console.WriteLine($"Application folder        : {_config.Destination}");
        Console.WriteLine($"Bin source                : {_config.BinSource}");
        Console.WriteLine($"Bin destination           : {_config.BinDestination}");
        Console.WriteLine($"Settings source           : {_config.SettingsSource}");
        Console.WriteLine($"Settings destination      : {_config.SettingsDestination}");
        Console.WriteLine($"Application(EXE) name     : {_config.StartName}");
        Console.WriteLine($"Application(process) name : {_config.ProcessName}");
        Console.WriteLine("-----------------------------------------------------------------------------------");
    }
}
