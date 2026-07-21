# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-21

First public release.

### Added

- **Disk space widget** – a borderless, transparent desktop gadget that sits
  behind all other windows and shows a drive's usage as a circular gauge, with
  the drive letter and total size, free space, and percentage free in the centre.
- **Multiple drives** – one independently placed and sized gauge per drive,
  managed from the settings dialog (at least one is always shown).
- **Always behind & click-through** – pinned to the bottom of the Z-order, hidden
  from Alt-Tab and the taskbar, and never steals focus; clicks pass through to the
  desktop until **Ctrl** is held to make the widget interactive.
- **Move & resize** – Ctrl-drag to move; Ctrl-hover and drag a corner handle to
  resize. Positions and sizes are remembered.
- **Snapping & no overlap** – dragging snaps to other widgets and to screen edges
  (stopping at the taskbar), keeps the whole widget on-screen, moves freely across
  multiple monitors, and never lets widgets overlap.
- **Settings dialog** – tabbed (Drives, Options, Appearance, Colours), themed to
  follow the system light/dark theme, with a live preview that reverts on cancel.
- **Configurable refresh interval** for the disk figures.
- **Configurable thresholds** – choose the free-space percentages at which the
  ring turns "low" and "critical".
- **Customisable appearance** – background and overall opacity, ring thickness,
  and the colour of every part of the gauge (background, track,
  healthy/low/critical ring, text).
- **Auto-start at login** – optional per-user `Run` registry entry.
- **Efficient by design** – idle between refreshes, woken only by a single
  low-level keyboard hook while Ctrl is held; the working set is trimmed and the
  GC tuned for a small memory footprint.
- **Resilience** – global exception handling and error logging keep the widget
  running instead of vanishing on an unexpected error.
- **Original application icon**, released under the project's MIT licence.
- **Unit tests** covering the UI-free logic (layout geometry, byte formatting,
  gauge thresholds, settings load/save/migration).
- **Folder publish profile** targeting `C:\Tools\DiskSpaceMonitor`.

[1.0.0]: https://github.com/ExitStatus/DiskSpaceMonitor/releases/tag/v1.0.0
