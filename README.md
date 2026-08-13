# Mario Teaches Typing Native

An unofficial, source-level Windows port of the enhanced CD-ROM edition of
*Mario Teaches Typing*. It recreates the original application as a native
Windows program: no DOSBox, virtual machine, mounted disc, CD-location prompt,
or original DOS executable is used at runtime.

## Download version 1.0

Download **Mario Teaches Typing Native.exe** from the
[v1.0.0 release](https://github.com/kandowontu/mario-teaches-typing-native/releases/tag/v1.0.0).

- Windows 10 or 11, x64
- Portable, self-contained executable; no installer or separate runtime
- Original game data, movies, speech, music, and lessons are embedded
- No original executable or CD is required to run this release
- Save data is written to `%LOCALAPPDATA%\MarioTypingNative\progress.json`

Windows may display a reputation warning because the executable is not
code-signed. Verify the SHA-256 checksum shown in the release notes before
running it.

## What is implemented

- The original 320×200 title animation, main menu, top bar, lesson cards,
  auxiliary screens, blackboard, report card, and certificates.
- Outdoor, underwater, castle, and practice activities with the original
  backgrounds, character variants, enemies, blocks, hand/finger guidance,
  prompts, scoring, timing, progression, bonuses, and reward paths.
- Native decoders for the original CRX, GSO, GCO, BAN, ANI, AV, MVE, XMIDI,
  RLD, and lesson-data formats.
- All 82 recovered enhanced-CD speech cue entries and native PCM playback.
- All seven Interplay MVE reward movies, original music and sound effects, and
  the CD rap track.
- Multiple student profiles, editable WPM targets and lesson times, character
  selection, progress history, problem-key reporting, audio controls, M.I.R.T.,
  printing, and persistent settings.
- Original 18.2 Hz timing conversion, integer net-WPM calculation, HUD text,
  progression gates, certificate thresholds, and key-to-finger mappings.

The app implements these systems directly in C# and WinForms. It never starts
or bundles an emulator. Detailed parity status is maintained in the
[function audit](docs/FUNCTION-AUDIT.md), with architectural notes in
[porting notes](docs/PORTING-NOTES.md).

## Controls

The port supports the original mouse and keyboard routes. On typing screens,
type the displayed characters; spaces are represented by the hand cue. Main
menu commands can be opened with the mouse or their original keyboard
accelerators. Student WPM and lesson-time fields accept direct digits,
Backspace, arrow keys, Page Up/Page Down, and the mouse wheel.

## Building from source

Requirements:

- Windows x64
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell 5.1
- A locally prepared `assets/original` tree matching
  [the documented layout](assets/original/README.md)

Original copyrighted game data is not stored in Git history. The official
release executable contains the data needed at runtime, but a source build
requires the developer to supply the expected local assets. `Build-Native.ps1`
validates the CHD and VHD byte lengths and SHA-256 checksums before embedding
them.

```powershell
.\scripts\Build-Native.ps1 -SourceMediaDirectory 'D:\path\to\source-media'
.\scripts\Test-Native.ps1
```

If `assets/original/media` already contains the validated images, the source
directory argument may be omitted. The resulting single-file application is
written to `dist\Mario Teaches Typing Native`.

The self-test checks embedded-media integrity, all native decoders, lesson and
progression rules, menu routes, student settings, gameplay state transitions,
original graphics, audio mappings, and the absence of emulator dependencies.

## Credits and license

See [CREDITS.md](CREDITS.md) for the original enhanced CD-ROM team and this
port’s contributors. The native-port source code is available under the
[MIT License](LICENSE.md); that license does **not** cover Nintendo, Mario,
Interplay, or other original game content.

This is a preservation and compatibility project, not an official Nintendo or
Interplay product. Mario and related names, characters, artwork, audio, and
trademarks belong to their respective rights holders. No endorsement is
claimed. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Project status

Version 1.0 is the first complete native release. Remaining work concerns
exhaustive frame-level audiovisual comparisons and broader hardware testing,
not replacement of the game with an emulator or launcher.
