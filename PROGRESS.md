# PROGRESS

Legende: [x] Code geschrieben · [ ] offen. **Nichts davon wurde gebaut oder gestartet** – „Fertig, wenn …“ aus Abschnitt 13 ist für keinen Meilenstein geprüft.

- [x] M1 Solution, Projekte, Pakete, DI, Logging, `ISettingsService` (JSON), Single Instance, DECISIONS/PROGRESS/README
- [x] M2 `BarWindow` (rahmenlos, Acrylic, nicht in Alt+Tab), AppBar am Hauptmonitor, Kontextmenü mit „Beenden“
- [x] M3 `ThreeSectionPanel` + Tests, Uhr & Datum
- [x] M4 CPU, RAM, Netzwerk, GPU, Warnfarben, Zahnrad, Klick → Task-Manager
- [x] M5 Einstellungsfenster (6 Seiten), Live-Anwendung, Autostart
- [x] M6 Wetter inkl. Ortssuche, Icons, Tooltip, Fehlerverhalten
- [x] M7 Virtuelle Desktops: Punkte/Pille, Animation, Klick, Mausrad, Events, Fallback
- [x] M8 Multi-Monitor, Vollbild-Erkennung, Explorer-Neustart, DPI-Wechsel, „Neu laden“
- [x] M9 README mit Build-/Publish-Anleitung

## Offen / Bekanntes

- [ ] `dotnet build -c Debug` auf Windows ausführen und Compilerfehler beheben (v. a. CsWin32-Signaturen, siehe README).
- [ ] `dotnet test` ausführen.
- [ ] Abnahmekriterien aus Abschnitt 14 manuell abhaken.
- [ ] Paketversionen pinnen (DECISIONS.md).
- [ ] Ressourcenverbrauch im Leerlauf messen (< 1 % CPU, < 100 MB).
- Bekannt: VirtualDesktopAccessor.dll benötigt Windows 11 24H2 (Build ≥ 26100.2605).
- Bekannt: Im Debug-Build schreibt der erste Start den Autostart-Eintrag auf die Debug-EXE (Einstellung „Mit Windows starten“ ausschalten oder Release nutzen).
