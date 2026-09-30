# TopBar für Windows 11 – Implementierungsauftrag

> **An den Agenten:** Dieses Dokument ist die vollständige und verbindliche Spezifikation. Alle Entscheidungen sind hier bereits getroffen. Lies das komplette Dokument, bevor du Code schreibst. Arbeite die Meilensteine in Abschnitt 13 der Reihe nach ab.

> **Stand 2026-09-30:** M1–M9 sind umgesetzt. Danach kamen Erweiterungen dazu (Pomodoro-Timer, Netzwerk-Einheit und -Tabelle, Abstands- und Rahmen-Feinschliff). Sie stehen gesammelt in **Abschnitt 18** und sind in den betroffenen Abschnitten mit *[nach M9]* markiert. Wo ein Text mit *[nach M9]* dem Ursprungstext widerspricht, gilt *[nach M9]*.

---

## 0. Arbeitsregeln für den Agenten

1. **Entscheidungen:** Was hier festgelegt ist, setzt du genau so um. Abweichungen nur, wenn etwas technisch nicht geht. Dann nimmst du die einfachste funktionierende Alternative und dokumentierst sie in `DECISIONS.md` (Format: Datum, Thema, Entscheidung, Grund).
2. **Nicht Spezifiziertes:** Nimm die einfachste Lösung, die zum Stil des Dokuments passt, und dokumentiere sie ebenfalls in `DECISIONS.md`. Halte nur an, wenn du komplett blockiert bist (z. B. eine Pflicht-Bibliothek funktioniert gar nicht).
3. **Fortschritt:** Führe `PROGRESS.md` mit den Meilensteinen aus Abschnitt 13 als Checkliste. Hake erledigte Punkte ab und notiere Bekanntes/Offenes.
4. **Lauffähigkeit:** Nach jedem Meilenstein muss `dotnet build -c Debug` ohne Fehler durchlaufen und die App startbar sein. Keine neuen Compiler-Warnungen einführen.
5. **Git:** Initialisiere ein Git-Repository (`git init`, `.gitignore` für .NET/Visual Studio). Committe nach jedem Meilenstein mit einer aussagekräftigen Nachricht (`M3: Uhr und Datum`).
6. **Abhängigkeiten:** Nur die in Abschnitt 2 genannten Pakete. Jede weitere NuGet-Referenz in `DECISIONS.md` begründen.
7. **Unsichere Fakten prüfen:** Wo in diesem Dokument „**prüfen**“ steht, verifizierst du den Punkt vor der Implementierung (Doku/README der Bibliothek lesen, kleines Testprogramm).
8. **Sprache:** Code, Bezeichner, Kommentare und Commit-Messages auf Englisch. Sichtbare UI-Texte auf Deutsch.
9. **Dokumentation pflegen:** Jedes neue Feature und jede Verhaltensänderung wird im selben Arbeitsgang dokumentiert, nicht erst später:
   - `TOPBAR_SPEC.md`: Verhalten, Einstellungen (inkl. JSON-Schema und Wertebereiche in 11.2/11.3), Projektstruktur, Abnahmekriterien (14) und Tests (15). Nachträge nach M9 kommen in Abschnitt 18 bzw. werden in den betroffenen Abschnitten mit *[nach M9]* markiert.
   - `DECISIONS.md`: nur Abweichungen von der Spec und Unsicherheiten (Datum, Thema, Entscheidung, Grund), keine Feature-Beschreibungen.
   - `PROGRESS.md`: Eintrag für das Feature; war es nicht gebaut oder getestet, das ausdrücklich vermerken.
   - `README.md`: nur, wenn sich für Nutzer etwas Sichtbares ändert (Funktionen, Bauen/Starten).
   Kein Feature gilt als fertig, solange Spec und `PROGRESS.md` nicht dazu passen.

---

## 1. Ziel

Eine schlanke, native **Status-Bar am oberen Bildschirmrand von Windows 11**, vergleichbar mit Waybar/Polybar unter Linux. Optisch angelehnt an die **PowerToys Command Palette**: Fluent Design, Acrylic-Hintergrund, abgerundete Hover-Flächen, Segoe UI Variable.

Die Bar:
- reserviert oben Platz wie die Taskleiste (maximierte Fenster beginnen darunter),
- startet automatisch mit Windows,
- hat drei Bereiche: links virtuelle Desktops *(nach M9: plus Pomodoro-Timer, siehe 18.1)*, Mitte Wetter/Uhr/Datum, rechts Systemauslastung und Einstellungen.

---

## 2. Tech-Stack (verbindlich)

| Bereich | Festlegung |
|---|---|
| Runtime | **.NET 10** (LTS). Nicht .NET 8 – dessen Support endet im November 2026. |
| UI | **WinUI 3** mit **Windows App SDK**, neueste stabile Version (Version in `DECISIONS.md` festhalten) |
| Packaging | **Unpackaged, self-contained**: `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`, `SelfContained=true`, `RuntimeIdentifier=win-x64` |
| MVVM | `CommunityToolkit.Mvvm` |
| DI/Hosting | `Microsoft.Extensions.Hosting` |
| Win32-Interop | `Microsoft.Windows.CsWin32` (Source Generator, `NativeMethods.txt`). Keine handgeschriebenen P/Invokes, außer für die VirtualDesktopAccessor-DLL. |
| Logging | `Microsoft.Extensions.Logging` + `Serilog.Extensions.Hosting` + `Serilog.Sinks.File` |
| Icons | Schrift **Segoe Fluent Icons** (im System vorhanden) für UI-Icons; **Fluent UI System Icons** (Font-Datei aus `microsoft/fluentui-system-icons`, MIT-Lizenz, in `Assets/Fonts/` beilegen) für Wetter-Icons |
| Virtuelle Desktops | **VirtualDesktopAccessor.dll** (GitHub `Ciantic/VirtualDesktopAccessor`, Windows-11-Build) |
| Tests | xUnit |
| Zielsystem | Windows 11 22H2 und neuer, x64. Kein Windows 10, kein ARM64. |

Die App braucht **keine Administratorrechte** und darf keine anfordern.

---

## 3. Projektstruktur

```
topbar/
├─ TopBar.sln
├─ TOPBAR_SPEC.md
├─ DECISIONS.md
├─ PROGRESS.md
├─ README.md
├─ LICENSE                         # MIT [nach M9]
├─ THIRD_PARTY_NOTICES.md          # [nach M9]
├─ src/
│  ├─ TopBar.App/                 # WinUI-3-Projekt (EXE)
│  │  ├─ Program.cs               # eigener Main (Single Instance), DISABLE_XAML_GENERATED_MAIN
│  │  ├─ App.xaml(.cs)
│  │  ├─ Windows/
│  │  │  ├─ BarWindow.xaml(.cs)
│  │  │  └─ SettingsWindow.xaml(.cs)
│  │  ├─ Controls/
│  │  │  ├─ ThreeSectionPanel.cs
│  │  │  ├─ DesktopIndicator.xaml(.cs)
│  │  │  ├─ WeatherControl.xaml(.cs)      # [nach M9] Wetter (vorher Teil von CenterInfo)
│  │  │  ├─ ClockControl.cs               # [nach M9] Uhrzeit bzw. Datum als eigenes Element
│  │  │  ├─ StatControl.xaml(.cs)         # [nach M9] ein Systemwert (CPU/GPU/RAM/Netzwerk); ersetzt SystemStatsControl
│  │  │  ├─ SettingsButtonControl.cs      # [nach M9] Zahnrad
│  │  │  └─ PomodoroControl.xaml(.cs)     # [nach M9]
│  │  ├─ Services/                         # u. a. AppController, AppNotifier (Toasts), BarElementFactory, BarElements [nach M9]
│  │  ├─ ViewModels/
│  │  ├─ Settings/                         # Einstellungsseiten
│  │  ├─ Strings/de-DE/Resources.resw
│  │  └─ Assets/
│  │     ├─ Fonts/FluentSystemIcons-Regular.ttf
│  │     └─ Native/VirtualDesktopAccessor.dll
│  ├─ TopBar.Core/                # netX.0 ohne UI: Interfaces, Modelle, Settings, reine Logik (u. a. Pomodoro/PomodoroEngine.cs [nach M9])
│  └─ TopBar.Platform/            # netX.0-windows: AppBar, PDH, VirtualDesktop, Registry, HTTP
└─ tests/
   └─ TopBar.Core.Tests/
```

