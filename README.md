# Disk Space Monitor

A borderless desktop widget that sits **behind** all your other windows (like a
wallpaper gadget) and shows remaining space on your drives as circular gauges,
with used / free / total stats in the middle. Show one drive or several — each
gauge is its own window you can place and size independently.

## Features

- **Circular gauge** – ring fills with used space; colour shifts green → amber →
  red as free space runs low. Centre shows the drive, free space, used, and total.
- **Multiple drives** – one gauge per drive, managed from the settings dialog
  (at least one is always shown).
- **Always behind** – pinned to the bottom of the window Z-order; hidden from
  Alt-Tab and the taskbar; never steals focus.
- **Transparent** – no window chrome, just a subtle dark disc behind the gauge
  for readability over any wallpaper.
- **Click-through when idle** – normally your clicks pass straight through to the
  desktop. Hold **Ctrl** to make the widget interactive.
- **Snapping & no overlap** – dragging/resizing snaps edges to other widgets and
  stops them ever overlapping.
- **Efficient** – idle between refreshes; a single low-level keyboard hook wakes
  the UI only while Ctrl is held (no continuous polling).
- **Remembers** each widget's position and size, and the refresh interval.

## Controls

| Action    | How                                                          |
|-----------|--------------------------------------------------------------|
| Move      | Hold **Ctrl**, click-drag anywhere on the widget             |
| Resize    | Hold **Ctrl**, hover, drag a corner handle                   |
| Settings  | Hold **Ctrl**, click the ⚙ button, or right-click → Settings |
| Hide one  | Hold **Ctrl**, click the ✕ button                            |
| Quit      | Settings → **Exit Application**, or right-click → Exit        |

## Project structure

A single solution with the app and its tests in separate project folders:

```
DiskSpaceMonitor.slnx
DiskSpaceMonitor/              # WPF app
  App.xaml(.cs)               # composition root + window/lifecycle manager
  Drives/                     # ByteSize, DiskGauge, DriveReader, DriveCatalog, records
  Layout/                     # WidgetLayout (snapping + collision geometry)
  Settings/                   # WidgetSettings, JsonSettingsStore
  Interop/                    # NativeMethods, CtrlHook (Win32)
  Views/                      # MainWindow, RingGauge, SettingsWindow
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

## Settings

Drives, positions, sizes, and the refresh interval are saved to:

```
%AppData%\DiskSpaceMonitor\settings.json
```

Delete that file to reset the widgets to defaults. A pre-multi-drive settings
file is migrated automatically on first load.

## Notes / next steps

- Auto-start on login is intentionally **not** enabled. To add it later, create a
  per-user `Run` registry entry under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
