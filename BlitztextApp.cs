using System;
using System.Threading;
using System.Threading.Tasks;
using Blitztext.Core;
using Blitztext.Tray;

namespace Blitztext;

/// <summary>
/// Main orchestrator — connects HotkeyListener → AudioRecorder → Pipeline.
/// Runs on the WPF UI thread; pipeline executes on a dedicated Task (max 1 at a time).
/// </summary>
public sealed class BlitztextApp : IDisposable
{
    private readonly AppConfig       _config;
    private readonly AudioRecorder   _recorder;
    private readonly HotkeyListener  _hotkeys;
    public  readonly TrayManager     Tray;
    private bool                     _apiAvailable;

    private readonly SemaphoreSlim   _pipelineLock = new(1, 1);
    private volatile bool            _recordingActive;
    private volatile bool            _recordingStarting;
    private volatile bool            _stopPending;    // stop arrived before start completed
    private nint                     _targetHwnd;

    // Callbacks so UI layer can show Toast and recording overlay
    public Action<string>? OnShowToast;
    public Action?         OnOpenSettings;
    public Action<string>? OnOverlayRecording;   // (mode)  — show blinking dot
    public Action<string>? OnOverlayProcessing;  //         — switch to amber dot
    public Action?         OnOverlayHide;        //         — fade out
    public Action?         OnOverlayHideImmediate; //       — instant hide before inject

    public BlitztextApp(AppConfig config)
    {
        _config   = config;
        _recorder = new AudioRecorder();
        _apiAvailable = !string.IsNullOrWhiteSpace(_config.ApiKey);

        Tray = new TrayManager(
            onOpenSettings: () => OnOpenSettings?.Invoke(),
            onQuit: Quit);

        LocalTranscriber.LoadStatusChanged += OnLocalModelLoadStatusChanged;

        _hotkeys = new HotkeyListener(
            getHotkeys:    () => _config.Hotkeys,
            onStart:       OnRecordingStart,
            onStop:        OnRecordingStop,
            getRecordMode: () => _config.RecordMode);

        _hotkeys.Start();
        AppLog.Add("Blitztext gestartet — bereit");
        AppLog.Add($"Aufnahmemodus: {_config.RecordMode}");
        RefreshTrayStatus();

        // Lokales Modell sofort im Hintergrund laden, damit der erste Hotkey nicht wartet
        if (_config.TranscribeMode == "local" && LocalTranscriber.ModelExists(_config.LocalModelPath))
        {
            Task.Run(() =>
            {
                try
                {
                    LocalTranscriber.WarmUp(_config.LocalModelPath, _config.WhisperLanguage);
                    AppLog.Add("Lokales Whisper-Modell vorgeladen");
                }
                catch (Exception ex)
                {
                    AppLog.Add($"Modell-Vorladen fehlgeschlagen: {ex.Message}");
                }
            });
        }
    }

    private void OnLocalModelLoadStatusChanged(bool success, string? error)
    {
        RefreshTrayStatus();
    }

    public void RefreshTrayStatus()
    {
        Tray.SetStatus(GetIdleTrayStatus());
    }

    private TrayStatus GetIdleTrayStatus()
    {
        if (_config.TranscribeMode == "local")
            return LocalTranscriber.IsModelLoaded ? TrayStatus.Ready : TrayStatus.Error;

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            return TrayStatus.Error;

        return _apiAvailable ? TrayStatus.Ready : TrayStatus.Error;
    }

    // ── hotkey callbacks (called from hook thread) ────────────────────

    private bool OnRecordingStart(string mode)
    {
        try
        {
        if (_config.TranscribeMode != "local" && string.IsNullOrEmpty(_config.ApiKey))
        {
            AppLog.Add("Kein API-Key gesetzt — Einstellungen öffnen");
            Tray.SetStatus(TrayStatus.Error);
            OnOpenSettings?.Invoke();
            return false;
        }

        if (_config.TranscribeMode == "local" && !LocalTranscriber.ModelExists(_config.LocalModelPath))
        {
            AppLog.Add("Lokales Modell nicht gefunden — Einstellungen öffnen");
            Tray.SetStatus(TrayStatus.Error);
            OnOpenSettings?.Invoke();
            return false;
        }

        if (!_pipelineLock.Wait(0))
        {
            AppLog.Add("Aufnahme ignoriert — vorherige Verarbeitung läuft noch");
            return false;
        }

        // Capture BEFORE any Dispatcher call — at this moment the user's window has focus
        _targetHwnd = Injector.CaptureTarget();

        AppLog.Add($"Aufnahme gestartet ({mode})");
        Tray.SetStatus(TrayStatus.Recording, mode);
        OnOverlayRecording?.Invoke(mode);

        try
        {
            _recordingStarting = true;
            _recorder.Start();
            _recordingActive = true;
        }
        catch (Exception ex)
        {
            // Mikrofon-Fehler — Zustand zurücksetzen und Lock freigeben
            _recordingActive = false;
            _recordingStarting = false;
            _stopPending     = false;
            _pipelineLock.Release();
            AppLog.SetError($"Mikrofon-Fehler beim Starten: {ex.Message}");
            Tray.SetStatus(TrayStatus.Error);
            OnOverlayHide?.Invoke();
            _ = Task.Delay(3000).ContinueWith(_ => RefreshTrayStatus());
            return false;
        }
        finally
        {
            _recordingStarting = false;
        }

        // Handle race: stop was requested while we were setting up
        if (_stopPending)
        {
            _stopPending = false;
            AppLog.Add("Aufnahme sofort gestoppt (stop war vor start angekommen)");
            OnRecordingStop(mode);
        }
        return true;
        }
        catch (Exception ex)
        {
            AppLog.SetError($"OnRecordingStart: {ex.Message}");
            return false;
        }
    }