Regeln:
- `TopBar.Core` hat keine Referenz auf WinUI oder Win32.
- Alle Win32-Aufrufe liegen in `TopBar.Platform`. ViewModels rufen nie direkt Win32 auf.
- Reine Rechenlogik (Formatierung, Raten, Mapping, Layout-Mathematik) liegt in `TopBar.Core` und ist getestet.

---

## 4. Services (Interfaces in `TopBar.Core`)

```csharp
public interface ISettingsService
{
    AppSettings Current { get; }
    void Update(Action<AppSettings> change);   // speichert sofort, feuert Changed
    event EventHandler<AppSettings> Changed;
}

public interface IVirtualDesktopService
{
    bool IsAvailable { get; }                  // false, wenn DLL/Windows-Build nicht passt
    IReadOnlyList<DesktopInfo> Desktops { get; }
    int CurrentIndex { get; }
    void SwitchTo(int index);
    event EventHandler Changed;                // Anzahl, Namen oder aktueller Desktop geändert
}
public sealed record DesktopInfo(int Index, string Name);

public interface IWeatherService
{
    WeatherSnapshot? Current { get; }          // null bis zum ersten Erfolg
    Task<IReadOnlyList<GeoLocation>> SearchLocationAsync(string query, CancellationToken ct);
    Task RefreshAsync(CancellationToken ct);
    event EventHandler Changed;
}
public sealed record WeatherSnapshot(
    double Temperature, double ApparentTemperature, int WeatherCode, bool IsDay,
    double WindSpeed, double TempMax, double TempMin, int PrecipitationProbability,
    DateTimeOffset FetchedAt, bool IsStale);
public sealed record GeoLocation(string Name, string? Admin1, string Country, double Latitude, double Longitude);

public interface ISystemStatsService
{
    SystemStats Current { get; }
    event EventHandler Changed;
}
public sealed record SystemStats(
    double? CpuPercent, double? GpuPercent,
    double RamPercent, ulong RamUsedBytes, ulong RamTotalBytes,
    double DownBytesPerSec, double UpBytesPerSec);

public interface IClockService
{
    DateTime Now { get; }
    event EventHandler Tick;                   // exakt auf Sekunden- bzw. Minutenwechsel
}

public interface IAutostartService
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
    void RepairPathIfNeeded();
}
```

Allgemeine Regeln für Services:
- Hintergrundarbeit mit `PeriodicTimer` bzw. `Task`, nie auf dem UI-Thread.
- UI-Updates ausschließlich über `DispatcherQueue.TryEnqueue`.
- Jeder Service fängt seine eigenen Fehler, loggt sie und läuft weiter. **Ein fehlerhaftes Modul darf nie die Bar zum Absturz bringen.**
- Wenn ein Modul nicht funktioniert (z. B. `IVirtualDesktopService.IsAvailable == false`, GPU-Counter fehlen), wird es in der UI ausgeblendet.

---

## 5. Bar-Fenster

### 5.1 Fenster-Eigenschaften
- `AppWindow` mit `OverlappedPresenter`:
  - `SetBorderAndTitleBar(false, false)`
  - `IsResizable = false`, `IsMinimizable = false`, `IsMaximizable = false`
  - `IsAlwaysOnTop = true`
- `AppWindow.IsShownInSwitchers = false` und zusätzlich Extended Style `WS_EX_TOOLWINDOW` setzen → nicht in Taskleiste und Alt+Tab.
- **Kein** `WS_EX_NOACTIVATE` (bricht Flyouts/Kontextmenüs in WinUI 3).
- *[nach M9]* Fensterrahmen vollständig entfernen, siehe 18.4.
- Position: x = linker Rand des Monitors, y = oberer Rand, Breite = volle Monitorbreite, Höhe = `BarHeight × DPI-Skalierung` (physische Pixel).
- Das Fenster ist per-Monitor-DPI-aware (Standard bei WinUI 3). Bei `WM_DPICHANGED` Höhe neu berechnen und AppBar neu setzen.

### 5.2 AppBar-Registrierung
Implementierung in `TopBar.Platform/AppBar/AppBarController.cs`:

1. Callback-Message-ID einmalig mit `RegisterWindowMessage("TopBar_AppBarCallback")` holen.
2. Fenster-Proc per `SetWindowSubclass` (comctl32) subclassen.
3. `SHAppBarMessage(ABM_NEW, …)` mit `hWnd` und `uCallbackMessage`.
4. Position setzen:
   - `APPBARDATA.uEdge = ABE_TOP`, `rc` = Monitorbereich (`MONITORINFO.rcMonitor`), `rc.bottom = rc.top + höhePx`
   - `ABM_QUERYPOS` → danach `rc.bottom = rc.top + höhePx` erneut setzen → `ABM_SETPOS`
   - Fenster mit `SetWindowPos` exakt auf das zurückgegebene `rc` setzen.
5. Callback-Nachrichten behandeln:
   - `ABN_POSCHANGED` → Schritt 4 wiederholen.
   - `ABN_FULLSCREENAPP` (`lParam != 0`): Prüfen, ob das Vordergrundfenster den Monitor dieser Bar vollständig abdeckt. Wenn ja, diese Bar mit `AppWindow.Hide()` ausblenden (AppBar **nicht** abmelden). Bei `lParam == 0` wieder `AppWindow.Show(false)` und Schritt 4.
6. Weitere Nachrichten im Subclass:
   - `RegisterWindowMessage("TaskbarCreated")` → Explorer wurde neu gestartet: `ABM_REMOVE`, dann ab Schritt 3 neu registrieren.
   - `WM_DISPLAYCHANGE`, `WM_DPICHANGED`, `WM_SETTINGCHANGE` (mit `SPI_SETWORKAREA`) → Schritt 4.
   - `WM_ACTIVATE` → `SHAppBarMessage(ABM_ACTIVATE)`.
   - `WM_WINDOWPOSCHANGED` → `SHAppBarMessage(ABM_WINDOWPOSCHANGED)`.
7. Beim Beenden **immer** `ABM_REMOVE` aufrufen: im normalen Exit-Pfad, im `Closed`-Event des Fensters und best effort in `AppDomain.CurrentDomain.UnhandledException` sowie `Application.UnhandledException`. Sonst bleibt oben ein leerer Streifen.

### 5.3 Multi-Monitor
- Einstellung `Monitors`: `"Primary"` (Default) oder `"All"`.
- Je Monitor eine eigene `BarWindow`-Instanz mit eigener AppBar-Registrierung.
- Alle Instanzen teilen sich dieselben Services und ViewModels (Daten werden nur einmal erhoben).
- Monitore über `EnumDisplayMonitors` ermitteln. Bei `WM_DISPLAYCHANGE` die Liste neu aufbauen: Bars für entfernte Monitore schließen (inkl. `ABM_REMOVE`), für neue Monitore erzeugen.
- Die Bar wird immer auf dem oberen Rand des jeweiligen Monitors platziert, auch wenn die Windows-Taskleiste oben angeordnet ist (dann stapeln sich beide – das ist okay).

