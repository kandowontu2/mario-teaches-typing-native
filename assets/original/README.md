# Local original-data layout

This directory is intentionally excluded from Git except for this file.
Original game data is not covered by the native-port source license.

A development machine that builds the complete application uses:

```text
assets/original/
├── game/MARIO/       # extracted enhanced CD-ROM game files
├── cd-audio/
│   └── track02.wav
├── lessons/          # original .LEE lesson files
└── media/
    ├── mario teaches typing.chd
    └── mario teaches typing.vhd
```

`scripts/Build-Native.ps1` can import validated CHD/VHD files from the directory
passed with `-SourceMediaDirectory`. The extracted `game/MARIO`, `cd-audio`, and
`lessons` data must already be present locally. The original DOS executable is
reference material only and is not executed or embedded by the native app.
