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
- [ ] M12 *(nach M9)* Netzwerk-Einheit (Bytes/Bits), Skalierung und Tabellenanzeige, engere Abstände der Systemwerte (Spec 18.2/18.3): **Code geschrieben, noch nicht gebaut und getestet** (ohne .NET-SDK entstanden)

## Offen / Bekanntes

- [x] `dotnet build -c Debug` auf Windows ausführen und Compilerfehler beheben (v. a. CsWin32-Signaturen, siehe README).
- [x] `dotnet test` ausführen.
- [x] Abnahmekriterien aus Abschnitt 14 manuell abhaken.
- [x] Paketversionen pinnen (DECISIONS.md).
- [x] `dotnet build` und `dotnet test` nach den Änderungen vom 2026-09-30 (Netzwerk, Abstände); danach die neuen Punkte in Abschnitt 14 (Pomodoro, Netzwerk, Optik) prüfen.
- [ ] Ressourcenverbrauch im Leerlauf messen (< 1 % CPU, < 100 MB).
- Bekannt: VirtualDesktopAccessor.dll benötigt Windows 11 24H2 (Build ≥ 26100.2605).
- Bekannt: Im Debug-Build schreibt der erste Start den Autostart-Eintrag auf die Debug-EXE (Einstellung „Mit Windows starten“ ausschalten oder Release nutzen).