### 5.4 Optik
- Hintergrund: `DesktopAcrylicController` mit eigener `SystemBackdropConfiguration`.
  - `IsInputActive = true` **dauerhaft** setzen und nie zurücksetzen. Grund: Die Bar hat fast nie den Fokus; sonst zeigt Windows statt Acrylic eine flache Ersatzfarbe.
  - `Theme` aus der Einstellung ableiten und bei Theme-Wechsel aktualisieren.
  - `TintOpacity` aus Einstellung `BackgroundOpacity` (Default 0.6).
- Untere Kante: 1 px Linie mit Brush `CardStrokeColorDefaultBrush`.
- Schrift: `Segoe UI Variable Text`, Größe aus Einstellung `FontSize` (Default 13).
- Ziffern dürfen die Breite nicht springen lassen: Jede Zahlenanzeige bekommt eine feste `MinWidth`, die für den breitesten möglichen Wert reicht. *[nach M9]* Prozentwerte: gemessen an „88 %“ (nicht „100 %“). Netzwerk: feste Spalten, siehe 18.2.
- Interaktive Elemente: Hover-Hintergrund `SubtleFillColorSecondaryBrush`, gedrückt `SubtleFillColorTertiaryBrush`, `CornerRadius = 4`, Innenabstand 8 px horizontal.
- Bar-Innenabstand links/rechts: 12 px. Abstand zwischen Elementen in einem Bereich: 12 px. *[nach M9]* Ausnahme Systemwerte rechts: Innenabstand der Kacheln 6 px, Abstand zwischen den Kacheln 0 px (sichtbarer Abstand ≈ 12 px), siehe 18.3.
- Theme: Einstellung `Theme` = `"System"` (Default), `"Light"` oder `"Dark"` → `RequestedTheme` des Root-Elements setzen. Bei `"System"` auf Änderungen reagieren (`UISettings.ColorValuesChanged`).

---

## 6. Layout: `ThreeSectionPanel`

Eigenes Panel mit genau drei Kindern (Left, Center, Right). Ein `Grid` mit Spalten `*`/`Auto`/`*` zentriert nicht exakt, wenn links und rechts unterschiedlich viel Inhalt haben – deshalb eigenes Panel.

Layout-Logik (als reine Funktion in `TopBar.Core/Layout/ThreeSectionLayout.cs`, getestet):
- Eingabe: Gesamtbreite `W`, gewünschte Breiten `l`, `c`, `r`, Mindestabstand `gap = 16`.
- Center: `x = (W − c) / 2`, Breite `c` (wenn `c > W`, dann `x = 0`, Breite `W`).
- Left: `x = 0`, Breite `min(l, centerX − gap)`, nicht negativ.
- Right: Breite `min(r, W − (centerX + c) − gap)`, `x = W − Breite`.
- Ist Left oder Right schmaler als gewünscht, wird der Inhalt abgeschnitten (`Clip`), nicht umgebrochen.

Alle drei Bereiche sind vertikal zentriert.

*[nach M9]* Was in den drei Bereichen steht, ist frei konfigurierbar (Abschnitt 18.6). Die Abschnitte 7 bis 9 beschreiben die Standardbelegung.

---

## 7. Links: Virtuelle Desktops

### 7.1 Darstellung
- Pro virtuellem Desktop ein Element, horizontal nebeneinander, Abstand 6 px.
- **Inaktiver Desktop:** Kreis, Durchmesser 8 px (Einstellung `DesktopDotSize`), Farbe `TextFillColorSecondaryBrush`.
- **Aktiver Desktop:** Pille, Breite `DotSize × 3` (= 24 px), Höhe `DotSize`, `CornerRadius = DotSize / 2`, Farbe `AccentFillColorDefaultBrush` (Windows-Akzentfarbe).
- **Wechsel animiert:** Breite und Farbe der beteiligten Elemente gleichzeitig animieren, Dauer 180 ms, Easing `CubicEase` (EaseOut). Die anderen Elemente rutschen dadurch weich zur Seite.
- Hover: Kreis wird auf `TextFillColorPrimaryBrush` aufgehellt.
- Trefferfläche jedes Elements mindestens 16 × Bar-Höhe px (unsichtbarer Rahmen), damit man es gut anklicken kann.
- Tooltip: Name des Desktops; wenn leer, „Desktop N“ (N ab 1).

### 7.2 Interaktion
- **Linksklick** auf ein Element → `SwitchTo(index)`.
- **Mausrad** über dem Bereich → vorheriger (Rad hoch) / nächster (Rad runter) Desktop. Am Anfang/Ende nicht umbrechen. Abschaltbar über Einstellung `DesktopScrollSwitch` (Default an).
- Keine weiteren Aktionen (kein Rechtsklickmenü pro Desktop, kein Anlegen/Löschen von Desktops).

### 7.3 Technik
- Windows bietet **keine offizielle API** zum Auflisten und Wechseln virtueller Desktops. Deshalb **VirtualDesktopAccessor.dll** verwenden.
- DLL in `Assets/Native/` ablegen, bei Build/Publish ins Ausgabeverzeichnis kopieren (`CopyToOutputDirectory=PreserveNewest`).
- P/Invoke-Signaturen in `TopBar.Platform/VirtualDesktops/VdaNative.cs`. **Prüfen:** Exportierte Funktionen und Signaturen im README der verwendeten Release-Version nachlesen. Erwartet werden u. a. `GetDesktopCount`, `GetCurrentDesktopNumber`, `GoToDesktopNumber`, `GetDesktopName`, `RegisterPostMessageHook`, `UnregisterPostMessageHook`.
- Änderungs-Erkennung:
  - Desktop-Wechsel: `RegisterPostMessageHook(hwnd, messageOffset)` auf ein eigenes, unsichtbares Message-Only-Fenster (`HWND_MESSAGE`); bei Empfang `Changed` feuern.
  - Anzahl und Namen: zusätzlich alle 1000 ms pollen, weil der Hook (**prüfen**) nicht zwingend Hinzufügen/Entfernen/Umbenennen meldet. `Changed` nur feuern, wenn sich wirklich etwas geändert hat.
  - Fallback, falls der Hook fehlschlägt: aktuellen Desktop alle 250 ms pollen.
- Beim Start einen Probe-Aufruf (`GetDesktopCount()`) in `try/catch` ausführen. Bei `DllNotFoundException`, `EntryPointNotFoundException`, `SEHException` oder Rückgabewert ≤ 0: `IsAvailable = false`, Fehler loggen, Bereich ausblenden.

---

## 8. Mitte: Wetter, Uhrzeit, Datum

Aufbau (von links nach rechts, 12 px Abstand, dazwischen je ein Trennpunkt „·“ in `TextFillColorTertiaryBrush`):

```
[Wetter-Icon] 14 °C  ·  14:32  ·  Di., 29. Sep.
```

### 8.1 Uhrzeit
- Formate:
  - 24h ohne Sekunden: `HH:mm` (Default)
  - 24h mit Sekunden: `HH:mm:ss`
  - 12h ohne Sekunden: `h:mm tt`
  - 12h mit Sekunden: `h:mm:ss tt`
- Einstellungen `ClockUse24h` (Default `true`), `ClockShowSeconds` (Default `false`).
- `IClockService` feuert `Tick` exakt beim Wechsel der Sekunde (bei Sekundenanzeige) bzw. der Minute: nach jedem Tick die Zeit bis zur nächsten vollen Sekunde/Minute berechnen und genau so lange warten. Kein fester 1000-ms-Timer (driftet).
- Nach Standby/Ruhezustand korrekt: auf `SystemEvents.PowerModeChanged` bzw. Zeitsprünge reagieren, indem jeder Tick die echte Uhrzeit liest.
- Kein Klickverhalten.

