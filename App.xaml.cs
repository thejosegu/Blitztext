using System.Windows;
using Blitztext.Core;
using Blitztext.UI;

namespace Blitztext;

public partial class App : Application
{
    private BlitztextApp? _app;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Global exception handlers — log everything to file ──────────
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var msg = $"[{DateTime.Now:HH:mm:ss}] FATAL: {args.ExceptionObject}";
            AppLog.Add(msg);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Add($"UI-FEHLER: {args.Exception.Message}\n{args.Exception.StackTrace}");
            args.Handled = true;   // prevent crash, keep running
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Add($"TASK-FEHLER: {args.Exception.Message}");
            args.SetObserved();
        };

        // DPI awareness
        try { NativeMethods.SetProcessDpiAwarenessContext(-4); } catch { }

        var config = new AppConfig();
        _app = new BlitztextApp(config);

        var overlay = new RecordingOverlay();

        _app.OnOverlayRecording = mode =>
            Dispatcher.Invoke(() => overlay.ShowRecording(mode));

        _app.OnOverlayProcessing = status =>
            Dispatcher.Invoke(() => overlay.ShowProcessing(status));

        _app.OnOverlayHide = () =>
            Dispatcher.Invoke(() => overlay.HideOverlay());

        _app.OnOverlayHideImmediate = () =>
            overlay.HideImmediate();   // already called on UI thread via InvokeAsync in pipeline

        _app.OnShowToast = msg =>
            Dispatcher.InvokeAsync(() => ToastWindow.Show(msg));

        _app.OnOpenSettings = () =>
            Dispatcher.Invoke(() => EnsureSettingsOpen(config));
    }

    private void EnsureSettingsOpen(AppConfig config)
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(config);
        _settingsWindow.OnSaved += updatedConfig =>
        {
            // hotkeys and config already updated inside SettingsWindow
        };
        _settingsWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _app?.Dispose();
        base.OnExit(e);
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern bool SetProcessDpiAwarenessContext(int value);
}
