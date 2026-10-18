# Pomodoro Tray

A Windows system-tray Pomodoro timer built with WinForms (.NET 8). No main window —
it lives entirely in the notification area, and the tray icon itself shows the
remaining minutes (or seconds, in the final minute) as text on a colored badge:
red for work, green for short breaks, blue for long breaks.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (this is a Windows-only
  project — `net8.0-windows` with WinForms — so it must be built on Windows)

## Build & run

```
cd PomodoroTray
dotnet run
```

Or open the folder in Visual Studio and press F5.

To produce a standalone .exe:

```
dotnet publish -c Release -r win-x64 --self-contained false
```

The output lands in `bin\Release\net8.0-windows\win-x64\publish\`.

## Using it

- The app starts minimized to the tray with no visible window.
- Right-click the tray icon for: current status, Start/Pause, Skip to next,
  Reset, Settings, and Exit.
- Double-click the tray icon to toggle Start/Pause.
- A balloon notification (plus an optional sound) fires whenever a work or
  break interval finishes, and the timer automatically advances: Work →
  Short break → ... → Long break (after N work sessions) → Work.

## Configurable settings (right-click → Settings...)

- Work interval length (minutes)
- Short break length (minutes)
- Long break length (minutes)
- Number of work sessions before a long break
- Whether the next interval starts automatically
- Whether a sound plays when an interval ends

Settings are saved to `%AppData%\PomodoroTray\settings.json` and reloaded
automatically the next time the app starts.

## Files

- `Program.cs` — entry point, starts the tray application context
- `TrayApplicationContext.cs` — timer state machine, tray icon, context menu,
  and the code that draws the live countdown badge each second
- `SettingsForm.cs` — the settings dialog (built entirely in code)
- `PomodoroSettings.cs` — settings model with JSON load/save
