# Blitztext Store Readiness

Stand: 2026-05-05

## Ziel

Diese Checkliste bewertet Blitztext fuer eine spaetere Veroeffentlichung im Microsoft Store als verpackte Desktop-App (MSIX / packaged full-trust app).

Sie ist keine Garantie fuer die Zertifizierung. Sie soll vor allem die technisch und policy-seitig riskanten Bereiche frueh sichtbar machen.

## Kurzfazit

- Blitztext ist technisch paketierbar.
- Die groessten Store-Risiken liegen nicht in WPF oder Whisper, sondern in globalem Keyboard-Hook und aktivem Texteinfuegen in fremde Anwendungen.
- Vor einer Submission muessen der vorbereitete Autostart-Schnitt und die neue Speichertrennung noch mit einem echten MSIX-Projekt abgeschlossen und verifiziert werden.

## Einstufung

- Unkritisch: normales WPF-UI, Tray, Settings, lokale und Remote-Transkription an sich
- Kritisch, aber loesbar: Autostart, Speichern von Laufzeitdaten, Packaging von nativen DLLs
- Hohes Risiko: globaler Hotkey-Hook
- Hoechstes Risiko: Clipboard- und SendInput-basierte Texteinspeisung in andere Apps

## Checkliste

| Bereich | Aktueller Stand in Blitztext | Risiko | Vor Store-Submission noetig |
|---|---|---|---|
| Globale Hotkeys | `WH_KEYBOARD_LL` Hook in `Core/HotkeyListener.cs` | Hoch | Klar dokumentieren, warum globaler Hook erforderlich ist; Datenschutztext ergaenzen; real mit MSIX testen |
| Texteinfuegen in andere Apps | Clipboard + `SendInput(Ctrl+V)` + Unicode Input in `Core/Injector.cs` | Sehr hoch | Als Kernrisiko behandeln; reale Store-Kompatibilitaet und Policy-Risiko frueh pruefen |
| Mikrofonaufnahme | NAudio `WaveInEvent` in `Core/AudioRecorder.cs` | Mittel | Mikrofon-Capabilities und Privacy-Hinweise sauber abdecken |
| Remote APIs | OpenAI/Groq in `Core/Transcriber.cs` und `Core/Processor.cs` | Mittel | Datenschutzerklaerung, Anbieterhinweise und Fehlerfaelle sauber beschreiben |
| Lokales Whisper | `Whisper.net` + native Runtime | Mittel | MSIX mit nativen Dateien und Modellpfaden real testen |
| Tray-App-Verhalten | `Shell_NotifyIcon` in `Tray/TrayManager.cs` | Niedrig | MSIX-installiert testen, ob Tray und Kontextmenue stabil laufen |
| Overlay / Fenster | Normale WPF-Fenster | Niedrig | Nur Regressionstest noetig |
| Autostart | Abstrahiert ueber `Core/AutostartService.cs`; `windows.startupTask` jetzt im Paketmanifest vorbereitet | Mittel | Echte Aktivierung im paketierten Lauf testen und WinRT-Projektion/Codepfad finalisieren |
| Konfiguration / Logs | Abstrahiert ueber `Core/AppStorage.cs`; packaged nutzt AppData-kompatible Nutzerpfade | Niedrig bis mittel | Im echten MSIX-Lauf testen |
| Schreiben ins Programmverzeichnis | In unpackaged Version vorgesehen | Mittel | Fuer MSIX nicht voraussetzen; Paketpfad ist read-only |
| Native DLL-Suche | Whisper native libraries neben EXE | Mittel | Bei MSIX ggf. `loaderSearchPathOverride` oder passende Paketstruktur pruefen |
| Updates | Noch kein Store-Updatefluss | Niedrig | Paketversionierung und Restart-Verhalten pruefen |

## Wahrscheinliche Showstopper

### 1. Globaler Keyboard-Hook

Blitztext lauscht systemweit auf Tastenkombinationen. Das ist technisch fuer klassische Desktop-Apps normal, aber aus Store-Sicht sensibel. Vor allem muss klar sein:

- Es werden nur Hotkeys erkannt.
- Es werden keine allgemeinen Tastatureingaben protokolliert.
- Es gibt keine verdeckte Hintergrundueberwachung.

Wenn dieser Punkt im Listing und in der Privacy-Kommunikation unsauber wirkt, ist das ein realistischer Ablehnungsgrund.

### 2. Texteinspeisung in fremde Anwendungen

Blitztext fuegt Text aktiv in andere Apps ein. Das geschieht ueber Clipboard-Manipulation und simulierte Tastenanschlaege. Genau dieses Verhalten ist funktional zentral, aber policy-seitig der riskanteste Teil des Produkts.

Wenn es eine Store-Ablehnung gibt, ist dieser Bereich der wahrscheinlichste Kandidat.

## Noetige technische Umbauten vor MSIX

