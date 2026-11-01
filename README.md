# TopBar

A slim status bar at the top edge of Windows 11, similar to Waybar. Built with WinUI 3 and .NET 10.

- **Left:** virtual desktops as dots, the active one as an animated pill (click to switch desktop);
  next to it a Pomodoro timer (click to start/pause, the stop icon resets it; durations are configurable)
- **Center:** weather, clock, date
- **Right:** CPU, GPU, RAM, network (bytes or bits per second, auto or fixed scaling) and a gear icon for settings

This is the default layout: under Settings → Anordnung (Layout) every element (desktops, Pomodoro, weather, time, date, CPU, GPU, RAM, network, gear) can be moved to a different section, reordered, or hidden, via drag and drop or a button.

The bar registers itself as an AppBar, so other windows do not overlap it. It hides itself for fullscreen apps, supports multiple monitors, and uses an Acrylic background. The UI is in German (UI labels are quoted in German below, with a translation in parentheses).

> **Status:** Builds, all 90 unit tests pass, the app starts and shows the bar. Manual acceptance testing (spec section 14) is not fully checked off yet.

## Note: vibe-coded

All code in this project was created by vibe coding with an AI assistant (Claude: Opus 5.5 and Sonnet 5.5), based on a specification I wrote (`TOPBAR_SPEC.md`). I have tested and tried it out, but I have not reviewed it line by line. Expect rough edges, and use it at your own risk.

## Requirements

- Windows 11, x64. For virtual desktops you need **24H2 (build ≥ 26100.2605)** because of `VirtualDesktopAccessor.dll`. On older builds the left section stays hidden.
- .NET 10 SDK
- No administrator rights needed, neither for building nor for running

## Build, test, run

```powershell
dotnet build -c Debug
dotnet test
dotnet run --project src/TopBar.App
```

Publish as a standalone EXE (no installed .NET or Windows App SDK runtime required):

```powershell
dotnet publish src/TopBar.App -c Release -r win-x64 --self-contained true -o publish
```

`publish/TopBar.exe` runs directly. There is no installer and no MSIX.

Note: autostart is off by default. If you enable "Mit Windows starten" (Start with Windows) in the settings, the entry `HKCU\...\Run\TopBar` is created pointing to the currently running EXE (for debug builds, that is the debug EXE; leave it off while developing).

## Runtime files

- Settings: `%LOCALAPPDATA%\TopBar\settings.json` (a corrupted file is backed up as `settings.broken-<timestamp>.json`)
- Logs: `%LOCALAPPDATA%\TopBar\logs\topbar-<date>.log` (kept for 7 days)

In `settings.json`, there is a switch per element under `Features` (default `true`, except `UrlChecker`). The switches are also available under Settings → Anordnung (Layout) → "Funktionen" (Features) and take effect immediately. A disabled element and its settings page disappear. The **URL checker** (calls a list of URLs, turns red on errors) is off by default: set `"UrlChecker": true` and restart TopBar, then the "URL-Checker" page and the element appear in "Anordnung" (Layout).

## Project structure

- `src/TopBar.Core`: pure logic without UI and Win32 (settings, layout, formatting, weather codes, GPU aggregation)
- `src/TopBar.Platform`: system access (AppBar, performance counters, virtual desktops, registry, HTTP). All Win32 calls go through CsWin32.
- `src/TopBar.App`: WinUI 3 app (window, controls, view models, settings pages)
- `tests/TopBar.Core.Tests`: xUnit tests

Further documents: `TOPBAR_SPEC.md` (specification, extensions after M9 in section 18), `DECISIONS.md` (decisions and deviations), `PROGRESS.md` (status).

## Notes for contributors and AI agents

- Read `TOPBAR_SPEC.md` before making any change, especially section 0 (working rules) and section 18 (additions after M9).
- Always document new features and changes (rule 9 in section 0): spec, `DECISIONS.md` (deviations only), `PROGRESS.md`, and for visible changes also `README.md`.
- Code, identifiers, comments and commit messages in English; visible UI texts in German.
- Code that is not built or not tested is noted as such in `PROGRESS.md`.

## Known limitations

- Some package versions are still floating versions (see `DECISIONS.md`).
- Date names in the bar depend on the system's ICU data (e.g. "Sept." instead of "Sep.").
- Weather data comes from [Open-Meteo](https://open-meteo.com/) (no API key required).

## License

[MIT](LICENSE). For bundled third-party components see `THIRD_PARTY_NOTICES.md`.
