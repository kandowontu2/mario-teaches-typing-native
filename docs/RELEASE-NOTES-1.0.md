# Mario Teaches Typing Native 1.0

The first complete native Windows release of the enhanced CD-ROM edition.

## Download

Download `Mario Teaches Typing Native.exe` and run it directly on a 64-bit
Windows 10 or Windows 11 system.

This is a portable, self-contained application. It includes the .NET 8 runtime,
WinForms libraries, native runtime components, game data, movies, speech,
music, sound effects, and lessons. Users do not need to install .NET, Visual
C++ redistributables, DOSBox, an original executable, or separate media files.

## Highlights

- Native title sequence, menus, four lesson worlds, typing/scoring engine,
  student profiles, reports, certificates, printing, demo, and settings.
- Original graphics, animation, video, speech, music, sound, and lesson formats
  decoded by the port itself.
- One executable with no installer, loose DLLs, emulator, or CD prompt.
- Original main-menu speech behavior, including removal of Mario's movie frame
  when a spoken clip finishes.
- Persistent progress at `%LOCALAPPDATA%\MarioTypingNative\progress.json`.

See [CHANGELOG.md](../CHANGELOG.md), [CREDITS.md](../CREDITS.md), and the
[function audit](FUNCTION-AUDIT.md) for full details.

## Verification

```text
File:    Mario Teaches Typing Native.exe
Version: 1.0.0
Size:    538,624,079 bytes
SHA-256: 93D050ED47AD6F5651731EF7A75A7FD7921520DE3FFF387B56A3CB0CCD20D318
```

The packaged self-test completed all 41 checks, covering embedded media hashes,
288 native assets, codecs, graphics, lessons, input routes, gameplay state,
WPM/progression rules, profiles, audio mappings, and the absence of emulator or
external-media dependencies.

## Unofficial preservation project

This is not an official Nintendo or Interplay release. Original game content,
characters, names, artwork, audio, and trademarks remain the property of their
respective rights holders. See [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md).
