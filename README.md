# TopBar

Schlanke Status-Bar am oberen Rand von Windows 11 (WinUI 3, .NET 10). Links virtuelle Desktops, in der Mitte Wetter · Uhr · Datum, rechts CPU/GPU/RAM/Netzwerk und Einstellungen. Die vollständige Spezifikation steht in `TOPBAR_SPEC.md`, getroffene Entscheidungen in `DECISIONS.md`, der Stand in `PROGRESS.md`.

> **Status:** Der gesamte Code wurde ohne .NET-SDK geschrieben und ist **noch nie gebaut oder gestartet worden**. Erwarte beim ersten Build Compilerfehler (siehe „Bekannte Risiken“).

## Voraussetzungen

- Windows 11 (für virtuelle Desktops: **24H2, Build ≥ 26100.2605**, wegen `VirtualDesktopAccessor.dll`), x64
- .NET 10 SDK
- Visual Studio 2022/2026 mit Workload „.NET-Desktopentwicklung“ *oder* nur das SDK (WinUI 3 baut auch per `dotnet build`; falls nicht: Workload „Windows App SDK C# Templates“ installieren)
- Keine Administratorrechte nötig – weder zum Bauen noch zum Ausführen

## Bauen, testen, veröffentlichen

```powershell
dotnet build -c Debug
dotnet test
dotnet publish src/TopBar.App -c Release -r win-x64 --self-contained true -o publish
```

`publish/TopBar.exe` läuft ohne installierte .NET-Runtime und ohne Windows-App-SDK-Runtime (unpackaged, self-contained). Kein Installer, kein MSIX. `VirtualDesktopAccessor.dll` und die Icon-Schrift liegen nach dem Publish neben bzw. unter der EXE.

Hinweis: Der erste Start schreibt (laut Spec) den Autostart-Eintrag `HKCU\...\Run\TopBar`. Bei Debug-Builds zeigt er auf die Debug-EXE – zum Testen die Einstellung „Mit Windows starten“ ausschalten.

## Dateien zur Laufzeit

- Einstellungen: `%LOCALAPPDATA%\TopBar\settings.json` (defekte Datei → `settings.broken-<Zeitstempel>.json`)
- Logs: `%LOCALAPPDATA%\TopBar\logs\topbar-<Datum>.log` (7 Tage)

## Projektstruktur

- `src/TopBar.Core` – reine Logik ohne UI/Win32 (Interfaces, Settings, Layout-Mathematik, Formatierung, Wettercodes, GPU-Aggregation) – getestet
- `src/TopBar.Platform` – Win32/Systemzugriff (AppBar, PDH, virtuelle Desktops, Registry, HTTP), alle Win32-Aufrufe per CsWin32
- `src/TopBar.App` – WinUI-3-App (Fenster, Controls, ViewModels, Einstellungsseiten)
- `tests/TopBar.Core.Tests` – xUnit

## Mitgelieferte Fremdkomponenten

- `VirtualDesktopAccessor.dll` – <https://github.com/Ciantic/VirtualDesktopAccessor> (MIT), Release „latest“ am 2026-09-29
- `FluentSystemIcons-Regular.ttf` – <https://github.com/microsoft/fluentui-system-icons> (MIT)

## Bekannte Risiken beim ersten Build

1. **CsWin32-Signaturen.** Die generierten Signaturen konnte ich nicht prüfen. Am ehesten betroffen: `PdhGetFormattedCounterValue` / `PdhGetFormattedCounterArray` (Zeigerparameter, `PDH_FMT_COUNTERVALUE_ITEM_W`), `EnumDisplayMonitors` (Lambda mit `RECT*`), `CreateWindowEx`, `GetWindowLong`/`SetWindowLong` (nint vs. int), `SHAppBarMessage`. Meist reicht es, in Visual Studio auf den Fehler zu gehen und die Überladung anzupassen (Zeiger ↔ `ref`/`out`).
2. **Paketversionen** sind Floating-Versionen (siehe `DECISIONS.md`).
3. **`Microsoft.WindowsAppSDK` 1.8.x vs. neuere Version:** Falls es ein neueres stabiles Release gibt, Version in `TopBar.App.csproj` anheben.
4. **Solution-Datei** ist von Hand geschrieben; im Zweifel neu erzeugen (`dotnet new sln`, `dotnet sln add ...`).
5. **XAML-/Ressourcenschlüssel:** `Resources.resw` wird über `x:Uid` und `Loc.Get` gelesen; fehlt eine Zeichenkette, erscheint der Schlüsselname statt des Textes.
6. **Unit-Tests mit Kultur:** `FormattingTests` erwarten deutsche Datumsnamen („Di., 29. Sep.“) und `de-DE` muss auf dem Rechner verfügbar sein (ICU).

## Manuelle Abnahme

Siehe Abschnitt 14 der Spezifikation. Die Checkliste ist noch **nicht** abgehakt.
