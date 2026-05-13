dotnet run --project .\Blitztext.csproj -c Debug

dotnet publish .\Blitztext.csproj -c Release

dotnet publish .\Blitztext.csproj -c Release /p:PublishProfile=LocalPortable

dotnet publish .\Blitztext.csproj -c Release /p:PublishProfile=LocalSlim

Hinweis: `dotnet publish` ohne explizites Projekt kann am Repo-Root die Solution inklusive `Blitztext.Package.wapproj` erwischen. Das MSIX-Projekt braucht Visual-Studio-Packaging-Targets und ist nicht der normale App-Publish-Pfad.

Hinweis: `LocalPortable` ist aktuell konservativ unkomprimiert. Eine separat getestete komprimierte Single-File-Variante kam hier auf ~70 MB und startete ebenfalls, aber nur als Tray-App ohne sichtbares Fenster. Der frühere Eindruck "startet nicht" ist damit aktuell nicht reproduziert; offen ist eher, ob beim Start nur kein sichtbares UI erschien oder ob eine lokale Umgebungsbesonderheit beteiligt war.

Hinweis: `config.json` und `.env` sind keine verlässlich mitveröffentlichten Dateien des Publish-Outputs. Ein frischer Publish kann daher mit Defaults starten, bis Konfiguration gespeichert oder Dateien manuell bereitgestellt werden.