# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-07-24

### Added

- **Pluggable widget styles** – the widget's visual is now chosen from a **Widget**
  dropdown in the settings dialog. A new style is added by implementing a single
  interface that covers its display, configuration, and settings tabs. Each style's
  files live in their own `Widgets/<Name>/` folder.
- **Concentric circles widget** – a second style: a single window that draws every
  drive as a nested ring (innermost = first drive), each swept to that drive's
  used %, with a per-ring label chip ("`C 90%`"). Ring thickness, a ring colour per
  drive, and the transparency of the unused-space track are configurable. The
  chips are coloured by the drive's status (healthy/low/critical) using the same
  free-space thresholds as the circular gauge, with their own status colours.
  Labels nudge apart automatically so they never overlap, and the window sizes
  itself tightly to its content. Selecting a single-instance style collapses the
  per-drive windows into one and back again, remembering its own position and size.
- **Vertical bar graph widget** – a third style: a single window with a vertical bar per
  drive on a 0–100% (used space) axis, each bar coloured by free-space status. An
  **Orientation** dropdown flips the axis between **Bottom Up** (0% at the bottom,
  bars growing upwards — the default) and **Top Down** (0% at the top, bars hanging
  downwards); the y-axis, the fill direction and the used-space captions all follow.
  The gap between bars, the unused-space transparency, the track/status colours, and
  the thresholds are configurable, and optional **Show used space** / **Show total
  space** captions display the humanized used amount on each bar and the total
  above it. The bars fill the window in both directions (see *Free sizing* below).
  The used/total captions auto-size to one shared size that fits the bar and
  rotate 90° when a bar is too narrow for horizontal text. A **Bar style** on the
  Effects tab outlines the fill: **Plain**, **Border** (an even outline of a chosen
  size and colour), or **3D Border** (a bevel of the chosen size, with the
  **highlight** along the top and left and the **lowlight** down the right and
  along the bottom, so the bar reads as raised).
- **Horizontal bar graph widget** – a fourth style: the vertical bar graph turned on
  its side. A bar per drive runs across a 0–100% (used space) axis with the bars
  stacked down the window, the drive label and its used % at the 0% end, and the
  total space at the 100% end. An **Orientation** dropdown picks **Left to Right**
  (0% at the left — the default) or **Right to Left**, mirroring the axis, the fill
  direction, the labels and the totals. It offers the same bar gap, unused-space
  transparency, colours, thresholds, caption toggles, bar styles and text glow as the
  vertical graph, and keeps its own copy of them all. Captions scale to the bar's slot
  instead of rotating, since a horizontal bar has the whole axis to write along. Both
  graphs share their configuration, settings editor and fill rendering
  (`Widgets/BarGraph/`), so an option added to one arrives in the other.
- **Free sizing for both bar graphs** – a bar graph's width and height are dragged
  independently, from a resize handle at the **centre of each side** as well as at
  each corner, and the chart fills whatever rectangle you give it: stretch a
  horizontal graph sideways and its bars lengthen, stretch it downwards and they
  thicken; the vertical graph does the same the other way round. Text size follows
  the *smaller* of the two directions, so a one-way stretch resizes the bars and
  leaves the labels alone — only a graph that has grown in both directions gets
  bigger text — and it stops at the minimum size, so a graph squeezed along one axis
  holds its labels at something readable rather than shrinking them away.
  Every multi-drive style keeps its own window rectangle, so switching between the
  concentric, vertical and horizontal styles restores the shape each was last given.
- **App-wide font and text size bounds** – the *General* tab gains a **Font** row
  showing the chosen font drawn in itself, with a **Choose…** button opening a
  chooser that lists every installed family (each drawn in its own face), filters as
  you type, and previews a sample; and **Minimum size** / **Maximum size** sliders
  that bound what every widget renders text at. All three apply to all four styles,
  so text doesn't change when you switch, and they preview live as you pick — Cancel
  puts them back. The bounds are on the *rendered* size, so they hold whatever size
  the widget is: a gauge dragged across the screen stops its labels growing at the
  maximum instead of turning them into a poster, and a small one holds them at the
  minimum instead of shrinking them away.
- **Colour picker** – each colour row now has a live swatch, an editable `#RRGGBB`
  box (with copy/paste), and a pipette button that opens a hue/saturation/brightness
  chooser with gradient slider tracks and a live preview that updates the widget as
  you drag; **Cancel** reverts. Replaces the inline R/G/B sliders.
- **Per-style settings** – every widget style keeps its own configuration, so
  switching styles or restarting no longer resets another style's settings.
- **Text outer glow** – the circular gauge and both bar graphs each gain an
  **Effects** tab with an **Outer glow** radius (0–10) and an **Outer glow colour**,
  adding an alpha-blended halo around the widget's text (the gauge's centre stats;
  a bar graph's labels, captions and axis ticks). The glow renders as stacked blurred
  copies strictly *behind* the text, so the glyphs stay sharp. It's a reusable
  component (`Widgets/Effects/`), ready for future widgets to adopt.

### Changed

- **Reorganised the settings dialog** into **General · Drives · <widget's own tabs>**.
  General holds auto-start, refresh interval, the Widget dropdown, and overall
  opacity; the chosen widget contributes its own tabs (for Circular: **Appearance**
  and **Colours**). The low/critical thresholds moved from *Options* to *Appearance*.
- The chosen widget and all its settings apply to **every** drive; live preview now
  updates all instances together, and Cancel reverts them.
- Appearance is now stored under a per-widget config block in `settings.json`;
  pre-1.1 settings files are migrated automatically on first load, preserving the
  existing look.
- The **⚙ button** in the Ctrl-hover overlay moved from the bottom edge to the
  centre of the widget and is twice the size, making it an easier target on a
  small widget and freeing the bottom-centre resize handle on the bar graphs to
  sit where the other side handles do.

### Removed

- The 600px ceiling on how large a widget could be dragged, and the 120px floor
  under each dimension. A widget may now be sized anywhere from 60px up to filling
  its monitor.

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

[1.1.0]: https://github.com/ExitStatus/DiskSpaceMonitor/releases/tag/v1.1.0
[1.0.0]: https://github.com/ExitStatus/DiskSpaceMonitor/releases/tag/v1.0.0