### 8.2 Datum
- Einstellung `DateFormat`, Default `"ddd, d. MMM"`, formatiert mit `CultureInfo.CurrentCulture`.
- In den Einstellungen Auswahl aus Vorlagen: `ddd, d. MMM` · `dd.MM.yyyy` · `dddd, d. MMMM` · `yyyy-MM-dd` · „Benutzerdefiniert“ (Freitextfeld mit Live-Vorschau; ungültiges Format → Vorschau zeigt „Ungültiges Format“, Wert wird nicht übernommen).
- Aktualisierung bei Datumswechsel (gleicher Tick wie die Uhr).

### 8.3 Wetter
- **Quelle:** Open-Meteo, kein API-Key.
- **Ortssuche** (in den Einstellungen):
  `GET https://geocoding-api.open-meteo.com/v1/search?name={query}&count=5&language=de&format=json`
  - Anzeige der Treffer als `Name, Admin1, Country`. Auswahl speichert Name, Latitude, Longitude.
  - Suche erst ab 2 Zeichen, 400 ms Debounce, laufende Anfragen abbrechen.
- **Wetterdaten:**
  `GET https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,apparent_temperature,weather_code,is_day,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto&forecast_days=1&temperature_unit={celsius|fahrenheit}&wind_speed_unit=kmh`
  - Werte mit `CultureInfo.InvariantCulture` parsen.
- **Kein Ort konfiguriert:** Wetterbereich zeigt nur ein Icon „Ort festlegen“ (`EB11`/Globe-Glyph aus Segoe Fluent Icons); Klick öffnet die Einstellungsseite „Wetter“.
- **Aktualisierung:** beim Start, dann alle `WeatherIntervalMinutes` (Default 15, erlaubt 5–120), sofort nach Ortswechsel, sowie nach Rückkehr aus dem Standby.
- **Fehler:** Letzten erfolgreichen Wert behalten. Ist er älter als 2 × Intervall → `IsStale = true` → Temperatur mit 50 % Deckkraft anzeigen, Tooltip ergänzt „Stand: HH:mm“. Retry nach 1 min, dann Intervall.
- **Anzeige:** Icon + gerundete Temperatur (`14 °C` bzw. `57 °F`).
- **Tooltip:**
  ```
  Bewölkt
  Gefühlt 12 °C
  Min 9 °C / Max 16 °C
  Regen 40 %
  Wind 12 km/h
  ```
- **Klick:** kein Verhalten.

### 8.4 Wettercode-Mapping (WMO → Text + Icon)

Als statische Tabelle in `TopBar.Core/Weather/WeatherCodes.cs`, getestet. Icon-Namen sind Glyphen aus **Fluent UI System Icons (Regular)**; die Codepoints aus der mitgelieferten JSON-Datei des Font-Pakets übernehmen. Nachts (`is_day = 0`) bei Code 0–2 die Mond-Variante verwenden.

| Code | Text | Icon Tag | Icon Nacht |
|---|---|---|---|
| 0 | Klar | `weather_sunny` | `weather_moon` |
| 1, 2 | Leicht bewölkt | `weather_partly_cloudy_day` | `weather_partly_cloudy_night` |
| 3 | Bewölkt | `weather_cloudy` | `weather_cloudy` |
| 45, 48 | Nebel | `weather_fog` | `weather_fog` |
| 51, 53, 55, 56, 57 | Nieselregen | `weather_drizzle` | `weather_drizzle` |
| 61, 63, 65, 66, 67, 80, 81, 82 | Regen | `weather_rain` | `weather_rain` |
| 71, 73, 75, 77, 85, 86 | Schnee | `weather_snow` | `weather_snow` |
| 95, 96, 99 | Gewitter | `weather_thunderstorm` | `weather_thunderstorm` |
| sonst | Unbekannt | `weather_cloudy` | `weather_cloudy` |

**Prüfen:** Existieren die Icon-Namen im Font-Paket? Wenn einer fehlt, den nächstpassenden wählen und in `DECISIONS.md` notieren.

---

## 9. Rechts: Systemauslastung und Einstellungen

Reihenfolge von links nach rechts: **CPU · GPU · RAM · Netzwerk · ⚙**

Jeder Wert: kleines Icon (Segoe Fluent Icons) + Zahl. Beschriftung „CPU“, „GPU“, „RAM“ nur als Text-Label, wenn Einstellung `StatsShowLabels` = `true` (Default `true`).

### 9.1 Datenquellen

| Wert | Quelle | Anzeige | Tooltip |
|---|---|---|---|
| CPU | PDH `\Processor Information(_Total)\% Processor Utility`; Wert auf 0–100 begrenzen (kann durch Turbo > 100 sein) | `12 %` | `CPU-Auslastung: 12 %` |
| GPU | PDH `\GPU Engine(*)\Utilization Percentage` (siehe 9.2) | `34 %` | `GPU-Auslastung: 34 %` |
| RAM | `GlobalMemoryStatusEx` → `dwMemoryLoad` bzw. aus `ullTotalPhys`/`ullAvailPhys` | `45 %` | `Arbeitsspeicher: 14,2 / 31,9 GB` |
| Netzwerk | `NetworkInterface.GetAllNetworkInterfaces()` → `GetIPStatistics().BytesReceived/BytesSent`, Delta pro Sekunde | `↓ 1,2 MB/s  ↑ 80 KB/s` *(nach M9: Tabelle, Einheit wählbar, 18.2)* | Name des/der Interfaces |

- PDH über CsWin32: `PdhOpenQuery`, `PdhAddEnglishCounter` (englische Counter-Namen, unabhängig von der Windows-Sprache!), `PdhCollectQueryData`, `PdhGetFormattedCounterValue` / `PdhGetFormattedCounterArray`. Die erste Messung liefert keinen gültigen Wert → erst ab dem zweiten Sample anzeigen.
- Aktualisierung alle `StatsIntervalMs` (Default 1000, erlaubt 500–5000).

### 9.2 GPU-Berechnung
- Counter-Instanzen heißen z. B. `pid_1234_luid_0x00000000_0x0000C3A1_phys_0_eng_3_engtype_3D`.
- Pro Sample: Werte pro `(luid, engtype)` über alle Prozesse **summieren**, dann pro `luid` das **Maximum über alle engtypes** nehmen (entspricht dem Task-Manager).
- Angezeigt wird die GPU mit der höchsten Auslastung (bei mehreren GPUs).
- Neue Prozesse erzeugen neue Instanzen → die Wildcard-Counter-Liste alle 10 s neu aufbauen (Counter entfernen und neu hinzufügen).
- Liefert PDH keine GPU-Counter: GPU-Anzeige ausblenden.

### 9.3 Netzwerk
- Berücksichtigt werden Interfaces mit `OperationalStatus.Up`, außer `Loopback` und `Tunnel`, sowie Interfaces, deren Beschreibung „Hyper-V“, „VirtualBox“, „VMware“, „WSL“, „vEthernet“ oder „TAP“ enthält.
- Einstellung `NetworkInterfaceId`: `null` = automatisch (Summe aller berücksichtigten), sonst genau das Interface mit dieser `Id`.
- Negatives Delta (z. B. nach Adapter-Neustart) als 0 behandeln.
- Formatierung: *[nach M9]* siehe 18.2 (Einheit Bytes/Bits, Skalierung, dreistellige Zahl). Die ursprüngliche Regel (nur Bytes, Basis 1024) bleibt als Default „Bytes“ + „Automatisch“ erhalten, mit einer Änderung: Die Einheit wechselt bei 1000 statt 1024 der aktuellen Einheit, damit die Zahl nie vierstellig wird.

