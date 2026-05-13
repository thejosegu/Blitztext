# Blitztext MSIX Packaging

Dieses Projekt ist das erste Packaging-Geruest fuer eine spaetere Store- oder Side-Load-MSIX-Version von Blitztext.

## Enthalten

- `Blitztext.Package.wapproj` als Windows Application Packaging Project
- `Package.appxmanifest` mit
  - `runFullTrust`
  - `windows.startupTask` mit `TaskId="BlitztextStartupTask"`
  - `windows.appExecutionAlias`
  - `windows.loaderSearchPathOverride`

## Noch offen

- echte Store-Identity aus dem Partner Center
- Signatur / Zertifikat
- Testlauf als installiertes MSIX
- echte `StartupTask`-Aktivierung im Codepfad der packaged Variante
- Verifikation der Whisper-Runtimes im Paket

## Build-Hinweis

Zum Bauen des Packaging-Projekts werden die Visual-Studio-Komponenten fuer MSIX Packaging bzw. Windows Application Packaging Projects benoetigt. Ohne diese Targets baut das WPF-Hauptprojekt weiterhin, das Packaging-Projekt jedoch nicht.

## VS Code Hinweis

Dieses Repo kann bequem in VS Code gepflegt werden. Das gilt auch fuer:

- `Package.appxmanifest` bearbeiten
- Logos und Paketstruktur vorbereiten
- `Blitztext.Package.wapproj` im Repo halten

Nicht durch eine VS-Code-Extension ersetzt wird derzeit aber das eigentliche WAP-/MSIX-Build-Tooling. Wenn du nur VS Code nutzt, brauchst du zusaetzlich eines der folgenden Setups auf dem Rechner:

1. Visual Studio oder Visual Studio Build Tools mit `MSIX Packaging Tools` / `Windows Application Packaging Project`
2. das separate `MSIX Packaging Tool` fuer Paketierung ausserhalb des Projekt-Builds

Kurz gesagt: VS Code ist hier Editor und Repo-Workflow, nicht der vollstaendige Ersatz fuer das Microsoft-Packaging-Tooling.