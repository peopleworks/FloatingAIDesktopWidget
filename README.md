# FloatingAIDesktopWidget

ES: Widget flotante tipo “chat head” para Windows (WinForms / .NET) que queda siempre encima y, al hacer click, lanza una aplicación configurable (ruta + parámetros).

EN: Floating “chat head” widget for Windows (WinForms / .NET). Always-on-top and launches a configurable target application on click (path + args).

## Features

- Floating draggable button (bottom-right by default)
- Tray icon + context menu (open/close target, edit settings, reload config, reset position, exit)
- `appsettings.json` hot reload + persistent position in `%AppData%`
- Global hotkey (configurable) with conflict warning
- Single instance for the widget (won’t spawn duplicates)
- Target management: focus existing process, avoid duplicate target launches, close target main window
- ES/EN UI switch (menu) + `UI.Language` setting

## Requirements

- Windows 10/11+
- .NET Desktop Runtime (matching the target framework)
- For building: `dotnet` SDK 9+ (project targets `net9.0-windows`)

## Build & Run

Build:

```powershell
dotnet build .\FloatingAIDesktopWidget.slnx -c Release
```

Run:

```text
FloatingAIDesktopWidget\bin\Release\net9.0-windows\FloatingAIDesktopWidget.exe
```

## Configuration (`appsettings.json`)

The widget reads `appsettings.json` from the **same folder as the widget `.exe`** (it’s copied to output).

Important: in JSON, Windows paths must use `\\` or `/`. Don’t end strings with a trailing `\`.

Example:

```json
{
  "Target": {
    "FileName": "C:\\TEMP\\Copilot\\PeopleWorksCopilot.exe",
    "Arguments": "",
    "WorkingDirectory": "C:\\TEMP\\Copilot",
    "RunAsAdministrator": false,
    "SingleInstance": true,
    "FocusExistingIfRunning": true,
    "AllowCloseFromMenu": true
  },
  "UI": {
    "Size": 64,
    "Margin": 16,
    "AlwaysOnTop": true,
    "ShowInTaskbar": false,
    "SnapToEdge": true,
    "Opacity": 1.0,
    "IconPath": "",
    "Language": "auto"
  },
  "Hotkey": {
    "Enabled": false,
    "Gesture": "Ctrl+Shift+Space"
  }
}
```

### Settings reference

**Target**

- `Target.FileName`: executable (or command) to launch
- `Target.Arguments`: optional arguments
- `Target.WorkingDirectory`: optional; if empty, inferred from the executable
- `Target.RunAsAdministrator`: requests elevation (UAC) using `runas`
- `Target.SingleInstance`: if the target is already running, don’t launch another one
- `Target.FocusExistingIfRunning`: bring the existing target to the foreground
- `Target.AllowCloseFromMenu`: enable “Close assistant” in the context menu

**UI**

- `UI.Size`: button size (px)
- `UI.Margin`: edge margin (px)
- `UI.SnapToEdge`: snap to left/right edge on release
- `UI.AlwaysOnTop`: keep widget always on top
- `UI.IconPath`: optional image (png/jpg/ico); if empty, a default icon is generated
- `UI.Language`: `auto` / `es` / `en` (`auto` uses Windows UI language)

**Hotkey**

- `Hotkey.Enabled`: enable/disable global hotkey
- `Hotkey.Gesture`: examples: `Ctrl+Shift+Space`, `Ctrl+Alt+F8`, `Win+Shift+S`

## Usage

- Left click: launch/focus the target
- Drag: move the button (position is saved)
- Right click (or tray icon): context menu
- Language: switch ES/EN from the menu (persists in `%AppData%`)

## Docs (HTML)

- `Documents/floating-ai-desktop-widget-guide.html` (includes ES/EN switch)

## Notes

- If the target runs as Administrator and the widget does not, Windows may block focus/close (UIPI). Run both at the same privilege level if needed.
- DevExpress is not required (the widget is pure WinForms).