### A. Autostart auf Store-Modell umstellen

Aktuell ist Autostart im Code bereits getrennt:

- unpackaged: Registry `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`
- packaged: separater Service-Pfad, der bewusst noch ein MSIX-Projekt mit `windows.startupTask` erwartet

Fuer Store / MSIX fehlt deshalb noch der letzte Schritt: Manifest-Extension `windows.startupTask` plus die echte Aktivierung ueber den paketierten Codepfad.

### B. Speicherstrategie fuer Store sauber abgrenzen

MSIX installiert in einen schreibgeschuetzten Paketpfad. Schreiben neben die EXE darf fuer die Store-Version nicht vorausgesetzt werden.

Dieser Schnitt ist jetzt im Code bereits vorhanden:

- unpackaged / portable Version: lokaler Ordner bevorzugt, AppData-Fallback
- Store / packaged Laufzeit: AppData als expliziter Standard

### C. Native Whisper-Dateien in MSIX testen

Whisper.net und die nativen Laufzeitdateien muessen in der verpackten App tatsaechlich aufgeloest werden.

Zu pruefen:

- Startet die App verpackt?
- Wird das lokale Modell gefunden?
- Werden native DLLs korrekt geladen?
- Muss ein `loaderSearchPathOverride` ins Manifest?

## Empfohlene Reihenfolge

1. Produkt- und Privacy-Text fuer Hook + Injektion vorbereiten
2. Store-kritische Architekturunterschiede fuer Autostart und Speicher festlegen
3. Erste MSIX-Paketierung lokal aufsetzen
4. Lokalen Paket-Test auf einem sauberen Windows-System durchfuehren
5. Erst danach Partner-Center-Submission vorbereiten

## Praktische Entscheidung vor dem Packaging

Vor dem eigentlichen MSIX-Projekt sollte diese Frage intern beantwortet sein:

> Wollen wir Blitztext im Store exakt mit globalem Hook und aktivem Texteinfuegen vertreiben, oder brauchen wir fuer den Store eine eingeschraenkte Variante?

Wenn die Antwort unklar ist, sollte zuerst genau dieser Produktentscheid getroffen werden. Sonst wird spaeter die Paketierung gebaut, obwohl das Kernverhalten eventuell die eigentliche Huerde ist.

## Empfehlung fuer den naechsten Schritt

Wenn Blitztext in den Store soll, ist der sinnvollste naechste technische Schritt:

1. Store-spezifische Delta-Liste ableiten
2. Autostart auf StartupTask umstellen
3. Speicherpfade fuer Store-Modus explizit definieren
4. Danach MSIX-Paketierung einrichten

## Aktueller Packaging-Stand

Im Repo liegt jetzt ein erstes Packaging-Geruest unter `Blitztext.Package/`:

- `Blitztext.Package.wapproj`
- `Package.appxmanifest`
- vorbereitete `windows.startupTask`-Extension mit `TaskId="BlitztextStartupTask"`
- vorbereitete `windows.appExecutionAlias`-Extension
- `runFullTrust`, Mikrofon-Capability und einfacher `loaderSearchPathOverride`

Noch nicht erledigt ist damit bewusst:

- echte Store-Identity aus dem Partner Center
- Zertifikat / Signatur
- installierter MSIX-Test
- finale packaged Autostart-Aktivierung im Code

## VS Code und MSIX

Fuer dieses Repo ist wichtig: Visual Studio Code reicht fuer die normale Entwicklung und fuer die unpackaged Builds, aber nicht allein fuer das klassische WAP-/MSIX-Tooling.

- VS Code kann `Blitztext.csproj` normal bauen, starten und publishen
- VS Code hat nach heutigem Stand keine offizielle Extension, die `Windows Application Packaging Project` oder die Visual-Studio-`MSIX Packaging Tools` ersetzt
- Das Packaging-Projekt `Blitztext.Package.wapproj` braucht weiterhin externes Microsoft-Tooling

Praktisch gibt es dafuer zwei Wege:

1. Visual Studio oder Visual Studio Build Tools mit `MSIX Packaging Tools` bzw. Support fuer `Windows Application Packaging Project`
2. das separate `MSIX Packaging Tool` fuer Konvertierung und Paketbearbeitung

Empfohlener Workflow fuer dieses Repo:

1. Code, Manifest und Paketstruktur in VS Code pflegen
2. das eigentliche MSIX-Bauen mit installiertem WAP-/MSIX-Tooling ausfuehren
3. den installierten Paketlauf real auf Windows testen

## Partner Center und Kommunikation

Neben der Technik gibt es beim Microsoft Store ein paar kommunikative Fallstricke, die bei Anmeldung, Registrierung, Einreichung und Freigabe frueh sauber geklaert sein sollten.

### 1. Konto und Verifikation

