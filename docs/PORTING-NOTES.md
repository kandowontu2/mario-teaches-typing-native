# Native-port notes

## Architecture boundary

The deliverable is a Windows application that implements game behavior itself.
`MARCD.EXE` is reference material only and is never launched. The runtime has no
DOSBox, emulated CPU, virtual drive, MSCDEX layer, or CD-location setting.

The source CHD (143,617,768 bytes), VHD (7,340,544 bytes), 288 extracted game
files, and CD audio are compile-time resources. `EmbeddedAssets` and
`OriginalAssetStore` expose read-only streams for native decoders. The lesson
engine reads the `*.LEE` resources directly; it does not extract either disk
image while running.

## Implemented native systems

1. Original CRX framebuffers, VGA palettes, GSO/GCO sprite banks, BAN background
   deltas, and ANI character animation.
2. Original menu, auxiliary, gameplay, blackboard, certificate, and report-card
   screens rendered through the native Windows framebuffer.
3. Native decoding/playback of the enhanced-CD AV format (224x144, 15 FPS,
   10,980 Hz PCM) and Interplay MVE reward movies (320x200 video and PCM).
4. The original executable's seven recovered AV cue tables: 82 initialized
   entries for welcome, idle, outdoor, underwater, castle, and debrief speech.
5. Native XMIDI sequencing through Windows MIDI, original RLD sound effects,
   and embedded CD-track playback.
6. Original lesson parsing and recovered level mapping, four world activity
   modes, continuous prompts, timed sessions, net-WPM, accuracy, errors,
   problem-key tracking, progression gates, and rewards.
7. Multi-student JSON persistence, editable WPM goal and lesson times,
   character/world persistence, certificate bits, global audio settings,
   native printing, and the original persisted M.I.R.T. speech-response toggle.
8. Headless resource, codec, progression, profile, cue-table, golden-frame, and
   package-verification paths.
9. The original greeting sequence: Interplay hold, twenty generated 7/8-scale
   Mario logo frames, header movement, thirteen-letter title reveal, and final
   hold; plus the original forty-frame Castle winner ending.
10. Optional application-wide 427x200 widescreen and 560x200 ultra-widescreen
    framebuffers. WinForms presents them at 16:9 and 21:9 using the same
    effective pixel aspect as the original 320x200-at-4:3 display.

## Reverse-engineering and parity status

`MARCD.EXE` retains Borland/Turbo Debug information. Its symbol and initialized
data tables were used to recover lesson/goal transitions, all 18 reward-MVE
mappings, AV cue arrays, callback addresses, and the original movie coordinates.
The runtime stores those recovered constants as native data and never interprets
or executes DOS code.

Recovered behavior now includes the exact NEXT thresholds (`<10` outdoor,
`10..29` underwater, `>=30` castle), lesson reset on a transition into water or
castle, Practice corpus thresholds and row checkpoints, 18.2 Hz timing,
accuracy, the original integer net-WPM expression, HUD formatting, certificate
boundaries, timed punctuation bonuses, SFX slots, and the complete outdoor
target transition: one-tick initial scroll, animated cursor, six-tick hit hold,
turtle hit frame, and four-piece brick burst. `CASTLE.BGS` is decoded as four
length-prefixed LZSS rooms; the native transition reproduces the DOS 40-frame,
eight-pixel scroll. Character, WPM, lesson-time, report-card,
certificate, M.I.R.T. speech, persisted audio-toggle, demo, mouse, and keyboard routes
are covered by the headless state-transition suite. The shared DOS hand service
has also been recovered: its complete 95-character ASCII table, shifted-symbol
aliases, ten sprite offsets, right-thumb Space rule, and activity-specific hand
coordinates now drive the original `FINGER.GSO` overlays in every typing mode.
The underwater route includes the original ten-slot ambient-bubble pool,
Mario-mouth emission/SFX, plotted-coordinate creature switch, 21-tick failure
gate, catch, and sink completion. The student character selector now plots the
original `GREET2.GSO` `prefbox_1` sprite at its recovered coordinates. Outdoor
coin-world playback begins from its dedicated `OUTSKY.CRX` framebuffer and
palette before applying the character-specific `*OUT.BAN`/`*OUT.ANI` streams;
water and castle retain their current world framebuffers, matching their three
separate executable entry routines.

The Castle transition audit also recovered five inclusive `_JaySetClipRect`
windows from `MARCD.EXE`: two left-pipe entrances, two right-pipe entrances,
and the vertical descent below the upper pipe lip. Native sprite plotting
accepts the equivalent exclusive Windows rectangle and applies it to every
character variant during those exact interlude frames.

The extended display modes are explicitly non-original presentation options.
Widescreen adds 107 logical pixels and 13 text columns; ultra-widescreen adds
240 pixels and 30 columns. Outside stages future targets in that additional
space, Water and Castle expose longer prompts, and Practice uses a seamless page
wrapping at 45 or 62 columns instead of 32. The main menu independently places
its AV movie, three headers, popups, lesson-card rail, and matching hitboxes
across the selected canvas. Other fixed panels remain unstretched over a tiled
copy of the original Mario wallpaper; title/ending stages use clean symmetric
extensions. Bonus animations, AV speech, and MVE rewards retain the selected
framebuffer as well. The original outdoor speed formula remains capped to its
320-pixel input range. The mutually exclusive setting is global, persistent,
and backward-compatible with the earlier widescreen progress JSON field.

Startup music is likewise taken directly from the executable rather than
inferred from the asset names: `_main` pushes `1` and calls
`_music_playsong` immediately before `_MAINMENU`, which then runs the greeting.
The native port starts decoded `RTYP1.XMI` at the same boundary and preserves
it through the title and menu screens until a gameplay/movie route replaces it.

Remaining 1:1 work is tracked as evidence, not substituted functionality:
M.I.R.T. and general audiovisual fade/overlap comparisons.
Those comparisons do not require an emulator in the shipped application, a
mounted image, or a CD prompt. See `FUNCTION-AUDIT.md` for the current
subsystem-by-subsystem status.
