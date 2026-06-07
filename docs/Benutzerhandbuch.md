# Blitztext – Benutzerhandbuch

Spracheingabe per Hotkey. Sprechen, loslassen, Text erscheint an der Cursorposition.

---

## Was ist Blitztext?

Blitztext ist eine Windows-Spracherkennung, die im Hintergrund läuft und per Hotkey gesteuert wird. Sobald du eine konfigurierte Tastenkombination drückst und hältst, wird dein Mikrofon aktiviert. Beim Loslassen wird das Gesagte via Whisper-KI transkribiert und per simuliertem Strg + V an der aktuellen Cursorposition eingefügt – in jedem Programm (E-Mail, Chat, Word, Browser, etc.).

**Das Programm läuft im System-Tray und ist nach dem Start unsichtbar – kein Fenster, keine Ablenkung.**

---

## Installation

### Voraussetzungen

- Windows 10 oder 11 (64-bit)
- Ein Mikrofon
- Ein API-Key von [OpenAI](https://platform.openai.com/) oder [Groq](https://groq.com/) (kostenloses Kontingent verfügbar) – oder das lokale Whisper-Modell

### Portable EXE

1. Den Ordner `dist\` entpacken und an einen beliebigen Ort kopieren
2. `Blitztext.exe` starten

Die EXE ist eine selbstenthaltene Single-File-Anwendung – keine Installation nötig, kein .NET Runtime erforderlich.

---

## Ersteinrichtung

1. **API-Key hinterlegen**: Rechtsklick auf das Tray-Icon → *Einstellungen* → Tab *Allgemein*. Trage deinen API-Key ein (OpenAI beginnt mit `sk-`, Groq mit `gsk_`). Der Key wird sicher in einer `.env`-Datei gespeichert und nie in die Konfigurationsdatei geschrieben.

2. **Sprache wählen**: Wähle deine Aufnahmesprache (z. B. *Deutsch*). *Auto* erkennt die Sprache automatisch.

3. **Aufnahmemodus wählen**:
   - **Halten**: Hotkey gedrückt halten während des Sprechens, loslassen zum Verarbeiten
   - **Umschalten**: Einmal drücken startet die Aufnahme, erneutes Drücken beendet sie

4. **Speichern** klicken.

---

## Bedienung

### Aufnahme starten

Drücke den konfigurierten Hotkey und sprich. Während der Aufnahme erscheint oben mittig ein halbtransparentes Overlay mit einem pulsierenden roten Punkt.

### Verarbeitung

Nach dem Loslassen des Hotkeys (Hold-Modus) bzw. dem zweiten Tastendruck (Toggle-Modus):
1. Das Overlay zeigt einen orangen Punkt (*Verarbeitung…*)
2. Die Audioaufnahme wird an Whisper gesendet
3. Der transkribierte Text wird an der Cursorposition eingefügt
4. Eine Toast-Benachrichtigung unten rechts bestätigt die Einfügung mit einer Textvorschau

### Tray-Icon

- **Grün**: Bereit
- **Rot**: Aufnahme läuft
- **Orange**: Verarbeitung läuft
- **Grau**: Fehler

Rechtsklick auf das Icon öffnet das Menü mit *Einstellungen* und *Beenden*.

---

## Die vier Modi

Blitztext bietet vier Aufnahmemodi mit eigenen Hotkeys:

### Normal

Reine Transkription. Das Gesagte wird 1:1 in Text umgewandelt.

**Standard-Hotkey**: Rechte Strg-Taste

### Plus

Formuliert den gesprochenen Text um – natürlicher, flüssiger, schriftsprachlich. Bedeutung und Länge bleiben erhalten.

**Standard-Hotkey**: Rechte Strg + Rechte Shift

### Rage

Wandelt wütende oder frustrierte Sprache in eine höfliche, professionelle Nachricht um.

**Standard-Hotkey**: Pfeil links + Pfeil rechts

### Emoji

Fügt passende Emojis in den transkribierten Text ein.

**Standard-Hotkey**: F8 + F9

> Die System-Prompts für Plus, Rage und Emoji können im Tab *Modi* der Einstellungen angepasst werden.

---

## Einstellungen

Die Einstellungen sind in sechs Tabs gegliedert:

### Allgemein

| Einstellung | Beschreibung |
|---|---|
| API-Key | OpenAI- oder Groq-Key (wird nicht in config.json gespeichert) |
| Sprache | Whisper-Sprache (auto, de, en, fr, es, it, pt, nl, pl, ru, zh, ja) |
| Aufnahmemodus | Halten oder Umschalten |
| Transkription | Online (API) oder Lokal (Whisper.net GGML) |
| Modellpfad | Pfad zum lokalen Whisper-Modell (nur bei lokalem Modus) |
| Autostart | Startet Blitztext automatisch mit Windows |

### Hotkeys

Für jeden der vier Modi kann eine eigene Tastenkombination festgelegt werden. Auf *Aufnehmen* klicken und die gewünschte Taste bzw. Kombination drücken.

**Empfehlung**: Rechte Modifikatortasten (Rechte Strg, Rechte Alt, Rechte Shift) verwenden, da sie selten mit anderen Programmen kollidieren.

### Modi

Hier können die System-Prompts für Plus, Rage und Emoji bearbeitet werden. Außerdem kann die Emoji-Dichte (1–10) eingestellt werden – je höher, desto mehr Emojis werden eingefügt.

### Snippets

Textbausteine, die automatisch ersetzt werden. Beispiel:

| Stichwort | Ersetzung |
|---|---|
| mfg | Mit freundlichen Grüßen |

Wenn du "mfg" sprichst, wird es automatisch zu "Mit freundlichen Grüßen" erweitert.

### Eigennamen

Eine Liste von Namen, Marken oder Fachbegriffen, die Whisper besser erkennen soll. Wird als *initial_prompt* an das Modell übergeben.

### Info

Zeigt Diagnoseinformationen:
- API-Status und verwendeter Provider
- Hotkey-Übersicht
- Letzte Transkription, Verarbeitung und Fehler
- Ereignisprotokoll (letzte 100 Einträge)

---

## Lokales Whisper-Modell

Alternativ zur Cloud-API kann Blitztext ein lokales Modell nutzen. Dies läuft komplett offline auf deiner CPU.

Das lokale Modell wird **nicht automatisch heruntergeladen** – es muss manuell bereitgestellt werden:

1. Lade die GGML-Modelldatei `ggml-small.bin` (~465 MB) herunter:  
   **https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin**

2. Lege die Datei neben die `Blitztext.exe` (im selben Verzeichnis) oder an einen beliebigen Ort.

3. In den Einstellungen: *Transkription* → *Lokal (Whisper.net)* wählen und den Pfad zur Datei angeben (über *Durchsuchen…* auswählen).

4. Das Modell wird beim ersten Start geladen – dies kann einige Sekunden dauern. Danach steht es sofort zur Verfügung.

Verwendet wird `ggml-small.bin` aus dem [ggerganov/whisper.cpp](https://huggingface.co/ggerganov/whisper.cpp) Projekt auf HuggingFace.

> Andere Modellgrößen (`ggml-tiny.bin`, `ggml-base.bin`, `ggml-medium.bin`) sind ebenfalls kompatibel und können über denselben HuggingFace-Pfad bezogen werden.

---

## Tastenkombinationen (Standard)

| Modus | Hotkey |
|---|---|
| Normal | Rechte Strg |
| Plus | Rechte Strg + Rechte Shift |
| Rage | Pfeil links + Pfeil rechts |
| Emoji | F8 + F9 |

Unterstützte Tasten: `ctrl_r`, `ctrl_l`, `alt_r`, `alt_l`, `shift_r`, `shift_l`, `cmd_r`, `cmd_l`, `Leertaste`, `F13`–`F15`, `Rollen`, `Pause`, sowie einzelne Zeichentasten.

---

## Fehlerbehebung

| Problem | Lösung |
|---|---|
| Keine Aufnahme | API-Key in den Einstellungen prüfen (oder lokales Modell aktivieren) |
| Mikrofon wird nicht erkannt | Standardmikrofon in Windows-Soundeinstellungen prüfen |
| Text wird nicht eingefügt | Zielanwendung prüfen – nicht alle Programme unterstützen simuliertes Strg+V (z. B. manche Terminal-Programme) |
| Tray-Icon wird grau | API-Fehler – Ereignisprotokoll im Tab *Info* prüfen |
| Hotkey reagiert nicht | Anderes Programm belegt die Tastenkombination; in den Einstellungen ändern |
| Lokales Modell lädt nicht | Prüfen, ob die Datei `ggml-small.bin` vorhanden und der Pfad korrekt ist |

---

## Deinstallation

1. Rechtsklick auf das Tray-Icon → *Beenden*
2. Falls Autostart aktiviert: In den Einstellungen deaktivieren und speichern
3. Den Programmordner löschen
4. Konfigurationsdateien (`config.json`, `.env`) werden neben der EXE gespeichert

---

## Lizenz & Credits

Blitztext verwendet:
- [OpenAI Whisper](https://platform.openai.com/docs/guides/speech-to-text) / [Groq Whisper](https://console.groq.com/docs/speech-to-text) für Spracherkennung
- [Whisper.net](https://github.com/sandrohanea/whisper.net) für lokale Transkription (whisper.cpp-Binding für .NET)
- [ggerganov/whisper.cpp](https://github.com/ggerganov/whisper.cpp) – die zugrundeliegende C/C++-Engine
- [NAudio](https://github.com/naudio/NAudio) für Audioaufnahme