### 9.4 Warnfarben
- Gilt für CPU, GPU und RAM.
- Wert ≥ `WarnThreshold` (Default 85) → Textfarbe `SystemFillColorCautionBrush`.
- Wert ≥ `CriticalThreshold` (Default 95) → `SystemFillColorCriticalBrush`.

### 9.5 Interaktion
- Klick auf CPU, GPU, RAM oder Netzwerk → `taskmgr.exe` starten (`Process.Start` mit `UseShellExecute = true`).
- **Zahnrad** (Segoe Fluent Icons `E713`) ganz rechts → Einstellungsfenster öffnen.

### 9.6 Sichtbarkeit
Einstellungen `ShowCpu`, `ShowGpu`, `ShowRam`, `ShowNetwork` (alle Default `true`). Das Zahnrad ist immer sichtbar.

---

## 10. Kontextmenü

Rechtsklick irgendwo auf der Bar (auch auf Elementen) öffnet ein `MenuFlyout` an der Mausposition:

| Eintrag | Icon | Aktion |
|---|---|---|
| Einstellungen | `E713` | Einstellungsfenster öffnen |
| Neu laden | `E72C` | Alle Bar-Fenster schließen (inkl. `ABM_REMOVE`), Services neu starten, Bars neu erzeugen |
| — | | Trenner |
| Beenden | `E7E8` | App sauber beenden (inkl. `ABM_REMOVE` für alle Bars) |

---

## 11. Einstellungen

### 11.1 Fenster
- Normales WinUI-3-Fenster, 900 × 640 (DIP), zentriert auf dem Monitor, auf dem der Mauszeiger ist. Titel: „TopBar – Einstellungen“. Hintergrund `MicaBackdrop`.
- `NavigationView` links (Stil wie PowerToys-Einstellungen), Seiten siehe 11.3.
- Nur eine Instanz: Ist das Fenster offen, wird es beim erneuten Öffnen nur aktiviert und in den Vordergrund geholt.
- Alle Änderungen gelten **sofort** (kein „Übernehmen“-Button). Schieberegler und Textfelder speichern mit 300 ms Debounce.

### 11.2 Speicherung
- Datei: `%LOCALAPPDATA%\TopBar\settings.json`, UTF-8, eingerückt, `System.Text.Json` mit Source Generator.
- Fehlt die Datei → Defaults verwenden und Datei anlegen.
- Ist die Datei defekt → nach `settings.broken-{yyyyMMdd-HHmmss}.json` umbenennen, Defaults laden, Warnung loggen.
- Unbekannte Felder ignorieren, fehlende Felder mit Default füllen.
- Schreiben atomar: in `settings.json.tmp` schreiben, dann `File.Replace`/`File.Move(overwrite: true)`.
- `SchemaVersion` für spätere Migrationen; aktuell `1`.

Vollständige Defaults:

```json
{
  "SchemaVersion": 1,
  "General": {
    "Autostart": false,
    "Theme": "System",
    "Monitors": "Primary",
    "BarHeight": 32,
    "FontSize": 13,
    "BackgroundOpacity": 0.6
  },
  "Desktops": {
    "Show": true,
    "DotSize": 8,
    "ScrollSwitch": true
  },
  "Clock": {
    "Use24h": true,
    "ShowSeconds": false,
    "DateFormat": "ddd, d. MMM"
  },
  "Weather": {
    "Show": true,
    "LocationName": null,
    "Latitude": null,
    "Longitude": null,
    "Unit": "Celsius",
    "IntervalMinutes": 15
  },
  "Stats": {
    "ShowCpu": true,
    "ShowGpu": true,
    "ShowRam": true,
    "ShowNetwork": true,
    "ShowLabels": true,
    "IntervalMs": 1000,
    "WarnThreshold": 85,
    "CriticalThreshold": 95,
    "NetworkInterfaceId": null,
    "NetworkUnit": "Bytes",
    "NetworkScale": "Auto"
  },
  "Pomodoro": {
    "WorkMinutes": 25,
    "ShortBreakMinutes": 5,
    "Sessions": 4,
    "LongBreakMinutes": 15
  },
  "Layout": {
    "Left": ["Desktops", "Pomodoro"],
    "Center": ["Weather", "Clock", "Date"],
    "Right": ["Cpu", "Gpu", "Ram", "Network", "Settings"]
  }
}
```

Werte beim Laden auf gültige Bereiche begrenzen: `BarHeight` 24–56, `FontSize` 11–18, `BackgroundOpacity` 0.0–1.0, `DotSize` 6–14, `IntervalMinutes` 5–120, `IntervalMs` 500–5000, Schwellwerte 50–100 (Critical ≥ Warn), `NetworkUnit` ∈ {`Bytes`, `Bits`}, `NetworkScale` ∈ {`Auto`, `Kilo`, `Mega`} (ungültig → erster Wert), Pomodoro: `WorkMinutes`/`ShortBreakMinutes`/`LongBreakMinutes` 1–99, `Sessions` 1–12 *[nach M9]*. `Layout` *[nach M9]*: unbekannte Element-IDs und doppelte Einträge werden entfernt (erstes Vorkommen gewinnt, Reihenfolge Left, Center, Right), Groß-/Kleinschreibung wird korrigiert; ein Element, das in keiner Liste steht, wird nicht angezeigt. Fehlt `Layout` in der Datei, gilt die Standardbelegung.

### 11.3 Seiten

**Allgemein**
- Mit Windows starten (ToggleSwitch)
- Design: System / Hell / Dunkel (RadioButtons)
- Anzeigen auf: Nur Hauptmonitor / Allen Monitoren (RadioButtons)
- Höhe der Leiste (Slider 24–56, Schritt 2, Wert daneben anzeigen)
- Schriftgröße (Slider 11–18)
- Deckkraft des Hintergrunds (Slider 0–100 %)

**Anordnung** *[nach M9]* (zweite Seite, nach „Allgemein“)
- Vier Listen: Links, Mitte, Rechts nebeneinander, darunter (volle Breite, unter den Knöpfen) Nicht angezeigt. Elemente per Drag-and-Drop zwischen und innerhalb der Listen verschieben.
- Knöpfe für das ausgewählte Element: Hoch, Runter, Linke Zone, Mittlere Zone, Rechte Zone, Ausblenden; „Anordnung zurücksetzen“.
- Wirkt sofort auf alle Leisten.

**Virtuelle Desktops**
- Anzeigen (ToggleSwitch)
- Mit Mausrad wechseln (ToggleSwitch)
- Punktgröße (Slider 6–14)
- Hinweis-InfoBar, wenn `IsAvailable == false`: „Virtuelle Desktops werden auf dieser Windows-Version nicht unterstützt.“

**Uhr & Datum**
- 24-Stunden-Format (ToggleSwitch)
- Sekunden anzeigen (ToggleSwitch)
- Datumsformat (ComboBox mit Vorlagen + „Benutzerdefiniert“ → TextBox), Live-Vorschau

**Wetter**
- Anzeigen (ToggleSwitch)
- Ort (AutoSuggestBox mit Geocoding-Suche), darunter aktuell gespeicherter Ort
- Einheit: °C / °F (RadioButtons)
- Aktualisierungsintervall in Minuten (NumberBox 5–120)

**Pomodoro** *[nach M9]*
- Arbeitsphase in Minuten (NumberBox 1–99)
- Kurze Pause in Minuten (NumberBox 1–99)
- Arbeitsphasen bis zur langen Pause (NumberBox 1–12)
- Lange Pause in Minuten (NumberBox 1–99)
- Hinweistext zur Bedienung

