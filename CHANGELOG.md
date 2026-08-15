# Changelog

All notable native-port releases are documented here.

## 1.1.0 — 2026-08-15

- Restored the original song 1 startup call so music begins under the title
  greeting, continues into the main menu, and resumes after menu detours or the
  Rap track when Music is enabled.
- Added persistent, mutually exclusive **Widescreen [F10]** and
  **Ultra Wide [F9]** settings. The entire application now uses 427×200 at 16:9
  or 560×200 at 21:9, including the title, menus, auxiliary screens, Practice,
  results, bonus/ending animations, AV speech, and MVE reward movies.
- Reflowed the main menu movie, headers, lesson rail, popups, and hitboxes across
  the full canvas; composited fixed panels over the original repeating Mario
  wallpaper; and expanded Practice into one seamless 45- or 62-column sheet.
- Composited Mario's main-menu speech animation over one continuous wallpaper,
  removing the AV clip's differently colored baked backdrop so its lettering
  remains seamless in extended display modes.
- Extended outdoor target staging and Water/Castle prompt rendering so both
  extended modes reveal more upcoming letters without increasing the original
  outdoor movement-speed ceiling.
- Restored all five disassembly-recovered Castle character clip windows so
  Mario, Luigi, and Peach disappear correctly behind both pipe entrances and
  the vertical pipe lip during room transitions.
- Expanded the packaged self-test to 43 checks, including three-mode display
  persistence and geometry, extended outdoor look-ahead, and every Castle
  clip-window boundary.

## 1.0.0 — 2026-08-13

- First complete native Windows release.
- Reimplemented the original title, menus, student management, lesson
  selection, four typing activities, scoring, progression, reports,
  certificates, printing, demo route, M.I.R.T., and persistent settings.
- Added native original-format graphics, animation, AV, MVE, XMIDI, PCM, RLD,
  and lesson decoders with no DOSBox or original-executable runtime dependency.
- Embedded the full runtime game data and CD audio in one portable executable.
- Restored original main-menu speech behavior: Mario's movie area is returned
  to the underlying menu after each clip finishes.
- Added a 41-check packaged self-test covering assets, codecs, state routes,
  gameplay behavior, audio mappings, progression, and dependency boundaries.
