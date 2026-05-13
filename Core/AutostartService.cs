using System.Diagnostics;
using System.Threading.Tasks;

namespace Blitztext.Core;

public interface IAutostartService
{
    Task ApplyAsync(bool enable);
}

public static class AutostartServiceFactory
{
    public static IAutostartService CreateDefault() =>
        RuntimeEnvironment.HasPackageIdentity
            ? new PackagedAutostartService()
            : new RegistryAutostartService();
}

internal sealed class RegistryAutostartService : IAutostartService
{
    public Task ApplyAsync(bool enable)
    {
        const string regPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        var exe = $"\"{Process.GetCurrentProcess().MainModule?.FileName}\"";

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath, writable: true);
        if (key == null) return Task.CompletedTask;

        if (enable)
            key.SetValue("Blitztext", exe);
        else
        {
            try { key.DeleteValue("Blitztext"); } catch { }
        }

        return Task.CompletedTask;
    }
}

internal sealed class PackagedAutostartService : IAutostartService
{
    private const string StartupTaskId = "BlitztextStartupTask";

    public Task ApplyAsync(bool enable)
    {
        if (!enable)
            return Task.CompletedTask;

        throw new InvalidOperationException(
            $"Die Store-Variante braucht einen MSIX-StartupTask mit der TaskId '{StartupTaskId}'. " +
            "Der Codepfad ist vorbereitet, aber das Paketprojekt und das Manifest fehlen noch.");
    }
}