**System**
- CPU / GPU / RAM / Netzwerk anzeigen (je ToggleSwitch)
- Beschriftungen anzeigen (ToggleSwitch)
- Aktualisierungsintervall (ComboBox: 0,5 s / 1 s / 2 s / 5 s)
- Warnschwelle, kritische Schwelle (Slider 50–100)
- Netzwerk: Einheit (ComboBox: Bytes pro Sekunde KB/s, MB/s / Bits pro Sekunde kbit/s, Mbit/s) *[nach M9]*
- Netzwerk: Skalierung (ComboBox: Automatisch / Immer kilo / Immer mega) *[nach M9]*
- Netzwerkadapter (ComboBox: „Automatisch“ + Liste der berücksichtigten Interfaces)

**Info**
- App-Name, Version (aus Assembly)
- Button „Log-Ordner öffnen“ (öffnet `%LOCALAPPDATA%\TopBar\logs` im Explorer)
- Button „Einstellungen zurücksetzen“ mit `ContentDialog`-Bestätigung

---

## 12. Start, Autostart, Single Instance, Logging

### 12.1 Single Instance
- Eigener `Program.Main` (Compile-Konstante `DISABLE_XAML_GENERATED_MAIN`).
- `AppInstance.FindOrRegisterForKey("TopBar.SingleInstance")`:
  - Ist die aktuelle Instanz nicht die registrierte → `RedirectActivationToAsync` an die vorhandene Instanz und sofort beenden.
  - Die vorhandene Instanz reagiert auf `Activated` mit dem Öffnen des Einstellungsfensters – **außer** der Start kam mit dem Argument `--autostart` (dann nichts tun).

### 12.2 Autostart
- Registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, Wertname `TopBar`, Wert `"<voller Pfad zur EXE>" --autostart`.
- `SetEnabled(true)` schreibt den Wert, `SetEnabled(false)` löscht ihn.
- `RepairPathIfNeeded()` beim Start: Wenn Autostart aktiv ist und der gespeicherte Pfad nicht zur aktuellen EXE passt → Wert neu schreiben.
- Die Einstellung `General.Autostart` und der Registry-Zustand werden beim Start abgeglichen (Einstellung ist führend).
- Autostart ist standardmäßig **aus** (Opt-in über die Einstellung „Mit Windows starten“). Beim allerersten Start wird nichts in die Registry geschrieben. *[nach M9]*

### 12.3 Startablauf
1. Single-Instance-Prüfung
2. Logging initialisieren
3. Settings laden
4. Autostart abgleichen
5. Services starten
6. Bars für die konfigurierten Monitore erzeugen
7. Beim Start mit `--autostart`: nichts weiter. Beim manuellen ersten Start (keine Settings vorhanden): Einstellungsfenster öffnen.

### 12.4 Logging
- `%LOCALAPPDATA%\TopBar\logs\topbar-.log`, täglich rollierend, 7 Dateien behalten.
- Level: Information (Debug-Build: Debug).
- Jede gefangene Exception mit Modulname loggen. Keine Logs pro Tick/Sample (nur Fehler und Zustandswechsel).

### 12.5 Ressourcen
- Leerlauf: **< 1 % CPU**, **< 100 MB RAM** (Arbeitssatz).
- Keine Animationen oder Timer laufen lassen, wenn alle Bars ausgeblendet sind (Vollbild) – außer Datenerhebung für Wetter.

---

## 13. Meilensteine

Reihenfolge einhalten. Nach jedem Meilenstein: bauen, starten, manuell prüfen, `PROGRESS.md` aktualisieren, committen.

| # | Inhalt | Fertig, wenn … |
|---|---|---|
| M1 | Solution, Projekte, Pakete, DI, Logging, `ISettingsService` mit JSON, Single Instance, `DECISIONS.md`, `PROGRESS.md`, `README.md` | App startet, legt settings.json und Log an, zweiter Start beendet sich sofort |
| M2 | `BarWindow`: rahmenlos, Acrylic, nicht in Alt+Tab, AppBar am Hauptmonitor, Kontextmenü mit „Beenden“ | Maximierte Fenster beginnen unter der Bar; nach „Beenden“ ist der Platz sofort frei |
| M3 | `ThreeSectionPanel` + Tests, Uhr & Datum | Uhr tickt exakt auf den Minutenwechsel, Mitte ist exakt zentriert |
| M4 | CPU, RAM, Netzwerk, dann GPU; Warnfarben; Zahnrad; Klick → Task-Manager | Werte plausibel vs. Task-Manager (±5 %-Punkte) |
| M5 | Einstellungsfenster mit allen Seiten, Live-Anwendung, Autostart | Jede Einstellung wirkt sofort; Autostart-Eintrag erscheint/verschwindet in der Registry |
| M6 | Wetter inkl. Ortssuche, Icons, Tooltip, Fehlerverhalten | Wetter wird angezeigt; ohne Netz kein Absturz, Anzeige als veraltet |
| M7 | Virtuelle Desktops: Kreise/Pille, Animation, Klick, Mausrad, Events, Fallback | Wechsel per Win+Strg+←/→ wird in < 300 ms angezeigt; neue/gelöschte Desktops erscheinen live |
| M8 | Multi-Monitor, Vollbild-Erkennung, Explorer-Neustart, DPI-Wechsel, Kontextmenü „Neu laden“ | Alle Punkte aus Abschnitt 14 erfüllt |
| M9 | Feinschliff Optik, README mit Build- und Publish-Anleitung | Abschnitt 14 komplett abgehakt |
| M10 *(nach M9)* | Pomodoro-Timer inkl. Einstellungsseite und Toast (18.1) | Kriterien „Pomodoro“ in Abschnitt 14 |
| M11 *(nach M9)* | Fensterrahmen (18.4) | Kriterium „Optik“ in Abschnitt 14 |
| M12 *(nach M9)* | Netzwerk-Einheit/Skalierung/Tabelle, Abstände (18.2–18.3) | Kriterien „Netzwerk“ und „Optik“ in Abschnitt 14 |

---

## 14. Abnahmekriterien (manueller Test)

