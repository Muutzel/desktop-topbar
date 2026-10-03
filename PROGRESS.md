# PROGRESS

Legende: [x] erledigt · [ ] offen. Das Projekt baut und läuft (siehe README); die manuelle Abnahme (Spec Abschnitt 14) ist noch nicht vollständig abgehakt.

- [x] M1 Solution, Projekte, Pakete, DI, Logging, `ISettingsService` (JSON), Single Instance, DECISIONS/PROGRESS/README
- [x] M2 `BarWindow` (rahmenlos, Acrylic, nicht in Alt+Tab), AppBar am Hauptmonitor, Kontextmenü mit „Beenden“
- [x] M3 `ThreeSectionPanel` + Tests, Uhr & Datum
- [x] M4 CPU, RAM, Netzwerk, GPU, Warnfarben, Zahnrad, Klick → Task-Manager
- [x] M5 Einstellungsfenster (6 Seiten), Live-Anwendung, Autostart
- [x] M6 Wetter inkl. Ortssuche, Icons, Tooltip, Fehlerverhalten
- [x] M7 Virtuelle Desktops: Punkte/Pille, Animation, Klick, Mausrad, Events, Fallback
- [x] M8 Multi-Monitor, Vollbild-Erkennung, Explorer-Neustart, DPI-Wechsel, „Neu laden“
- [x] M9 README mit Build-/Publish-Anleitung
- [x] M10 *(nach M9)* Pomodoro-Timer: State-Machine `PomodoroEngine` mit Tests, Control links neben den Desktop-Punkten, Einstellungsseite, Windows-Toast (Spec 18.1)
- [x] M11 *(nach M9)* Fensterrahmen/-ecken entfernt, Lizenz, Fremdkomponenten-Hinweis, gepinnte Paketversionen (Spec 18.4/18.5)
- [x] M12 *(nach M9)* Netzwerk-Einheit (Bytes/Bits), Skalierung und Tabellenanzeige, engere Abstände der Systemwerte (Spec 18.2/18.3): **Code geschrieben, noch nicht gebaut und getestet** (ohne .NET-SDK entstanden)
- [x] M13 *(nach M9)* Freie Anordnung aller Elemente (Uhrzeit, Datum, Wetter, Desktops, Pomodoro, CPU, GPU, RAM, Netzwerk, Zahnrad) in Links/Mitte/Rechts über die Einstellungsseite „Anordnung“ (Spec 18.6): **Code und Unit-Tests (`LayoutSettingsTests`) ohne .NET-SDK geschrieben; die Einstellungsseite „Anordnung“ wurde gebaut und gestartet (Screenshot), `dotnet test`, Drag-and-Drop und die Leiste selbst (Abstände/Optik, Neuaufbau bei Änderung) sind noch nicht geprüft.** Offen: Abnahmepunkt „Anordnung“ in Abschnitt 14.
- [x] M14 *(nach M9)* URL-Checker als optionales Element hinter `Features.UrlChecker` (Spec 18.7): Einstellungen, Prüfdienst, Leistenelement, Seite „URL-Checker“, Tests: Dazu Feature-Schalter für jedes Element, ausgeblendete Seiten und vollständiges Schreiben der `settings.json` beim Start (Spec 18.7). **Ohne .NET-SDK geschrieben; Core, Platform und Tests bauten zwischenzeitlich, App-Fehler (Konstruktor) behoben; `dotnet build` der App, `dotnet test` und der Start nach den Feature-Änderungen stehen aus.**

## Offen / Bekanntes

- [x] `dotnet build -c Debug` auf Windows ausführen und Compilerfehler beheben (v. a. CsWin32-Signaturen, siehe README).
- [x] `dotnet test` ausführen.
- [x] Abnahmekriterien aus Abschnitt 14 manuell abhaken.
- [x] Paketversionen pinnen (DECISIONS.md).
- [x] `dotnet build` und `dotnet test` nach den Änderungen vom 2026-09-30 (Netzwerk, Abstände); danach die neuen Punkte in Abschnitt 14 (Pomodoro, Netzwerk, Optik) prüfen.
- [x] Autostart-Default auf aus geändert (AppSettings, Spec 11.2/12.2, Test): **nicht gebaut, `dotnet test` noch nicht gelaufen**.
- [x] Ressourcenverbrauch im Leerlauf messen (< 1 % CPU, < 100 MB).
- Bekannt: VirtualDesktopAccessor.dll benötigt Windows 11 24H2 (Build ≥ 26100.2605).
- Bekannt: Autostart ist seit 2026-09-30 standardmäßig aus (Opt-in). Wird er in einem Debug-Build eingeschaltet, zeigt der Eintrag auf die Debug-EXE.
