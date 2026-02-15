# minol

minol is a lightweight, always-on-top Windows overlay that displays a timer and keyboard input (including key chords) with configurable appearance. Themes and settings persist to disk and can be saved/loaded as JSON files.

**Key Features**
- Real-time elapsed timer (click to pause/resume).
- Keyboard input/ chord display with a smart hold-and-fade behaviour.
- Full theme management: save/load/delete theme JSON files.
- Persistent settings stored in `%AppData%/minol/settings.json`.

**Quick Start (Windows)**
1. Build:

```bash
dotnet build
```

2. Run:

```bash
dotnet run
```

3. Overlay: The app opens as an always-on-top transparent window. Right-click the overlay to access the context menu (Edit, Start, Pause, Reset, Exit). You can also click to pause and unpause.

**Settings & Themes**
- Default settings file location: `%AppData%/minol/settings.json`.
- Themes are saved in: `%AppData%/minol/Themes/` as individual `.json` files.
- You can: Save the current configuration as a named theme, load an existing theme, delete themes, and mark a theme as the default. If a default theme is set, the app attempts to load it on startup.

**Where settings are stored**
- Settings: `%AppData%\minol\settings.json`
- Themes: `%AppData%\minol\Themes\` (each theme is a `.json`)
- Default theme marker: `%AppData%\minol\default_theme.txt`