- [ ] Nach Windows-Anmeldung erscheint die Bar automatisch oben auf dem Hauptmonitor.
- [ ] Maximierte Fenster überdecken die Bar nicht und beginnen darunter.
- [ ] Die Bar erscheint nicht in Taskleiste und Alt+Tab.
- [ ] Nach „Beenden“ wird der reservierte Platz sofort freigegeben.
- [ ] `taskkill /f /im explorer.exe` und danach `explorer.exe` starten → Bar sitzt weiterhin korrekt und reserviert Platz.
- [ ] Ein Vollbild-Video (Browser, F11) oder Vollbild-Spiel wird nicht von der Bar überlagert; danach erscheint die Bar wieder.
- [ ] Mittlerer Bereich ist pixelgenau zentriert, auch wenn links 1 und rechts alle Werte angezeigt werden.
- [ ] Virtuelle Desktops: ein Kreis pro Desktop, der aktive als Pille in Akzentfarbe; animierter Wechsel; Klick wechselt; Mausrad wechselt; Hinzufügen/Entfernen/Umbenennen wird live übernommen.
- [ ] Uhr springt genau zum Minutenwechsel (bzw. Sekundenwechsel) um.
- [ ] CPU und RAM weichen höchstens ±5 Prozentpunkte vom Task-Manager ab; GPU zeigt unter Last (z. B. Video/Spiel) plausible Werte.
- [ ] Netzwerk zeigt beim Download plausible Werte.
- [ ] Wetter wird angezeigt; ohne Netzwerk bleibt der letzte Wert und wird als veraltet dargestellt.
- [ ] Rechtsklick → Einstellungen öffnet das Fenster; Rechtsklick → Beenden beendet sauber.
- [ ] Änderungen in den Einstellungen (Höhe, Theme, Monitore, sichtbare Werte) wirken sofort ohne Neustart.
- [ ] Autostart lässt sich an- und ausschalten und wirkt nach Neuanmeldung.
- [ ] Wechsel der Skalierung (100 % ↔ 150 %) und An-/Abstecken eines Monitors funktionieren ohne Neustart.
- [ ] Defekte settings.json führt nicht zum Absturz (Backup wird angelegt).
- [ ] Funktioniert ohne Administratorrechte.
- [ ] CPU-Last der App im Leerlauf < 1 %.
- [ ] *[nach M9]* Pomodoro: Klick startet/pausiert; Zeit läuft sekundengenau; Weiter-Pfeil und Stopp-Symbol erscheinen nur, solange ein Zyklus läuft; Stopp setzt auf Arbeitsphase 1/N zurück.
- [ ] *[nach M9]* Pomodoro: Läuft eine Phase ab, erscheint ein Windows-Toast (oder der Systemton), die nächste Phase wartet pausiert auf einen Klick; nach der langen Pause steht der Timer wieder am Anfang.
- [ ] *[nach M9]* Pomodoro: Zeiten in den Einstellungen ändern wirkt sofort; eine laufende Phase wird dadurch nie verlängert.
- [ ] *[nach M9]* Netzwerk: Einheit (Bytes/Bits) und Skalierung (Automatisch/kilo/mega) wirken sofort; beim Wechsel KB/s ↔ MB/s (bzw. kbit/s ↔ Mbit/s) verschiebt sich nichts.
- [ ] *[nach M9]* Anordnung: Jedes Element (Desktops, Pomodoro, Wetter, Uhrzeit, Datum, CPU, GPU, RAM, Netzwerk, Zahnrad) lässt sich per Maus und per Knopf in einen anderen Bereich und an eine andere Position schieben bzw. ausblenden; die Leiste folgt sofort; nach „Neu laden“/Neustart bleibt die Anordnung erhalten; Mitte bleibt zentriert, auch wenn ein Bereich leer ist.
- [ ] *[nach M9]* Optik: Kein Rahmen, keine Rundung, kein Lichtsaum um die Leiste; Abstand Icon–Label–Wert bei CPU/GPU/RAM gleichmäßig 6 px, kein toter Hover-Bereich zwischen den Werten.

## 15. Unit-Tests (Pflicht, in `TopBar.Core.Tests`)

- `ThreeSectionLayout`: symmetrisch, asymmetrisch, Überbreite links/rechts, Center breiter als W.
- `WeatherCodes`: jeder Code aus Tabelle 8.4, Tag/Nacht, unbekannter Code.
- Netzwerk-Rate: Delta-Berechnung, negatives Delta, Formatierung aller Stufen aus 9.3.
- GPU-Aggregation: Summe je (luid, engtype), Maximum je luid, mehrere GPUs.
- Settings: Defaults, fehlende Felder, ungültige Werte werden begrenzt, defekte Datei.
- Zeitformat/Datumsformat inkl. ungültigem benutzerdefiniertem Format.
- *[nach M9]* Netzwerk: Bytes und Bits, Skalierung Auto/Kilo/Mega, Zahlen- und Einheiten-Teile, Normalisierung von `NetworkUnit`/`NetworkScale`.
- *[nach M9]* `LayoutSettings`: Standardbelegung, Verschieben zwischen Bereichen, Index-Begrenzung, Entfernen/Wiederherstellen, Normalisierung (unbekannt, doppelt, Schreibweise, null).
- *[nach M9]* `PomodoroEngine`: kompletter Zyklus, Start/Pause/Skip/Reset, Phasenende ohne Autostart, Einstellungsänderung während des Laufs, Formatierung „mm:ss“.

---

## 16. Build & Publish

In `README.md` dokumentieren und selbst prüfen:

```powershell
dotnet build -c Debug
dotnet test
dotnet publish src/TopBar.App -c Release -r win-x64 --self-contained true -o publish
```

- Ergebnis: Ordner `publish/` mit `TopBar.exe`, lauffähig ohne installierte .NET-Runtime oder Windows-App-SDK-Runtime.
- Kein Installer, kein MSIX.

---

## 17. Nicht im Umfang

Folgendes **nicht** umsetzen, auch nicht teilweise:
- Windows 10, ARM64
- Tray-Icons / Infobereich
- Medien-Steuerung, Lautstärke, Akku, Bluetooth
- Kalender-Flyout oder Klickaktionen auf Uhr/Datum/Wetter (außer „Ort festlegen“)
- Sparklines/Verlaufsgraphen
- Anlegen/Löschen/Umbenennen virtueller Desktops aus der Bar
- Installer, Auto-Update
- Pomodoro: Statistik/Verlauf, Speichern des Timer-Zustands über einen Neustart, frei wählbare Töne *[nach M9]*
- Weitere Sprachen als Deutsch (UI-Texte trotzdem in `Resources.resw`, damit später erweiterbar)

Die Architektur soll neue Module später einfach ermöglichen: Jedes Modul = Service + ViewModel + Control, eingehängt in einen der drei Bereiche.

---

## 18. Nachträge nach M9

Diese Abschnitte beschreiben, was nach den Meilensteinen M1–M9 dazukam. Das Verhalten ist aus dem Code abgeleitet und gilt als verbindlich. Entscheidungen und Unsicherheiten dazu stehen in `DECISIONS.md`.

### 18.1 Pomodoro-Timer (links, rechts neben den Desktop-Punkten)

**Darstellung:** `[Symbol] mm:ss  Info  [Weiter] [Stopp]`
- Symbol: 🍅 (Segoe UI Emoji, einfarbig, nimmt die Textfarbe) in der Arbeitsphase, ☕ in Pausen. Deckkraft 100 % wenn der Timer läuft, sonst 45 %.
- Zeit `mm:ss`, Sekunden aufgerundet (nie `00:00`, solange noch Zeit übrig ist). Feste Breite (gemessen an „88:88“). Pausiert: Deckkraft 60 %.
- Info (Sekundärfarbe): in der Arbeitsphase `N/M` (aktuelle Arbeitsphase von M), in Pausen „Pause“ bzw. „Lange Pause“.
- Weiter-Pfeil (`E893`) und Stopp-Symbol (`E71A`) sind nur sichtbar, solange der Zyklus nicht im Zustand „Leerlauf“ ist.
- Tooltip Hauptknopf: `<Phase> N/M · Klicken zum Starten | Pausieren | Fortsetzen`. Tooltips der Nebenknöpfe: „Phase beenden und zur nächsten wechseln“, „Zurücksetzen“.

**Verhalten** (`TopBar.Core/Pomodoro/PomodoroEngine.cs`, reine State-Machine ohne Timer/UI, Zeit wird von außen übergeben):
- Zyklus: Arbeit 1 → kurze Pause → … → Arbeit N → lange Pause → zurück in den Leerlauf (Arbeitsphase 1, gestoppt).
- Zustände: `Idle`, `Running`, `Paused`.
- Klick auf den Hauptknopf: Start bzw. Pause bzw. Fortsetzen.
- Läuft eine Phase ab, wird die nächste vorbereitet, aber **nicht gestartet**: Zustand `Paused` mit voller Dauer. Der Nutzer startet sie per Klick. Nach der langen Pause springt der Timer in den Leerlauf.
- Weiter-Pfeil (Skip): beendet die aktuelle Phase sofort und startet die nächste (auch aus der Pause). Im Leerlauf ohne Wirkung.
- Stopp: zurück auf Arbeitsphase 1, gestoppt.
- Einstellungen während des Laufs: Die laufende Phase behält ihre Restzeit, wird aber höchstens auf die neue Dauer gekürzt, nie verlängert. Im Leerlauf gilt die neue Dauer sofort. Sinkt die Zahl der Arbeitsphasen unter die aktuelle, wird die aktuelle Nummer begrenzt.
- Der Zustand wird nicht gespeichert: Ein App-Neustart setzt den Timer zurück, „Neu laden“ (Kontextmenü) nicht.

