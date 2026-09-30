# TopBar

Schlanke Statusleiste am oberen Rand von Windows 11, ähnlich Waybar. Gebaut mit WinUI 3 und .NET 10.

- **Links:** virtuelle Desktops als Punkte, der aktive als animierte Pille (Klick wechselt den Desktop)
  daneben ein Pomodoro-Timer (Klick startet/pausiert, Stopp-Symbol setzt zurück; Zeiten einstellbar)
- **Mitte:** Wetter · Uhr · Datum
- **Rechts:** CPU, GPU, RAM, Netzwerk und ein Zahnrad für die Einstellungen

Die Leiste registriert sich als AppBar: Andere Fenster überlappen sie nicht. Sie blendet sich bei Vollbild-Apps aus, unterstützt mehrere Monitore und nutzt einen Acrylic-Hintergrund. Die Oberfläche ist auf Deutsch.

> **Status:** Baut, alle 90 Unit-Tests laufen, die App startet und zeigt die Leiste. Die manuelle Abnahme (Spec §14) ist noch nicht vollständig abgehakt.

## Hinweis: Vibe-coded

Der gesamte Code dieses Projekts ist per Vibe-Coding mit einem KI-Assistenten (Claude) entstanden, auf Basis einer von mir geschriebenen Spezifikation (`TOPBAR_SPEC.md`). Ich habe ihn getestet und ausprobiert, aber nicht Zeile für Zeile geprüft. Rechne mit Ecken und Kanten, und nutze es auf eigenes Risiko.

## Voraussetzungen

- Windows 11, x64. Für die virtuellen Desktops **24H2 (Build ≥ 26100.2605)**, wegen `VirtualDesktopAccessor.dll`. Auf älteren Builds bleibt der linke Bereich ausgeblendet.
- .NET 10 SDK
- Keine Administratorrechte nötig, weder zum Bauen noch zum Ausführen

## Bauen, testen, starten

```powershell
dotnet build -c Debug
dotnet test
dotnet run --project src/TopBar.App
```

Veröffentlichen als eigenständige EXE (ohne installierte .NET- oder Windows-App-SDK-Runtime):

```powershell
dotnet publish src/TopBar.App -c Release -r win-x64 --self-contained true -o publish
```

`publish/TopBar.exe` läuft direkt. Es gibt keinen Installer und kein MSIX.

Hinweis: Der erste Start trägt laut Spec den Autostart-Eintrag `HKCU\...\Run\TopBar` ein. Bei Debug-Builds zeigt er auf die Debug-EXE. Zum Entwickeln die Einstellung „Mit Windows starten“ ausschalten.

## Dateien zur Laufzeit

- Einstellungen: `%LOCALAPPDATA%\TopBar\settings.json` (eine defekte Datei wird zu `settings.broken-<Zeitstempel>.json` gesichert)
- Logs: `%LOCALAPPDATA%\TopBar\logs\topbar-<Datum>.log` (7 Tage)

## Projektstruktur

- `src/TopBar.Core`: reine Logik ohne UI und Win32 (Einstellungen, Layout, Formatierung, Wettercodes, GPU-Aggregation)
- `src/TopBar.Platform`: Systemzugriff (AppBar, Performance-Zähler, virtuelle Desktops, Registry, HTTP). Alle Win32-Aufrufe laufen über CsWin32.
- `src/TopBar.App`: WinUI-3-App (Fenster, Controls, ViewModels, Einstellungsseiten)
- `tests/TopBar.Core.Tests`: xUnit-Tests

Weitere Dokumente: `TOPBAR_SPEC.md` (Spezifikation), `DECISIONS.md` (Entscheidungen und Abweichungen), `PROGRESS.md` (Stand).

## Bekannte Einschränkungen

- Paketversionen sind teilweise noch Floating-Versionen (siehe `DECISIONS.md`).
- Datumsnamen in der Leiste hängen von den ICU-Daten des Systems ab (z. B. „Sept.“ statt „Sep.“).
- Wetterdaten kommen von [Open-Meteo](https://open-meteo.com/) (kein API-Schlüssel nötig).

## Lizenz

[MIT](LICENSE). Mitgelieferte Fremdkomponenten siehe `THIRD_PARTY_NOTICES.md`.
