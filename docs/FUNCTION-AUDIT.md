# Native function audit

This audit distinguishes **implemented and exercised** behavior from **proven
1:1** behavior. Passing a route test means the control changes the expected
native state and renders without depending on DOSBox, `MARCD.EXE`, a mounted
image, or loose CD files. It does not by itself prove frame-identical timing.

| Area | Implemented and exercised | Remaining 1:1 evidence |
|---|---|---|
| Title/main menu | Disassembly-confirmed startup `_music_playsong(1)` continuing across the greeting/menu; original Interplay hold, 20-step 7/8 Mario logo zoom, move to the header, 13-letter `TEACHES TYPING` reveal and final hold; File, Help, Student, Lesson Times, Certificates, Report Card, M.I.R.T., Rap, persisted Music/SFX/Speech toggles, four world cards, NEXT, demo world selection, keyboard and mouse routes | Frame-level idle/demo audiovisual transition comparison and title fade/SFX overlap |
| Display modes | Persistent, mutually exclusive File-menu/F10/F9 choices; application-wide 427×200 at 16:9 or 560×200 at 21:9; extra active playfield in moving worlds; responsive main-menu headers/movie/lesson rail; seamless 45/62-column Practice sheet; unstretched fixed panels over tiled original wallpaper; translated mouse hitboxes and nearest-neighbor scaling | Broader DPI/GPU/display compatibility matrix |
| Student settings | New/load profile, editable name, WPM digits/backspace/arrows/wheel/Page keys, character selection with original `GREET2` 40x50 `prefbox_1` at x 66/132/208 and y 130, persisted world/lesson/character/times, Done/Cancel | Pixel-level comparison of every overlay state |
| Lesson times | Independent 1–60 minute Outside/Water/Castle/Practice values by digits, arrows, wheel, and Page keys | Repeat-rate behavior under held keys |
| NEXT/progression | Original WPM thresholds; Practice remains Practice; transition into Water/Castle resets lesson to 1 | Edge cases tied to original internal completion flags |
| Lesson selection | Home, Top, Bottom, Numbers, Symbols, All Keyboard; original sentence-file rules; randomized All Keyboard source; token rotation and continuous wrapping | Statistical comparison of original random selection sequence |
| Typing input | Expected-key matching, errors, problem-key counts, Space display, timer begins with gameplay, timer ends the activity, prompt wraps | DOS key-repeat and unusual keyboard-layout behavior |
| Scoring | Original 18.2 Hz elapsed-time conversion; integer net-WPM `max(0, correct-errors) * 182 / ticks`; exact Typed/WPM/Errors HUD strings and coordinates; attempts, errors, WPM and problem keys persisted/reported | Side-by-side verification at timer rollover boundaries |
| Finger guidance | All twelve original `FINGER.GSO` sprites; recovered 95-character ASCII lookup; uppercase/symbol aliases; exact ten per-finger offsets; right-thumb Space cue; original Outside/Castle, Underwater, and Practice base-hand coordinates | Side-by-side confirmation for non-US keyboard-layout input |
| Outdoor | Original background, character variants, moving blocks/Koopas/fingers, dynamic expected-key finger cue, target queue, exact chase/jump tables, one-tick initial scroll, 12-tick cursor cycle, six-tick hit hold, turtle hit frame, four-piece ballistic brick burst, timed bonus brick with recovered FONT7 `(6,3)` / `{0,15,31}` composition and layer order, `OUTSKY.CRX`-based coin BAN/ANI animation, HUD, results and progression; extended modes stage extra upcoming targets beyond x=319 while preserving the original speed ceiling | Exhaustive side-by-side capture across randomized target orders |
| Underwater | Original background, character variants, six swim frames, dynamic expected-key finger cue, exact initial biter/octopus transition, plotted-coordinate chase/catch, WPM-driven depth, 21-tick failure gate and sink path, ten-slot ambient bubble pool with 2×2-to-3×3 physics and Mario SFX, timed bonus bubble and coin animation, HUD, results and progression; 16:9 exposes 13 and 21:9 exposes 30 additional prompt columns | Exhaustive side-by-side capture across randomized ambient-bubble emissions |
| Castle | Four decoded `CASTLE.BGS` rooms, exact 40-step 8-pixel scroll composition, dynamic expected-key finger cue, rock-face tables, both construction sequences with recovered four-tick flashes and randomized 4–9-tick dwell intervals, all five original inclusive character clip windows at the left/right/vertical pipe mouths, lava/sinking scene, character variants, both timed bonus paths, HUD, results and progression; 16:9 exposes 13 and 21:9 exposes 30 additional prompt columns | Final frame-level side-by-side timing |
| Practice | Goal-dependent original corpus; exact 32-column classic wrapping plus 45-column 16:9 and 62-column 21:9 paper layouts; dynamic expected-key finger cue, explicit right-thumb Space/Enter row checkpoint, 15-tick reaction faces, continuous timing/scoring/results | DOS held-key repeat comparison |
| Audio | Persisted independent Music/SFX/Speech toggles, immediate world-music resume, native XMIDI/RLD/PCM/AV/MVE/CD-track paths, recovered SFX remap and AV predicates | Side-by-side cue fade/overlap timing on real MIDI devices |
| Movies/rewards | Seven MVE files decode natively; recovered reward mappings and coordinates; final newly-earned Castle certificate routes through the original 40-frame `ENDF.CRX`/`ENDF.BAN` winner animation | Eligibility/ordering edge cases and exact transition timing |
| Blackboard/results | Original `BLACKBRD.CRX`, exact FONT5 labels and text-grid positions, original-width keys/errors/WPM/goal/accuracy/time values, spoken debrief clips composited on the populated board, native reward transitions, and Next/Menu keyboard and mouse routes | Side-by-side fade and speech-overlap timing |
| Reports | Original single-page `RCARD.CRX` report for the active student; level, goal, optional last WPM/accuracy, exact five-key problem ranking, and original any-key/any-click dismissal | Page typography/spacing against every original report variant |
| Certificates | Recovered Outside `>=9`, Water `>=26`, and Castle `>=40` award boundaries; persistent per-student bits; mouse/keyboard selection and native print request | Printer typography comparison across Windows drivers |
| M.I.R.T. | Independent persisted speech-response toggle through File-menu and direct `I` routes, matching the original main-menu dispatch | Audiovisual fade/overlap comparison |
| Persistence | Multiple named students; WPM, lesson, character, world, four lesson times, certificates, results, problem keys, global audio toggles, and global display mode; backward-compatible JSON | Optional migration of external original DOS student disks |
| Embedded media | Byte-validated CHD/VHD and 288 imported files compiled into the single executable; runtime needs no source share | Future legitimate-asset ownership gate requested for release |
| Packaging | Self-contained x64 single-file executable; rejects external DOSBox/CHD/VHD/`MARCD.EXE`; headless resource/state/render tests | Broader Windows-version and device compatibility matrix |

## Audit criteria

- Each exposed menu command must have a keyboard route; controls shown as
  clickable also need a mouse route.
- Editable numeric controls must accept direct entry and incremental controls,
  clamp to their documented range, and persist per student.
- Every activity must accept typing, advance or wrap its prompt, update errors
  and problem keys, time out, save a result, and reach its results/menu route.
- Every toggle must alter its own subsystem without implicitly toggling another.
- Every packaged runtime asset must come from embedded resources; the CHD/VHD
  are archival embedded data and are never mounted or interpreted at runtime.

The automated suite enforces these functional criteria. Items in the final
column remain open until supported by disassembly-derived behavior or a
side-by-side capture of the same original game state.