**Technik:**
- `PomodoroViewModel` treibt die Engine mit einem 250-ms-`DispatcherQueueTimer`, der nur läuft, solange der Timer läuft.
- Phasenende → Windows-Toast über das Windows App SDK (`AppNotificationManager`, unpackaged, Registrierung beim Start, Abmeldung beim Beenden; `AppNotifier`). Schlägt der Toast fehl: Systemton per `MessageBeep` (`SystemSound.Notify`). Der Toast braucht keine Aktion beim Klick.
- `ThreeSectionPanel` nimmt genau drei Kinder; der linke Bereich ist deshalb ein horizontales `StackPanel` (Spacing 12) aus `DesktopIndicator` und `PomodoroControl`.
- Einstellungsseite „Pomodoro“ (Icon `E916`, nicht verifiziert) zwischen „Wetter“ und „System“, Werte siehe 11.2/11.3.

### 18.2 Netzwerkanzeige

**Einstellungen** `Stats.NetworkUnit` (`Bytes` | `Bits`, Default `Bytes`) und `Stats.NetworkScale` (`Auto` | `Kilo` | `Mega`, Default `Auto`).

**Formatierung** (`TopBar.Core/Stats/NetworkRate.cs`, Werte werden abgeschnitten, nie gerundet, deutsche Dezimaltrennung):
- Bytes: Basis 1024, Einheiten `KB/s`, `MB/s`, `GB/s`. Bits: Wert × 8, Basis 1000, Einheiten `kbit/s`, `Mbit/s`, `Gbit/s`.
- `Auto`: Die Einheit wechselt, sobald der Wert in der aktuellen Einheit ≥ 1000 ist. `Kilo`/`Mega`: immer kilo bzw. mega.
- Zahl: kilo immer ganzzahlig; ab mega unter 10 eine Nachkommastelle (`9,9`), sonst ganzzahlig. Der reine Text (`NetworkRate.Format`) füllt auf 3 Zeichen mit Ziffernbreiten-Leerzeichen (U+2007) auf, z. B. `  5 KB/s`.
- Bei fester Skalierung kann die Zahl mehr als drei Stellen haben (z. B. `10000 kbit/s`).

**Darstellung als Tabelle** (`StatControl`, Art Netzwerk):
- Feste Spalten: `↓ Zahl Einheit  ↑ Zahl Einheit`, Pfeile in der Sekundärfarbe.
- Zahlenspalte rechtsbündig, Breite gemessen an „0000“ (vierstellig möglich). Einheitenspalte linksbündig, Breite der breitesten möglichen Einheit der gewählten Einstellung (`NetworkRate.UnitSamples`).
- Spaltenabstand 4 px, zwischen den beiden Hälften 8 px.

### 18.3 Systemwerte: Abstände

- Werte (CPU/GPU/RAM) linksbündig, damit Icon, Label und Wert gleichmäßig 6 px Abstand haben. Der Leerraum der festen Breite liegt rechts vom Wert.
- Kachel-Innenabstand 6 px (statt 8), Abstand zwischen den Kacheln 0 (statt 12). Ergebnis: sichtbarer Abstand zwischen zwei Werten ≈ 12 px plus der Restplatz der festen Breite.

### 18.4 Fensterrahmen

Das WinUI-Fenster trägt standardmäßig Rahmen-, Titel- und 3D-Kanten-Stile, die einen hellen Saum um die Leiste zeichnen. Deshalb:
- Fensterstil auf `WS_POPUP | WS_VISIBLE` setzen (`Win32Window.StripFrame`).
- Erweiterte Stile `WS_EX_WINDOWEDGE`, `WS_EX_CLIENTEDGE`, `WS_EX_DLGMODALFRAME`, `WS_EX_STATICEDGE` entfernen (zusammen mit `WS_EX_TOOLWINDOW`).
- DWM: Rahmenfarbe `DWMWA_COLOR_NONE` und `DWMWCP_DONOTROUND` (eckig, bündig mit dem Bildschirmrand).
- `WM_NCCALCSIZE` und `WM_NCPAINT` mit 0 beantworten: Es gibt keinen Nicht-Client-Bereich.
- Die Werte werden einmalig beim Start geloggt.

### 18.5 Sonstiges

- Lizenz MIT (`LICENSE`), mitgelieferte Fremdkomponenten in `THIRD_PARTY_NOTICES.md` (VirtualDesktopAccessor.dll, Fluent System Icons).
- Paketversionen sind gepinnt (siehe `DECISIONS.md`); Ziel-Framework der App `net10.0-windows10.0.22621.0`, Plattform x64.
- Datumsformat-Test: Die abgekürzte Vorlage `ddd, d. MMM` wird gegen die Kultur selbst geprüft, weil „Di.“/„Sep.“ von den ICU-Daten des Rechners abhängen.

### 18.6 Anordnung der Elemente

**Elemente** (IDs in `TopBar.Core/Settings/LayoutSettings.cs`, `BarElementIds`): `Desktops`, `Pomodoro`, `Weather`, `Clock`, `Date`, `Cpu`, `Gpu`, `Ram`, `Network`, `Settings` (Zahnrad). Uhrzeit und Datum sind getrennte Elemente; CPU, GPU, RAM und Netzwerk sind einzelne Kacheln.

**Einstellung** `Layout.Left` / `Layout.Center` / `Layout.Right`: geordnete Listen von Element-IDs (Standardbelegung siehe 11.2, entspricht dem Aussehen vor der Änderung bis auf die Punkte unten). Jedes Element steht höchstens einmal auf der Leiste. Was in keiner Liste steht, wird nicht angezeigt.

**Bedienung:** Einstellungsseite „Anordnung“ (11.3): die drei Bereichslisten nebeneinander, darunter die Knöpfe und die volle Breite „Nicht angezeigt“. Jede Änderung wird sofort gespeichert; `BarWindow` baut die drei Bereiche neu auf, sobald sich die Listen geändert haben (Vergleich über einen Schlüssel aus den drei Listen). Die Leiste selbst ist nicht per Maus umsortierbar: Klick, Mausrad und Rechtsklick der Elemente bleiben unverändert.

**Zusammenspiel mit den Anzeigen-Schaltern:** „Anzeigen“-Schalter (Wetter, Desktops, CPU, GPU, RAM, Netzwerk) bleiben auf ihren Seiten erhalten. Ein Element erscheint nur, wenn es in einem Bereich steht **und** sein Schalter an ist. Wird ein Element über die Anordnungsseite in einen Bereich gelegt, schaltet das seinen Schalter ein; Ausblenden über die Anordnung lässt den Schalter unverändert. Elemente mit ausgeschaltetem Schalter zeigt die Seite mit dem Zusatz „(aus)“.

**Optische Änderungen gegenüber vorher:** Die „·“-Trenner in der Mitte entfallen; Abstände entstehen nur noch durch die Elementränder (Text-Elemente 6 px links/rechts, Kacheln und Zahnrad über ihren Innenabstand). Der Außenrand der Leiste ist von 12 auf 6 px verkleinert, weil Text-Elemente jetzt selbst 6 px Rand haben.

**Technik:** `BarElementFactory` erzeugt pro Element-ID ein Control (eine Instanz je Leistenfenster). `ThreeSectionPanel` bekommt weiterhin genau drei Kinder, jeweils ein horizontales `StackPanel` pro Bereich (auch wenn leer).
