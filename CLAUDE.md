# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Blitztext** is a Windows system-tray speech-to-text app (WPF + C# 12 / .NET 10). It records audio via a global hotkey, transcribes via Whisper (local GGML model or OpenAI/Groq API), optionally post-processes the text through a ChatGPT/Groq prompt (Plus/Rage/Emoji modes), applies snippet replacements, then injects the result at the cursor via Win32 clipboard + `SendInput(Ctrl+V)`.

## Build & Run

```powershell
# Build
dotnet build

# Run (launches the tray app)
dotnet run

# Publish – self-contained single-file (large, no .NET runtime required)
dotnet publish -c Release /p:PublishProfile=LocalPortable

# Publish – framework-dependent single-file (small, requires installed runtime)
dotnet publish -c Release /p:PublishProfile=LocalSlim
```

There are no automated tests in this repository.

## VS Code + MSIX

The main WPF project can be developed fully in VS Code.

The packaging project `Blitztext.Package/Blitztext.Package.wapproj` is different: it depends on Microsoft WAP/MSIX tooling that is normally provided by Visual Studio or Visual Studio Build Tools. There is currently no equivalent VS Code extension in this repo workflow that replaces those packaging targets.

Practical consequence:

- Use VS Code for app code, manifest edits, and packaging-file maintenance.
- Use installed Microsoft packaging tooling to actually build or validate the `.wapproj`.

## Architecture

The pipeline is linear and locked: only one recording/transcription/injection can run at a time via a `SemaphoreSlim(1,1)` in `BlitztextApp.cs`.

```
HotkeyListener (Win32 WH_KEYBOARD_LL hook thread)
  → BlitztextApp.OnRecordingStart / OnRecordingStop
    → AudioRecorder (NAudio WaveInEvent, 16 kHz mono WAV)
    → RunPipelineAsync [background Task, exclusive lock]
        → Transcriber (OpenAI/Groq API)  OR  LocalTranscriber (Whisper.net GGML)
        → Processor.ProcessAsync (ChatGPT/Groq for Plus/Rage/Emoji modes, optional)
        → Processor.ApplySnippets (keyword → expansion)
        → Injector.RawSetClipboard + SendCtrlV (Win32 clipboard, no WPF)
```

`BlitztextApp` is the central orchestrator and wires all side-effects (overlay, tray icon, toast notifications) through `Action<>` callbacks rather than direct dependencies.

### Key files

| File | Role |
|------|------|
| `BlitztextApp.cs` | Orchestrator; hotkey callbacks, pipeline sequencing, state tracking |
| `Core/HotkeyListener.cs` | Win32 `WH_KEYBOARD_LL` hook; 50 ms debounce; hold/toggle modes; multi-key combos |
| `Core/AudioRecorder.cs` | NAudio `WaveInEvent`; 16 kHz mono; discards clips < 300 ms; explicit device lifecycle |
| `Core/Transcriber.cs` | OpenAI/Groq Whisper multipart API upload; provider detected by key prefix |
| `Core/LocalTranscriber.cs` | Whisper.net GGML wrapper; lazy-loads and caches model; call `Unload()` to free VRAM |
| `Core/Processor.cs` | ChatGPT/Groq chat completion for mode-specific prompts; `ApplySnippets` text replacement |
| `Core/Injector.cs` | Raw Win32 clipboard (no WPF); `SendInput` Ctrl+V; Unicode typing fallback |
| `Core/AppConfig.cs` | `config.json` + `.env` beside the executable; falls back to AppData if the local folder is not writable; `DetectProvider()` checks key prefix |
| `Core/AppLog.cs` | Ring buffer (100 entries) + async file append to `blitztext.log` beside the executable, with AppData fallback |
| `Tray/TrayManager.cs` | `Shell_NotifyIcon` tray icon; runtime GDI+ icon generation; context menu |
| `UI/RecordingOverlay.xaml.cs` | Top-center blinking dot overlay (red = recording, amber = processing) |
| `UI/SettingsWindow.xaml.cs` | Tabbed settings UI; delegates to `UI/Tabs/` controllers |

### Configuration & storage

Runtime state is stored beside the executable when possible, with AppData fallback if that folder is not writable:
- `config.json` — hotkeys, prompts, snippets, model selection, all user prefs
- `.env` — `GROQ_API_KEY=...` (read at startup, merged into config)
- `blitztext.log` — ring-buffered operation log

### Hotkey modes

`HotkeyListener` supports two recording modes (configurable per-hotkey):
- **hold** — recording starts on `KeyDown`, stops on `KeyUp`
- **toggle** — first press starts, second press stops; short-tap guard preserves clips

The 50 ms debounce uses a generation counter (`Interlocked`) to cancel stale timers; this is load-bearing — see `docs/C_SHARP_RUNTIME_FINDINGS.md` for the race conditions it prevents.

### Provider detection

`AppConfig.DetectProvider()` inspects the key prefix (`gsk_` → Groq, else OpenAI). `Transcriber` and `Processor` both use `IsGroq` to switch base URLs and model defaults; no separate config flag is needed.

### UI threading

All WPF updates from background tasks must go through `Dispatcher.Invoke` / `BeginInvoke`. The overlay and tray icon updates in `BlitztextApp` use the `Action<>` callback pattern to keep `BlitztextApp` decoupled from WPF.

## Known complexity areas

`docs/C_SHARP_RUNTIME_FINDINGS.md` (German) documents six resolved bugs: config path discrepancies, start/stop race conditions, recorder hangs, auto-repeat key events, toggle state rollback, and short-tap loss. Read it before touching `HotkeyListener` or `AudioRecorder`.
