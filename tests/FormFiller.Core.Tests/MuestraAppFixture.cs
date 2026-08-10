using System.Diagnostics;

namespace FormFiller.Core.Tests;

internal static class MuestraAppFixture
{
    public static Process Start()
    {
        var exePath = ResolveExe();
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Fixture executable not found at '{exePath}'.", exePath);
        }

        return Process.Start(new ProcessStartInfo(exePath))!;
    }

    public static IntPtr WaitForMainWindowHandle(Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            Thread.Sleep(100);
        }

        return IntPtr.Zero;
    }

    public static string ResolveExe()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "fixtures", "MuestraApp", "bin", "Debug", "net8.0-windows", "MuestraApp.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "MuestraApp.exe");
    }
}