- Fuer das Partner-Center-Konto genug Zeit einplanen. Identitaets- und Firmenpruefungen koennen Tage dauern.
- Firmenname, Rechtsform, Ansprechpartner, Adresse und Zahlungsdaten konsistent halten. Unterschiedliche Schreibweisen fuehren schnell zu Rueckfragen.
- Den kuenftigen Publisher-Namen frueh festlegen. Er ist spaeter im Store sichtbar und sollte zu Website, Datenschutzseite und Support-Mail passen.
- Eine belastbare Support-Adresse verwenden, nicht nur eine private Wegwerf-Adresse.

### 2. Produktname und Markenrisiko

- Vor der Reservierung pruefen, ob `Blitztext` als Store-Name, Publisher-Name und Marke unkritisch ist.
- Namen, Untertitel und Kurzbeschreibung nicht mit generischen oder markenrechtlich riskanten Begriffen ueberladen.
- Falls spaeter noch umbenannt werden koennte, das vor dem ersten Listing entscheiden, nicht erst nach Screenshots, Datenschutztexten und Paket-Identity.

### 3. Listing-Text: heikle Punkte aktiv entschärfen

Bei Blitztext sollte das Listing nicht defensiv, aber sehr klar formulieren, was die App tut und was sie nicht tut.

- Globaler Hotkey: klar sagen, dass nur definierte Hotkeys erkannt werden.
- Keine Keylogger-Wirkung: klar sagen, dass keine allgemeinen Tastatureingaben protokolliert oder gespeichert werden.
- Mikrofon: klar sagen, dass Sprache zur Transkription aufgenommen wird.
- Remote-Verarbeitung: klar sagen, dass je nach Modus Daten an externe KI-Dienste wie OpenAI oder Groq gesendet werden koennen.
- Lokaler Modus: klar sagen, wenn Transkription auch lokal ohne Cloud moeglich ist.
- Texteinfuegen: klar sagen, dass die App transkribierten Text aktiv in andere Anwendungen einfuegen kann.

Wenn diese Punkte im Listing verschwiegen oder weichgespuelt wirken, steigt das Risiko fuer Rueckfragen oder Ablehnung.

### 4. Datenschutz und Einwilligung

- Vor der Einreichung eine echte Datenschutzseite bereitstellen, nicht nur einen Platzhalter.
- Die Datenschutzseite muss explizit erklaeren:
	- welche Audiodaten verarbeitet werden
	- wann Daten lokal bleiben
	- wann Daten an Drittdienste gehen
	- wo Konfiguration, Logs und API-Schluessel gespeichert werden
	- wie Nutzer Support oder Loeschung anfragen koennen
- App-Beschreibung, Datenschutzseite und In-App-Texte muessen dieselbe Geschichte erzaehlen. Widersprueche sind gefaehrlich.

### 5. Screenshots und erste Freigabe

- Keine Screenshots verwenden, die mehr versprechen als die erste eingereichte Version wirklich kann.
- Bei einem sensiblen Produkt wie Blitztext lieber konservativ listen: klare Settings, Mikrofon, Transkriptionsfluss, lokaler Modus, aber keine zu aggressiven Automationsclaims.
- Fuer die erste Freigabe eine kleine, moeglichst stabile Version bevorzugen statt sofort jede Zusatzfunktion mitzunehmen.

### 6. Freigabe-Strategie

- Die erste Submission nicht als Marketing-Launch behandeln, sondern als Review-Test.
- Wenn moeglich zuerst mit begrenzter Sichtbarkeit, privater Zielgruppe oder sehr kontrollierter Freigabe arbeiten.
- Release Notes und Submission Notes fuer die Reviewer aktiv nutzen.

Gerade fuer Blitztext sollten die Reviewer-Hinweise kurz erklaeren:

- warum globale Hotkeys benoetigt werden
- dass keine allgemeinen Tastatureingaben mitgelesen werden
- warum Texteinfuegen funktional zum Produkt gehoert
- dass Mikrofon und optional externe KI-Dienste Teil des Produktzwecks sind

### 7. Praktische Checkliste vor der ersten Einreichung

Vor `Submit` sollte mindestens alles Folgende fertig sein:

- finaler Publisher-Name
- reservierter App-Name
- Datenschutz-URL
- Support-URL oder Support-Mail
- konsistente Produktbeschreibung fuer Store und Website
- ehrliche Screenshots
- klare Reviewer-Notiz fuer Hook, Mikrofon und Texteinfuegen
- Entscheidung, ob Cloud-Modi standardmaessig aktiv oder opt-in sein sollen

## Empfehlung fuer den organisatorischen naechsten Schritt

Vor dem weiteren Packaging-Aufwand sollte zuerst die Store-Basis stehen:

1. Partner-Center-Konto einrichten und verifizieren
2. App-Namen reservieren
3. Publisher-, Support- und Datenschutztexte festziehen
4. erst danach die erste echte MSIX-Submission vorbereiten