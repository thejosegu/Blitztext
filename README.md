# Blitztext

Spracherkennung per Hotkey für Windows. Drücken, sprechen, loslassen – der transkribierte Text erscheint an der aktuellen Cursorposition.

## Installation

1. Den Ordner `dist\` an einen beliebigen Ort kopieren
2. `Blitztext.exe` starten

Selbstenthaltene Single-File-EXE – keine Installation nötig, kein .NET Runtime erforderlich.

## Lokales Whisper-Modell

Das lokale Sprachmodell wird **nicht** mitgeliefert und muss manuell heruntergeladen werden:

1. **`ggml-small.bin`** (~465 MB) herunterladen:  
   https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin

2. Die Datei neben die `Blitztext.exe` legen oder Pfad in den Einstellungen angeben.

3. Transkription auf *Lokal (Whisper.net)* stellen und Modellpfad auswählen.

Verwendet wird [ggerganov/whisper.cpp](https://github.com/ggerganov/whisper.cpp) via [Whisper.net](https://github.com/sandrohanea/whisper.net).

## Bedienungsanleitung

Siehe [docs/Benutzerhandbuch.md](docs/Benutzerhandbuch.md) für die vollständige Anleitung.

## Build

```powershell
dotnet build
dotnet publish -c Release /p:PublishProfile=LocalPortable   # → dist\
```

## Lizenz & Credits

- Spracherkennung: [OpenAI Whisper](https://platform.openai.com/docs/guides/speech-to-text) / [Groq](https://groq.com/) / [Whisper.net](https://github.com/sandrohanea/whisper.net)
- Audio: [NAudio](https://github.com/naudio/NAudio)
- GUI: WPF / .NET 10