    private void OnRecordingStop(string mode)
    {
        try
        {
        if (_recordingStarting)
        {
            // Start is still acquiring the device; defer stop until Start finished.
            _stopPending = true;
            return;
        }

        if (!_recordingActive)
        {
            return;
        }
        _recordingActive = false;

        AppLog.Add($"Aufnahme gestoppt ({mode})");
        System.IO.MemoryStream? audio;
        try { audio = _recorder.Stop(); }
        catch (Exception ex)
        {
            AppLog.SetError($"Mikrofon-Fehler beim Stoppen: {ex.Message}");
            RefreshTrayStatus();
            OnOverlayHide?.Invoke();
            _pipelineLock.Release();
            return;
        }

        if (audio == null)
        {
            AppLog.Add("Keine Audiodaten — zu kurze Aufnahme?");
            RefreshTrayStatus();
            OnOverlayHide?.Invoke();
            _pipelineLock.Release();
            return;
        }

        Tray.SetStatus(TrayStatus.Processing, mode);
        OnOverlayProcessing?.Invoke(_config.TranscribeMode == "local" ? "Transkribiere lokal..." : "Transkribiere...");
        _ = Task.Run(() => RunPipelineAsync(audio!, mode));
        }
        catch (Exception ex) { AppLog.SetError($"OnRecordingStop: {ex.Message}"); }
    }

    // ── pipeline (background Task) ────────────────────────────────────

    private async Task RunPipelineAsync(System.IO.MemoryStream audio, string mode)
    {
        try
        {
            // 1. Transcribe
            string transcript;
            if (_config.TranscribeMode == "local")
            {
                transcript = await LocalTranscriber.TranscribeAsync(
                    audio,
                    modelPath: _config.LocalModelPath,
                    language:  _config.WhisperLanguage);
            }
            else
            {
                transcript = await Transcriber.TranscribeAsync(
                    audio,
                    apiKey:      _config.ApiKey,
                    language:    _config.WhisperLanguage,
                    properNouns: _config.ProperNouns.Count > 0
                                     ? [.. _config.ProperNouns]
                                     : null,
                    model: _config.ActiveWhisperModel);
                _apiAvailable = true;
            }

            AppLog.Add($"Transkript ({mode}): {transcript}");

            // 2. Process (Plus / Rage / Emoji)
            if (mode != "normal")
                OnOverlayProcessing?.Invoke("Formatiere Text...");

            var result = await Processor.ProcessAsync(
                transcript,
                mode:            mode,
                apiKey:          _config.ApiKey,
                promptTemplate:  _config.GetPrompt(mode),
                emojiDensity:    _config.EmojiDensity,
                model:           _config.ActiveChatModel,
                temperature:     _config.Temperature,
                maxTokens:       _config.MaxTokens);

            if (_config.TranscribeMode != "local" && mode != "normal")
                _apiAvailable = true;

            if (mode != "normal")
                AppLog.Add($"Verarbeitet ({mode}): {result}");

            AppLog.SetLast(transcript, result, mode);

            // 3. Apply snippets
            result = Processor.ApplySnippets(result, _config.Snippets);

            // 4. Inject via clipboard paste — atomic, no per-char flickering.
            //    RawSetClipboard uses Win32 directly (no WPF, no focus change).
            var hwnd = _targetHwnd;
            AppLog.Add($"Inject: target={hwnd:X} title='{Injector.GetWindowTitle(hwnd)}'");

            string? oldClip = Injector.RawGetClipboard();
            Injector.RawSetClipboard(result);

            OnOverlayProcessing?.Invoke("Fuege Text ein...");

            await App.Current.Dispatcher.InvokeAsync(() => OnOverlayHideImmediate?.Invoke());
            await Task.Delay(50);

            Injector.SendCtrlV();
            await Task.Delay(150);

            Injector.RawSetClipboard(oldClip);
        }
        catch (Exception ex)
        {
            if (_config.TranscribeMode != "local")
                _apiAvailable = false;
            AppLog.SetError(ex.Message);
            Tray.SetStatus(TrayStatus.Error);
            OnOverlayHide?.Invoke();
            _ = Task.Delay(3000).ContinueWith(_ => RefreshTrayStatus());
            return;
        }
        finally
        {
            audio.Dispose();
            _pipelineLock.Release();
        }

        // Set Ready AFTER lock release so a new recording that started immediately
        // doesn't get overwritten by this Ready call.
        if (!_recordingActive)
            RefreshTrayStatus();
    }

    // ── lifecycle ─────────────────────────────────────────────────────

    public void Quit()
    {
        LocalTranscriber.LoadStatusChanged -= OnLocalModelLoadStatusChanged;
        _hotkeys.Stop();
        _recorder.Dispose();
        Tray.Dispose();
        App.Current.Dispatcher.Invoke(() => App.Current.Shutdown());
    }

    public void Dispose()
    {
        LocalTranscriber.LoadStatusChanged -= OnLocalModelLoadStatusChanged;
        _hotkeys.Stop();
        _recorder.Dispose();
        Tray.Dispose();
    }
}
