# Mario Teaches Typing Native 1.1

Version 1.1 expands the native Windows port across modern wide displays while
preserving the original 320×200 presentation as the default.

## Download

Download `Mario Teaches Typing Native.exe` and run it directly on a 64-bit
Windows 10 or Windows 11 system.

The application is portable and self-contained. It includes the .NET 8
runtime, WinForms libraries, native runtime components, game data, movies,
speech, music, sound effects, and lessons. Users do not need to install .NET,
Visual C++ redistributables, DOSBox, an original executable, or separate media.

## Highlights

- Adds persistent 16:9 widescreen and 21:9 ultra-widescreen modes throughout
  the title, menus, lessons, Practice, results, animations, and movies.
- Expands moving lesson worlds to expose more upcoming letters while retaining
  the original outdoor movement-speed ceiling.
- Expands Practice to seamless 45-column or 62-column pages in extended modes.
- Reflows the original main menu across the selected canvas and composites
  Mario's speech animation over continuous wallpaper lettering.
- Restores the original startup music call beneath the title and main menu.
- Restores all five recovered Castle pipe clip windows so each playable
  character disappears correctly while entering and leaving pipes.
- Remains a single native Windows executable with no emulator, CD prompt,
  external runtime, or loose game files.

Use **F10** for widescreen, **F9** for ultra-widescreen, and **F11** or
**Alt+Enter** for full screen. Press an active display-mode key again to return
to classic 4:3.

See [CHANGELOG.md](../CHANGELOG.md), [CREDITS.md](../CREDITS.md), and the
[function audit](FUNCTION-AUDIT.md) for full details.

## Verification

```text
File:    Mario Teaches Typing Native.exe
Version: 1.1.0
Size:    538,652,751 bytes
SHA-256: 33C103814E386C8B28163EE0ACE39A4C66553C47E744453A1EFBDE07040E8CA6
```

The packaged self-test completes all 43 checks, covering embedded media hashes,
288 native assets, codecs, graphics, lessons, input routes, gameplay state,
three display modes, expanded geometry, Castle pipe clipping, WPM/progression
rules, profiles, audio mappings, and the absence of emulator or external-media
dependencies.

## Unofficial preservation project

This is not an official Nintendo or Interplay release. Original game content,
characters, names, artwork, audio, and trademarks remain the property of their
respective rights holders. See [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md).
