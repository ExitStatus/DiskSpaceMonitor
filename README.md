# Disk Space Monitor

A borderless desktop widget that sits **behind** all your other windows (like a
wallpaper gadget) and shows remaining space on your drives as circular gauges,
with the drive, free space, and percentage free in the middle. Show one drive or
several — each gauge is its own window you can place and size independently.

## Features

- **Circular gauge** – ring fills with used space; colour shifts green → amber →
  red as free space runs low. Centre shows the drive letter and total size, the
  free space, and the percentage free.
- **Multiple drives** – one gauge per drive, managed from the settings dialog
  (at least one is always shown).
- **Always behind** – pinned to the bottom of the window Z-order; hidden from
  Alt-Tab and the taskbar; never steals focus.
- **Transparent** – no window chrome, just a subtle dark disc behind the gauge
  for readability over any wallpaper.
- **Click-through when idle** – normally your clicks pass straight through to the
  desktop. Hold **Ctrl** to make the widget interactive.
- **Snapping & no overlap** – dragging snaps to other widgets and to screen edges
  (stopping at the taskbar), keeps the whole widget on-screen, moves freely across
  multiple monitors, and never lets widgets overlap.
- **Customisable appearance** – background and overall opacity, ring thickness,
  and the colour of every part (background, track, healthy/low/critical ring,
  text), all with a live preview.
- **Configurable thresholds** – choose the free-space percentages at which the
  ring turns "low" and "critical".
- **Auto-start** – optionally launch at login (a per-user `Run` registry entry).
- **Efficient** – idle between refreshes; a single low-level keyboard hook wakes
  the UI only while Ctrl is held (no continuous polling). The working set is
  trimmed while idle to keep the memory footprint small.
- **Remembers** every setting: each widget's position and size, the refresh
  interval, thresholds, and all appearance choices.

## Controls

| Action    | How                                                            |
|-----------|----------------------------------------------------------------|
| Move      | Hold **Ctrl**, click-drag anywhere on the widget               |
| Resize    | Hold **Ctrl**, hover, drag a corner handle                     |
| Settings  | Hold **Ctrl**, click the ⚙ button, or right-click → Settings…  |
| Hide one  | Right-click → **Hide this drive**                              |
| Quit      | Settings → **Exit Application**, or right-click → Exit application |

## Project structure

A single solution with the app and its tests in separate project folders:

```
DiskSpaceMonitor.slnx
DiskSpaceMonitor/              # WPF app
  App.xaml(.cs)               # composition root + window/lifecycle manager
  Drives/                     # ByteSize, DiskGauge, DriveReader, DriveCatalog, records
  Layout/                     # WidgetLayout (snapping + collision geometry)
  Settings/                   # WidgetSettings, JsonSettingsStore
  Startup/                    # AutoStartService (HKCU Run entry)
  Interop/                    # NativeMethods, CtrlHook (Win32)
  Diagnostics/                # ErrorLog
  Views/                      # MainWindow, RingGauge, SettingsWindow, tabs
DiskSpaceMonitor.UnitTests/   # NUnit + FluentAssertions, mirrors the app folders
```

UI-free logic (geometry, byte formatting, gauge thresholds, settings
load/save/migration) lives in small services behind interfaces, so it's covered
by unit tests; the WPF views are thin.

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (to build) or the .NET 10
  Desktop Runtime (to run a published build)

## Run

```powershell
dotnet run --project DiskSpaceMonitor
```

## Test

```powershell
dotnet test
```

## Build a standalone executable

```powershell
dotnet publish DiskSpaceMonitor -c Release -r win-x64 --self-contained false
```

Or use the bundled folder profile, which publishes a Release build to
`C:\Tools\DiskSpaceMonitor`:

```powershell
dotnet publish DiskSpaceMonitor -p:PublishProfile=FolderProfile
```

Both are framework-dependent, so they need the .NET 10 Desktop Runtime installed.

## Settings

The settings dialog (⚙ button or right-click → Settings…) has four tabs:

- **Drives** – which drives to show (at least one is always kept).
- **Options** – refresh interval, auto-start at login, and the free-space
  percentages at which the ring turns "low" and "critical".
- **Appearance** – background opacity, overall opacity, and ring thickness.
- **Colours** – the RGB colour of each part of the gauge.

Appearance and colour changes preview live on the widgets; **Cancel** reverts
them, **OK** applies and saves.

All of this — drives, positions, sizes, refresh interval, thresholds, opacities,
ring thickness, and colours — is saved to:

```
%AppData%\DiskSpaceMonitor\settings.json
```

Delete that file to reset the widgets to defaults. A pre-multi-drive settings
file is migrated automatically on first load.

**Auto-start** is controlled by the *Options* tab. When enabled it writes a
per-user `Run` entry named `DiskSpaceMonitor` under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` pointing at the running
executable; disabling it removes the entry. Publish to a stable location (see
[Build a standalone executable](#build-a-standalone-executable) above) before
enabling, so the registered path doesn't point at a build folder.

## License

Licensed under the [MIT License](LICENSE). The application icon is original
artwork created for this project and is covered by the same licence — the
project bundles no third-party assets (see [attributions.md](attributions.md)).
