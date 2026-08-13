using System.Text.Json;
using MarioTypingWindows.Original;

namespace MarioTypingWindows;

internal static class NativeSelfTest
{
    public static int Run(string reportPath)
    {
        var checks = new List<string>();
        try
        {
            var resourceNames = LessonLibrary.GetResourceNames();
            if (resourceNames.Count < 20)
            {
                throw new InvalidDataException($"Expected at least 20 embedded lesson resources, found {resourceNames.Count}.");
            }
            checks.Add($"Embedded lesson resources: {resourceNames.Count}");

            foreach (var category in LessonLibrary.Categories)
            {
                foreach (var mode in Enum.GetValues<ActivityMode>())
                {
                    var prompt = LessonLibrary.CreatePrompt(category.Category, mode, 0);
                    if (prompt.Length < 10)
                    {
                        throw new InvalidDataException($"Prompt is unexpectedly short: {category.Title}/{mode}");
                    }
                }
            }
            checks.Add("All 20 category/activity combinations produced native lesson prompts.");

            for (var selection = 0; selection < 5; selection++)
            {
                if (NativeGameEngine.LessonCategoryForSelection(selection) != (LessonCategory)selection)
                {
                    throw new InvalidDataException($"Lesson selection {selection + 1} mapped to the wrong corpus.");
                }
            }
            for (var choice = 0; choice < 5; choice++)
            {
                if (NativeGameEngine.LessonCategoryForSelection(5, choice) != (LessonCategory)choice)
                {
                    throw new InvalidDataException("All Keyboard did not cover all five original corpora.");
                }
            }
            if (NativeGameEngine.PromptCharacterAt("abc", 3) != 'a' ||
                NativeGameEngine.PromptCharacterAt("abc", 7) != 'b')
            {
                throw new InvalidDataException("Original wrapping lesson stream behavior failed.");
            }
            checks.Add("All six executable lesson mappings and the wrapping randomized token stream validated.");

            if (NativeGameEngine.CalculateOriginalWordsPerMinute(60, 0, 60) != 10 ||
                NativeGameEngine.CalculateOriginalWordsPerMinute(60, 6, 60) != 9 ||
                NativeGameEngine.CalculateOriginalWordsPerMinute(4, 8, 60) != 0 ||
                NativeGameEngine.OriginalElapsedTicks(60_000) != 1092 ||
                NativeGameEngine.OriginalElapsedSeconds(1092) != 60 ||
                NativeGameEngine.CalculateOriginalWordsPerMinuteFromTicks(60, 0, 1092) != 10 ||
                NativeGameEngine.CalculateOriginalWordsPerMinuteFromTicks(60, 6, 1092) != 9 ||
                NativeGameEngine.CalculateOriginalWordsPerMinuteFromTicks(4, 8, 1092) != 0)
            {
                throw new InvalidDataException("Original net-character WPM formula failed.");
            }
            checks.Add("Original 18.2 Hz elapsed-time and integer net-character WPM rules validated.");

            var expectedOutsideJump = new[] { 0, -19, -26, -28, -28, -25, -20, -2, 0, 0, 0, 0 };
            var expectedOutsideCursor = new[] { 0, 0, 1, 1, 2, 2, 3, 3, 2, 2, 1, 1 };
            if (!Enumerable.Range(0, expectedOutsideJump.Length)
                    .Select(NativeGameEngine.OriginalOutsideJumpYOffset)
                    .SequenceEqual(expectedOutsideJump) ||
                !Enumerable.Range(0, expectedOutsideCursor.Length)
                    .Select(NativeGameEngine.OriginalOutsideCursorFrame)
                    .SequenceEqual(expectedOutsideCursor) ||
                NativeGameEngine.OriginalOutsideForwardSpeed(293) != 9 ||
                NativeGameEngine.OriginalOutsideForwardSpeed(160) != 5 ||
                NativeGameEngine.OriginalOutsideForwardSpeed(-1) != 9 ||
                NativeGameEngine.OriginalOutsideHorizontalDelta(20, 273) != 8 ||
                NativeGameEngine.OriginalOutsideHorizontalDelta(300, 100) != -8 ||
                NativeGameEngine.OriginalOutsideHorizontalDelta(100, 100) != 0)
            {
                throw new InvalidDataException("Original outdoor movement and jump tables failed.");
            }
            checks.Add("Original 18.2 Hz outdoor scroll, chase, 12-frame jump, and cursor animation rules validated.");

            if (NativeGameEngine.OriginalHandForCharacter('a') !=
                    new OriginalHandElement(1, false, 0, 9) ||
                NativeGameEngine.OriginalHandForCharacter('f') !=
                    new OriginalHandElement(4, false, 20, 6) ||
                NativeGameEngine.OriginalHandForCharacter('j') !=
                    new OriginalHandElement(9, true, 4, 6) ||
                NativeGameEngine.OriginalHandForCharacter(';') !=
                    new OriginalHandElement(6, true, 29, 9) ||
                NativeGameEngine.OriginalHandForCharacter(' ') !=
                    new OriginalHandElement(10, true, 0, 19) ||
                NativeGameEngine.OriginalHandForCharacter('A') !=
                    NativeGameEngine.OriginalHandForCharacter('a') ||
                NativeGameEngine.OriginalHandForCharacter('\n') is not null)
            {
                throw new InvalidDataException("Original ASCII-to-finger guidance table failed.");
            }
            checks.Add("Original 95-character key-to-finger table, hand selection, and overlay offsets validated.");

            if (NativeGameEngine.OriginalWaterTargetY(0, 10) != 157 ||
                NativeGameEngine.OriginalWaterTargetY(5, 10) != 126 ||
                NativeGameEngine.OriginalWaterTargetY(10, 10) != 94 ||
                NativeGameEngine.OriginalWaterTargetY(20, 20) != 94 ||
                NativeGameEngine.WaterCreatureX(20, 112) != -52 ||
                NativeGameEngine.WaterCreatureX(0, 80) != 280)
            {
                throw new InvalidDataException("Original water depth or trailing-creature rules failed.");
            }
            var waterBubble = (X: 44, Y: 170, VerticalVelocity: 0, Large: false);
            for (var tick = 0; tick < 10; tick++)
            {
                waterBubble = NativeGameEngine.OriginalWaterBubbleStep(
                    waterBubble.X,
                    waterBubble.Y,
                    0,
                    waterBubble.VerticalVelocity);
            }
            if (waterBubble != (44, 158, -10, false))
            {
                throw new InvalidDataException("Original small-bubble rise path failed before its size transition.");
            }
            waterBubble = NativeGameEngine.OriginalWaterBubbleStep(
                waterBubble.X,
                waterBubble.Y,
                0,
                waterBubble.VerticalVelocity);
            if (waterBubble != (44, 156, -11, true))
            {
                throw new InvalidDataException("Original water bubble did not grow on its eleventh tick.");
            }
            checks.Add("Original water WPM depth, trailing-creature wrap, and 2x2-to-3x3 bubble physics validated.");

            if (NativeGameEngine.OriginalCastleBatchEnd("One. Two! Three? Four.", 0) != 16 ||
                NativeGameEngine.OriginalCastleTargetX(0, 100) != 67 ||
                NativeGameEngine.OriginalCastleTargetX(50, 100) != 147 ||
                NativeGameEngine.OriginalCastleTargetX(100, 100) != 227 ||
                NativeGameEngine.OriginalCastleDesiredVelocity(0, 67) != 5 ||
                NativeGameEngine.OriginalCastleDesiredVelocity(100, 67) != -5 ||
                NativeGameEngine.OriginalCastleDesiredVelocity(66, 67) != 1 ||
                NativeGameEngine.OriginalCastleDesiredVelocity(67, 67) != 0 ||
                NativeGameEngine.OriginalCastleNextVelocity(0, 5) != 1 ||
                NativeGameEngine.OriginalCastleNextVelocity(3, -5) != 2 ||
                NativeGameEngine.OriginalCastleNextVelocity(-3, 5) != -2)
            {
                throw new InvalidDataException("Original castle batching, target, or eased movement rules failed.");
            }
            checks.Add("Original castle three-sentence batches and eased progress chase validated.");

            var practiceWrapped = NativeGameEngine.OriginalPracticeLine(
                "one two three four five six seven eight nine", 0);
            if (practiceWrapped.Text.Length > 32 ||
                practiceWrapped.Text != "one two three four five six" ||
                practiceWrapped.NextOffset != 28 ||
                NativeGameEngine.OriginalPracticeFace(16, 50, 30) != 1 ||
                NativeGameEngine.OriginalPracticeFace(15, 31, 30) != 3 ||
                NativeGameEngine.OriginalPracticeFace(15, 30, 30) != 2 ||
                NativeGameEngine.OriginalPracticeFace(15, 31, 30, hasStudent: false) != 2)
            {
                throw new InvalidDataException("Original practice wrapping or reaction-face rules failed.");
            }
            checks.Add("Original Practice 32-column word wrapping and 15-tick reaction-face rules validated.");

            if (NativeGameEngine.OriginalBonusDelaySeconds(120, 0) != 15 ||
                NativeGameEngine.OriginalBonusDelaySeconds(120, 0x7fff) != 104 ||
                NativeGameEngine.OriginalBonusCharacter(0) != '!' ||
                NativeGameEngine.OriginalBonusCharacter(0x7fff) != '/')
            {
                throw new InvalidDataException("Original bonus delay or punctuation range failed.");
            }
            checks.Add("Original timed bonus punctuation ranges validated.");

            if (NativeGameEngine.OriginalSoundEffectSlot(1) != 1 ||
                NativeGameEngine.OriginalSoundEffectSlot(6) != 6 ||
                NativeGameEngine.OriginalSoundEffectSlot(11) != 10)
            {
                throw new InvalidDataException("Original SFX slot remapping failed.");
            }
            checks.Add("Original correct/error/rock/build and 11-to-10 SFX slot mapping validated.");

            var outsideHud = NativeGameEngine.BuildOriginalHudState(
                OriginalScreen.Outside, 7, 4, 12, 18);
            var waterHud = NativeGameEngine.BuildOriginalHudState(
                OriginalScreen.Water, 7, 4, 12, 18);
            var practiceHud = NativeGameEngine.BuildOriginalHudState(
                OriginalScreen.Practice, 7, 4, 12, 18);
            if (outsideHud.TimeText != "  0:00:18" ||
                outsideHud.TypedText != "Typed: 11" ||
                outsideHud.WordsPerMinuteText != "WPM:  12" ||
                outsideHud.ErrorsText != "Errors:  4" ||
                outsideHud.WordsPerMinuteX != 256 || outsideHud.ErrorsX != 240 ||
                outsideHud.FirstRow != 168 || outsideHud.SecondRow != 184 ||
                waterHud.TypedText != "Typed: 11 " ||
                waterHud.WordsPerMinuteText != "   WPM:  12" ||
                waterHud.ErrorsText != " Errors:  4" ||
                waterHud.WordsPerMinuteX != 232 || waterHud.ErrorsX != 232 ||
                waterHud.FirstRow != 8 || waterHud.SecondRow != 24 ||
                practiceHud.FirstRow != 8 || practiceHud.SecondRow != 24)
            {
                throw new InvalidDataException("Original four-world HUD formats or coordinates failed.");
            }
            checks.Add("Original Typed/WPM/Errors HUD formats and one-based text-grid coordinates validated.");

            var (cdBytes, installBytes) = EmbeddedAssets.GetEmbeddedMediaLengths();
            if (cdBytes != EmbeddedAssets.ExpectedCdLength || installBytes != EmbeddedAssets.ExpectedInstallLength)
            {
                throw new InvalidDataException(
                    $"Embedded media length mismatch: CHD={cdBytes}, VHD={installBytes}.");
            }
            checks.Add($"Complete embedded CD image: {cdBytes} bytes");
            checks.Add($"Complete embedded installed image: {installBytes} bytes");

            var (cdHash, installHash) = EmbeddedAssets.GetEmbeddedMediaHashes();
            if (!cdHash.Equals(EmbeddedAssets.ExpectedCdSha256, StringComparison.OrdinalIgnoreCase) ||
                !installHash.Equals(EmbeddedAssets.ExpectedInstallSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("An embedded source image failed its SHA-256 integrity check.");
            }
            checks.Add($"Embedded CD SHA-256: {cdHash}");
            checks.Add($"Embedded install SHA-256: {installHash}");

            var originalAssets = new OriginalAssetStore();
            if (originalAssets.Paths.Count != 288)
            {
                throw new InvalidDataException(
                    $"Expected 288 extracted original game files, found {originalAssets.Paths.Count}.");
            }
            checks.Add($"Complete extracted native asset set: {originalAssets.Paths.Count} files");

            var mainMenu = CrxImage.Decode(originalAssets.ReadAllBytes("MAINMENU.CRX"));
            var mainMenuHash = OriginalGraphicChecks.PixelHash(mainMenu);
            if (!mainMenuHash.Equals(OriginalGraphicChecks.MainMenuPixelsSha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Original MAINMENU.CRX pixel hash mismatch: {mainMenuHash}");
            }
            checks.Add($"Original main-menu framebuffer decoded: {mainMenu.Pixels.Length} pixels");

            var testFontBank = OriginalSpriteBank.DecodeGso(
                originalAssets.ReadAllBytes("FONT5I.GSO"),
                originalAssets.ReadAllText("FONT5I.GSK"));
            var testFont = new OriginalFont(testFontBank, "font5_", 8, 8);
            using (var textRaster = new NativeRaster(32, 24))
            {
                testFont.Draw(
                    textRaster,
                    mainMenu.Palette,
                    "A\nB",
                    0,
                    0,
                    maximumCharacters: 1,
                    foregroundIndex: mainMenu.Palette.FindNearest(255, 255, 255));
                using var textBitmap = textRaster.Snapshot();
                var secondLineVisible = false;
                for (var y = 8; y < 16 && !secondLineVisible; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        if (textBitmap.GetPixel(x, y).ToArgb() != 0)
                        {
                            secondLineVisible = true;
                            break;
                        }
                    }
                }
                if (!secondLineVisible)
                {
                    throw new InvalidDataException("Multi-line original-font rendering truncated after the first line.");
                }
            }
            checks.Add("Multi-line original-font controls render every line within the per-line width limit.");

            foreach (var fileName in new[] { "OUTSIDE.CRX", "WATER.CRX", "CASTLE.CRX", "PRACTICE.CRX" })
            {
                var scene = CrxImage.Decode(originalAssets.ReadAllBytes(fileName));
                if (scene.Pixels.Length != 320 * 200)
                {
                    throw new InvalidDataException($"Scene did not decode to 320x200: {fileName}");
                }
            }
            checks.Add("Original outdoor, underwater, castle, and practice screens decoded to 320x200.");

            var walking = OriginalSpriteBank.DecodeGso(
                originalAssets.ReadAllBytes("WALKING.GSO"),
                originalAssets.ReadAllText("WALKING.GSK"));
            var marioRun = walking["mrun0001_1"];
            if (walking.Names.Count != 90 || marioRun.Width != 40 || marioRun.Height != 50)
            {
                throw new InvalidDataException("Original walking sprite bank failed structural validation.");
            }
            checks.Add("Original Mario/Luigi/Peach run, jump, and sink bank decoded: 90 frames.");

            var outsideObjects = OriginalSpriteBank.DecodeGso(
                originalAssets.ReadAllBytes("OUTSIDE.GSO"),
                originalAssets.ReadAllText("OUTSIDE.GSK"));
            if (!outsideObjects.Names.Contains("turt1_1") ||
                !outsideObjects.Names.Contains("brick1_1") ||
                !outsideObjects.Names.Contains("cursor_1") ||
                outsideObjects["brick1_1"] is not { Width: 20, Height: 16 })
            {
                throw new InvalidDataException("Outdoor Koopa/brick/cursor sprites failed structural validation.");
            }
            checks.Add("Original outdoor Koopa, 20x16 brick, and cursor sprites decoded.");

            var idle = OriginalSpriteBank.DecodeGco(
                originalAssets.ReadAllBytes("IDLE.GCO"),
                originalAssets.ReadAllText("IDLE.GCK"));
            if (idle.Names.Count < 140 || idle["mstd0001_1"].PixelCount != 25 * 42)
            {
                throw new InvalidDataException("Compressed original idle bank failed validation.");
            }
            checks.Add($"Original compressed idle/reaction bank decoded: {idle.Names.Count} frames.");

            var greetingObjects = OriginalSpriteBank.DecodeGso(
                originalAssets.ReadAllBytes("GREET.GSO"),
                originalAssets.ReadAllText("GREET.GSK"));
            var greetingZoom = NativeGameEngine.CreateGreetingZoomFrames(greetingObjects["lmario_1"]);
            var greetingDelayTicks = NativeGameEngine.CreateGreetingLetterDelayTicksForTest(
                0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0);
            using var greetingTimelineEngine = new NativeGameEngine(
                enableAudio: false,
                enableCinematics: false,
                progressFilePath: Path.Combine(
                    Path.GetTempPath(), $"mtt-native-greeting-{Guid.NewGuid():N}.json"));
            var fadeStart = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(0, greetingDelayTicks);
            var fadeFirstIntermediate = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(58, greetingDelayTicks);
            var firstLetter = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(6_810, greetingDelayTicks);
            var seventhLetter = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(8_515, greetingDelayTicks);
            var wordBreakHold = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(9_000, greetingDelayTicks);
            var eighthLetter = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(9_505, greetingDelayTicks);
            var allLetters = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(11_100, greetingDelayTicks);
            var finalHold = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(11_320, greetingDelayTicks);
            var completeTitle = greetingTimelineEngine.GreetingTimelineAtMillisecondsForTest(18_120, greetingDelayTicks);
            var greetingDiagnostic =
                $"delays={string.Join(',', greetingDelayTicks)} " +
                $"startRgb={Color.FromArgb(fadeStart.Palette.Argb[0])} " +
                $"fadeChanged={!fadeStart.Palette.Argb.SequenceEqual(fadeFirstIntermediate.Palette.Argb)} " +
                $"first={firstLetter.Stage}/{firstLetter.LetterCount} " +
                $"seventh={seventhLetter.LetterCount} break={wordBreakHold.LetterCount} " +
                $"eighth={eighthLetter.LetterCount} all={allLetters.LetterCount} " +
                $"final={finalHold.Complete} complete={completeTitle.Complete}";
            if (greetingObjects["smario_1"] is not { Width: 119, Height: 48 } ||
                greetingZoom.Length != 20 ||
                (greetingZoom[0].Width, greetingZoom[0].Height) != (308, 120) ||
                (greetingZoom[19].Width, greetingZoom[19].Height) != (21, 6) ||
                !greetingDelayTicks.SequenceEqual([4, 5, 6, 7, 4, 5, 6, 7, 4, 5, 6, 7, 4]) ||
                Color.FromArgb(fadeStart.Palette.Argb[0]) is not { R: 12, G: 12, B: 12 } ||
                fadeStart.Palette.Argb.SequenceEqual(fadeFirstIntermediate.Palette.Argb) ||
                firstLetter is not { Stage: GreetingStage.Letters, LetterCount: 1 } ||
                seventhLetter.LetterCount != 7 ||
                wordBreakHold.LetterCount != 7 ||
                eighthLetter.LetterCount != 8 ||
                allLetters.LetterCount != 13 ||
                finalHold.Complete ||
                !completeTitle.Complete)
            {
                throw new InvalidDataException(
                    $"Original logo zoom or TEACHES TYPING reveal tables failed: {greetingDiagnostic}.");
            }
            greetingTimelineEngine.AdvanceTitleAnimationMillisecondsForTest(
                greetingTimelineEngine.TitleAllLettersVisibleMilliseconds);
            if (greetingTimelineEngine.TitleSoundEffectCount != 13)
            {
                throw new InvalidDataException(
                    $"Title reveal scheduled {greetingTimelineEngine.TitleSoundEffectCount} keyed SFX instead of 13.");
            }
            checks.Add("Original palette fades, 20/19/6-frame logo motion, randomized 4-7 tick keyed title reveal, word-break pause, and final hold validated.");

            var castleBase = CrxImage.Decode(originalAssets.ReadAllBytes("CASTLE.CRX"));
            var castleBackgrounds = CastleBackgroundDecoder.Decode(
                originalAssets.ReadAllBytes("CASTLE.BGS"),
                castleBase.Pixels);
            var expectedCastleBackgroundHashes = new[]
            {
                "88A8C7D7FC4CF87D3F541D2392E8F53EA48C4BBDEA56D76C99DCCC4D7407F0A5",
                "9F45BA040B171957B69EA59129E58ACCB280EEC0E1A8526460C53BF8C50DD605",
                "BE4825C68AFD1975E4129E5798B3FEC256F2A49DC6676F8673B5209206812BD4",
                "4AF4D9C1070DAA61FA513E15004FD63E437FE2108FEDB104E860BBB732B7229B",
            };
            if (castleBackgrounds.Count != CastleBackgroundDecoder.SceneCount)
            {
                throw new InvalidDataException(
                    $"Expected four CASTLE.BGS scenes, found {castleBackgrounds.Count}.");
            }
            for (var scene = 0; scene < castleBackgrounds.Count; scene++)
            {
                var sceneRegion = castleBackgrounds[scene].AsSpan(
                    CastleBackgroundDecoder.SceneTop * CrxImage.Width,
                    CastleBackgroundDecoder.ScenePixelCount);
                var hash = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(sceneRegion));
                if (!hash.Equals(expectedCastleBackgroundHashes[scene], StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"CASTLE.BGS scene {scene} pixel hash mismatch: {hash}.");
                }
            }
            var firstCastleScroll = CastleBackgroundDecoder.ComposeScrollFrame(
                castleBackgrounds[0], castleBackgrounds[1], 1);
            var completedCastleScroll = CastleBackgroundDecoder.ComposeScrollFrame(
                castleBackgrounds[0], castleBackgrounds[1], 40);
            var castleRow = CastleBackgroundDecoder.SceneTop * CrxImage.Width;
            if (!firstCastleScroll.AsSpan(castleRow, 312)
                    .SequenceEqual(castleBackgrounds[0].AsSpan(castleRow + 8, 312)) ||
                !firstCastleScroll.AsSpan(castleRow + 312, 8)
                    .SequenceEqual(castleBackgrounds[1].AsSpan(castleRow, 8)) ||
                !completedCastleScroll.SequenceEqual(castleBackgrounds[1]))
            {
                throw new InvalidDataException("Original 8-pixel castle scroll composition failed.");
            }
            checks.Add("All four CASTLE.BGS scenes and exact 40-step horizontal scroll composition validated.");

            var endingBase = CrxImage.Decode(originalAssets.ReadAllBytes("ENDF.CRX"));
            var endingFrames = BackAnimationDecoder.DecodeBan(
                originalAssets.ReadAllBytes("ENDF.BAN"),
                endingBase.Pixels);
            if (endingFrames.Count != 40 || endingFrames[0].SequenceEqual(endingFrames[^1]))
            {
                throw new InvalidDataException(
                    $"Original castle certificate ending failed BAN decoding: {endingFrames.Count} frames.");
            }
            checks.Add("Original 40-frame castle certificate ending animation decoded from ENDF.CRX/ENDF.BAN.");

            var castleBuild = BackAnimationDecoder.DecodeBan(
                originalAssets.ReadAllBytes("WCAS.BAN"),
                castleBase.Pixels);
            if (castleBuild.Count != 48)
            {
                throw new InvalidDataException($"Expected 48 timed castle construction frames, found {castleBuild.Count}.");
            }
            var bonusAnimationCounts = new List<string>();
            foreach (var prefix in new[] { "M", "L", "P" })
            {
                foreach (var suffix in new[] { "OUT", "SWM", "CAS" })
                {
                    var baseImage = suffix switch
                    {
                        "SWM" => CrxImage.Decode(originalAssets.ReadAllBytes("WATER.CRX")),
                        "CAS" => castleBase,
                        _ => CrxImage.Decode(originalAssets.ReadAllBytes("OUTSKY.CRX")),
                    };
                    var banFrames = BackAnimationDecoder.DecodeBan(
                        originalAssets.ReadAllBytes($"{prefix}{suffix}.BAN"),
                        baseImage.Pixels);
                    var aniFrames = BackAnimationDecoder.DecodeAni(
                        originalAssets.ReadAllBytes($"{prefix}{suffix}.ANI"));
                    if (banFrames.Count == 0 || aniFrames.Count == 0 ||
                        Math.Abs(banFrames.Count - aniFrames.Count) > 1)
                    {
                        throw new InvalidDataException(
                            $"{prefix}{suffix} BAN/ANI synchronization failed: {banFrames.Count}/{aniFrames.Count} frames.");
                    }
                    bonusAnimationCounts.Add($"{prefix}{suffix}={Math.Max(banFrames.Count, aniFrames.Count)}");
                }
            }
            checks.Add(
                $"Original timed BAN/ANI background and character animation codecs validated ({string.Join(", ", bonusAnimationCounts)})." );

            var welcomeMovie = AvDecoder.Decode(originalAssets.ReadAllBytes("MOVIES/Z8.AV"));
            if (welcomeMovie.Frames.Count != 195 ||
                welcomeMovie.Frames.Any(frame => frame.Length != OriginalAvMovie.Width * OriginalAvMovie.Height) ||
                welcomeMovie.UnsignedPcmAudio.Length != (14 + 195) * 732)
            {
                throw new InvalidDataException("Original Z8 welcome movie failed AV structural validation.");
            }
            checks.Add("Original enhanced-CD Mario welcome movie decoded: 195 frames plus unsigned PCM audio.");

            var expectedAvCueCount = 3 + 24 + 13 + 13 + 15 + 4 + 10;
            var avCueCount = OriginalAvCues.MainMenuRandom.Length +
                             OriginalAvCues.MainMenuIdle.Length +
                             OriginalAvCues.OutsideWorld.Length +
                             OriginalAvCues.CastleWorld.Length +
                             OriginalAvCues.WaterWorld.Length +
                             OriginalAvCues.ReportFail.Length +
                             OriginalAvCues.WaterDebrief.Length;
            if (avCueCount != expectedAvCueCount ||
                OriginalAvCues.MainMenuRandom is not ["Z8", "Z9", "Z52"] ||
                OriginalAvCues.MainMenuIdle[0] != "Z10" ||
                OriginalAvCues.MainMenuIdle[^1] != "Z54" ||
                OriginalAvCues.OutsideWorld[0] != "Z73" ||
                OriginalAvCues.OutsideWorld[^1] != "C66" ||
                OriginalAvCues.CastleWorld[0] != "Z74" ||
                OriginalAvCues.CastleWorld[^1] != "Z72" ||
                OriginalAvCues.WaterWorld[0] != "Z75" ||
                OriginalAvCues.WaterWorld[^1] != "Z70" ||
                OriginalAvCues.ReportFail[^1] != "Z79" ||
                OriginalAvCues.WaterDebrief[^1] != "Z89")
            {
                throw new InvalidDataException("Original enhanced-CD AV cue-table mapping failed validation.");
            }
            foreach (var cue in OriginalAvCues.MainMenuRandom
                         .Concat(OriginalAvCues.MainMenuIdle)
                         .Concat(OriginalAvCues.OutsideWorld)
                         .Concat(OriginalAvCues.CastleWorld)
                         .Concat(OriginalAvCues.WaterWorld)
                         .Concat(OriginalAvCues.ReportFail)
                         .Concat(OriginalAvCues.WaterDebrief))
            {
                if (!originalAssets.Paths.Contains($"MOVIES/{cue}.AV", StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Original enhanced-CD cue is missing: {cue}.AV");
                }
            }
            checks.Add($"Original executable AV cue tables validated: {avCueCount} entries across 7 tables.");

            var outsideMarioCues = NativeGameEngine.EligibleWorldCues(
                OriginalScreen.Outside,
                OriginalCharacter.Mario);
            var waterMarioCues = NativeGameEngine.EligibleWorldCues(
                OriginalScreen.Water,
                OriginalCharacter.Mario);
            var castleLuigiCues = NativeGameEngine.EligibleWorldCues(
                OriginalScreen.Castle,
                OriginalCharacter.Luigi);
            var castleFailureCues = NativeGameEngine.EligibleDebriefCues(
                OriginalScreen.Castle,
                previousWpm: 20,
                currentWpm: 10);
            var outsideFailureCues = NativeGameEngine.EligibleDebriefCues(
                OriginalScreen.Outside,
                previousWpm: 20,
                currentWpm: 10);
            var smallImprovementCues = NativeGameEngine.EligibleDebriefCues(
                OriginalScreen.Water,
                previousWpm: 20,
                currentWpm: 29);
            var largeImprovementCues = NativeGameEngine.EligibleDebriefCues(
                OriginalScreen.Water,
                previousWpm: 20,
                currentWpm: 30);
            if (outsideMarioCues is null || outsideMarioCues.Count != 11 ||
                outsideMarioCues.Contains("C65") || outsideMarioCues.Contains("C66") ||
                waterMarioCues is null || waterMarioCues.Count != 13 ||
                waterMarioCues.Contains("M65") || waterMarioCues.Contains("M66") ||
                !waterMarioCues.Contains("Z68") ||
                castleLuigiCues is null || !castleLuigiCues.Contains("Z65") ||
                castleLuigiCues.Contains("Z66") ||
                castleFailureCues.Count != 3 || castleFailureCues.Contains("Z76") ||
                outsideFailureCues.Count != 4 || !outsideFailureCues.Contains("Z76") ||
                smallImprovementCues.Count != 10 ||
                largeImprovementCues.Count != 8 || largeImprovementCues.Contains("Z80") ||
                largeImprovementCues.Contains("Z83"))
            {
                throw new InvalidDataException("Original enhanced-CD AV callback predicates failed validation.");
            }
            checks.Add("Original executable AV callback predicates validated for character, world, and WPM state.");

            var lessonMovie = MveDecoder.Decode(originalAssets.ReadAllBytes("MOVIES/MARIO1.MVE"));
            if (lessonMovie.Width != 320 || lessonMovie.Height != 200 ||
                lessonMovie.Frames.Count != 469 || lessonMovie.AudioSampleRate != 22_050 ||
                lessonMovie.AudioBitsPerSample != 8 || lessonMovie.AudioChannels != 1 ||
                lessonMovie.PcmAudio.Length != 1_199_708)
            {
                throw new InvalidDataException(
                    $"Original MARIO1.MVE cutscene failed native structural decoding: " +
                    $"{lessonMovie.Width}x{lessonMovie.Height}, frames={lessonMovie.Frames.Count}, " +
                    $"audio={lessonMovie.AudioSampleRate}Hz/{lessonMovie.AudioBitsPerSample}bit/" +
                    $"{lessonMovie.AudioChannels}ch/{lessonMovie.PcmAudio.Length} bytes.");
            }
            checks.Add("Original 320x200 Interplay MVE cutscene decoded natively: 469 video/PCM frames.");

            var expectedRewardMovies = new[]
            {
                new[] { 1, 2, 2, 2, 2, 3 },
                new[] { 4, 4, 4, 4, 4, 5 },
                new[] { 6, 6, 6, 6, 6, 7 },
            };
            var rewardWorlds = new[]
            {
                OriginalScreen.Outside,
                OriginalScreen.Water,
                OriginalScreen.Castle,
            };
            for (var worldIndex = 0; worldIndex < rewardWorlds.Length; worldIndex++)
            {
                for (var lessonLevel = 0; lessonLevel < 6; lessonLevel++)
                {
                    var actual = NativeGameEngine.RewardMovieNumber(rewardWorlds[worldIndex], lessonLevel);
                    if (actual != expectedRewardMovies[worldIndex][lessonLevel])
                    {
                        throw new InvalidDataException(
                            $"Reward movie mapping mismatch for {rewardWorlds[worldIndex]} " +
                            $"lesson {lessonLevel + 1}: expected {expectedRewardMovies[worldIndex][lessonLevel]}, " +
                            $"found {actual}.");
                    }
                }
            }
            checks.Add("Original executable reward-movie jump tables validated for all 18 world/lesson pairs.");

            var progressionChecks = new[]
            {
                (OriginalScreen.Outside, 1, 5, 5, new LessonProgressOutcome(2, 5, true, false)),
                (OriginalScreen.Outside, 6, 5, 5, new LessonProgressOutcome(6, 7, false, false)),
                (OriginalScreen.Outside, 6, 5, 9, new LessonProgressOutcome(6, 10, false, true)),
                (OriginalScreen.Outside, 6, 10, 10, new LessonProgressOutcome(6, 10, true, true)),
                (OriginalScreen.Water, 5, 10, 10, new LessonProgressOutcome(6, 10, true, false)),
                (OriginalScreen.Water, 6, 10, 10, new LessonProgressOutcome(6, 15, false, false)),
                (OriginalScreen.Water, 6, 10, 26, new LessonProgressOutcome(6, 30, false, true)),
                (OriginalScreen.Water, 6, 30, 30, new LessonProgressOutcome(6, 30, true, true)),
                (OriginalScreen.Castle, 6, 30, 30, new LessonProgressOutcome(6, 35, true, false)),
                (OriginalScreen.Castle, 6, 40, 40, new LessonProgressOutcome(6, 45, true, true)),
                (OriginalScreen.Castle, 3, 30, 29, new LessonProgressOutcome(3, 30, false, false)),
            };
            foreach (var (world, lesson, goal, measured, expected) in progressionChecks)
            {
                var actual = NativeGameEngine.AdvanceLessonProgress(world, lesson, goal, measured);
                if (actual != expected)
                {
                    throw new InvalidDataException(
                        $"Progression mismatch for {world} lesson {lesson}, goal {goal}, " +
                        $"measured {measured}: expected {expected}, found {actual}.");
                }
            }
            checks.Add("Original executable lesson/goal progression validated at all three world boundaries.");

            var nextWorldChecks = new[]
            {
                (OriginalScreen.Castle, 5, OriginalScreen.Outside),
                (OriginalScreen.Outside, 9, OriginalScreen.Outside),
                (OriginalScreen.Outside, 10, OriginalScreen.Water),
                (OriginalScreen.Castle, 29, OriginalScreen.Water),
                (OriginalScreen.Water, 30, OriginalScreen.Castle),
                (OriginalScreen.Practice, 5, OriginalScreen.Practice),
                (OriginalScreen.Practice, 150, OriginalScreen.Practice),
            };
            foreach (var (current, goal, expected) in nextWorldChecks)
            {
                if (NativeGameEngine.WorldForNext(current, goal) != expected)
                {
                    throw new InvalidDataException(
                        $"NEXT mapping failed for current={current}, goal={goal}.");
                }
            }
            if (!NativeGameEngine.ShouldResetLessonForWorldChange(
                    OriginalScreen.Outside, OriginalScreen.Water) ||
                !NativeGameEngine.ShouldResetLessonForWorldChange(
                    OriginalScreen.Water, OriginalScreen.Castle) ||
                NativeGameEngine.ShouldResetLessonForWorldChange(
                    OriginalScreen.Water, OriginalScreen.Water) ||
                NativeGameEngine.ShouldResetLessonForWorldChange(
                    OriginalScreen.Practice, OriginalScreen.Practice))
            {
                throw new InvalidDataException("NEXT lesson-reset transition failed.");
            }
            checks.Add("Original NEXT thresholds validated, including Practice-mode retention.");

            if (NativeGameEngine.PracticeModeForGoal(9) != ActivityMode.LetterDrill ||
                NativeGameEngine.PracticeModeForGoal(10) != ActivityMode.WordChallenge ||
                NativeGameEngine.PracticeModeForGoal(29) != ActivityMode.WordChallenge ||
                NativeGameEngine.PracticeModeForGoal(30) != ActivityMode.SentenceChallenge)
            {
                throw new InvalidDataException("Practice corpus thresholds do not match the original initializer.");
            }
            checks.Add("Original Practice letter/word/sentence thresholds validated.");

            var profileTestPath = Path.Combine(
                Path.GetTempPath(),
                $"mtt-native-profile-{Guid.NewGuid():N}.json");
            try
            {
                var profileStore = ProgressStore.Load(profileTestPath);
                profileStore.StartNewStudent("Mario");
                profileStore.SetStudentInformation("Mario", 12, 3, OriginalCharacter.Mario);
                profileStore.SetCurrentWorld(OriginalScreen.Water);
                profileStore.SetLessonTimes([3, 6, 7, 11]);
                profileStore.AwardCertificate(OriginalScreen.Outside);
                profileStore.StartNewStudent("Luigi");
                profileStore.SetStudentInformation("Luigi", 30, 6, OriginalCharacter.Luigi);
                profileStore.SetCurrentWorld(OriginalScreen.Castle);
                if (!profileStore.LoadStudent("Mario") ||
                    profileStore.GoalWordsPerMinute != 12 ||
                    profileStore.LessonLevel != 3 ||
                    profileStore.Character != OriginalCharacter.Mario ||
                    profileStore.CurrentWorld != OriginalScreen.Water ||
                    !profileStore.HasCertificate(OriginalScreen.Outside) ||
                    !profileStore.LessonTimesMinutes.SequenceEqual([3, 6, 7, 11]))
                {
                    throw new InvalidDataException("In-memory student profile switching failed.");
                }

                var reloadedProfiles = ProgressStore.Load(profileTestPath);
                if (reloadedProfiles.StudentNames.Count < 3 ||
                    !reloadedProfiles.LoadStudent("Luigi") ||
                    reloadedProfiles.GoalWordsPerMinute != 30 ||
                    reloadedProfiles.LessonLevel != 6 ||
                    reloadedProfiles.Character != OriginalCharacter.Luigi ||
                    reloadedProfiles.CurrentWorld != OriginalScreen.Castle ||
                    reloadedProfiles.HasCertificate(OriginalScreen.Outside))
                {
                    throw new InvalidDataException("Persisted multi-student profile reload failed.");
                }
                checks.Add("Native multi-student New/Load/Edit progress and character persistence validated.");
            }
            finally
            {
                File.Delete(profileTestPath);
                File.Delete(profileTestPath + ".tmp");
            }

            var controlTestPath = Path.Combine(
                Path.GetTempPath(),
                $"mtt-native-controls-{Guid.NewGuid():N}.json");
            try
            {
                using (var controlEngine = new NativeGameEngine(
                           enableAudio: false,
                           enableCinematics: false,
                           progressFilePath: controlTestPath))
                {
                    controlEngine.HandleKeyDown(Keys.Space);
                    controlEngine.HandleClick(145, 4);
                    controlEngine.HandleClick(145, 30);
                    if (controlEngine.Screen != OriginalScreen.StudentInformation)
                    {
                        throw new InvalidDataException("Student Edit did not open Student Information.");
                    }

                    if (controlEngine.StudentInformationSelection != 5)
                    {
                        throw new InvalidDataException("Student Information did not begin on the original Done focus.");
                    }
                    controlEngine.HandleKeyDown(Keys.Tab);
                    if (controlEngine.StudentInformationSelection != 0)
                    {
                        throw new InvalidDataException("Tab did not wrap the six-stop Student Information focus to Name.");
                    }
                    controlEngine.HandleKeyDown(Keys.Right);
                    if (controlEngine.StudentInformationSelection != 1)
                    {
                        throw new InvalidDataException("Right did not advance Student Information focus to Goal WPM.");
                    }
                    controlEngine.HandleMouseWheel(120);
                    controlEngine.HandleKeyDown(Keys.PageUp);
                    controlEngine.HandleKeyDown(Keys.Home);
                    controlEngine.HandleCharacter('4');
                    controlEngine.HandleCharacter('0');
                    controlEngine.HandleKeyDown(Keys.Enter);
                    controlEngine.HandleKeyDown(Keys.D);
                    if (controlEngine.GoalWordsPerMinute != 40)
                    {
                        throw new InvalidDataException("Focused WPM wheel/Page/direct-entry controls did not commit.");
                    }

                    controlEngine.HandleKeyDown(Keys.E);
                    controlEngine.HandleCharacter('e');
                    controlEngine.HandleClick(150, 94);
                    if (controlEngine.StudentInformationEditField != 1)
                    {
                        throw new InvalidDataException("Clicking the Goal WPM row did not activate its editor.");
                    }
                    controlEngine.HandleKeyDown(Keys.End);
                    controlEngine.HandleKeyDown(Keys.PageDown);
                    controlEngine.HandleCharacter('4');
                    controlEngine.HandleCharacter('0');
                    controlEngine.HandleKeyDown(Keys.Enter);
                    controlEngine.HandleKeyDown(Keys.D);
                    if (controlEngine.GoalWordsPerMinute != 40)
                    {
                        throw new InvalidDataException("Mouse-selected Goal WPM direct entry did not commit.");
                    }

                    controlEngine.HandleKeyDown(Keys.E);
                    controlEngine.HandleCharacter('e');
                    // Preferences opens on Done (5); four steps select Luigi (3).
                    controlEngine.HandleKeyDown(Keys.Right);
                    controlEngine.HandleKeyDown(Keys.Right);
                    controlEngine.HandleKeyDown(Keys.Right);
                    controlEngine.HandleKeyDown(Keys.Right);
                    controlEngine.HandleKeyDown(Keys.Enter);
                    // Two more steps return to Done.
                    controlEngine.HandleKeyDown(Keys.Right);
                    controlEngine.HandleKeyDown(Keys.Right);
                    controlEngine.HandleKeyDown(Keys.Enter);
                    if (controlEngine.Screen != OriginalScreen.MainMenu ||
                        controlEngine.GoalWordsPerMinute != 40 ||
                        controlEngine.StudentCharacter != OriginalCharacter.Luigi)
                    {
                        throw new InvalidDataException(
                            $"Original six-stop keyboard WPM/character editing did not commit " +
                            $"(screen={controlEngine.Screen}, goal={controlEngine.GoalWordsPerMinute}, " +
                            $"character={controlEngine.StudentCharacter}, selection={controlEngine.StudentInformationSelection}, " +
                            $"edit={controlEngine.StudentInformationEditField}).");
                    }

                    controlEngine.HandleKeyDown(Keys.T);
                    if (controlEngine.Screen != OriginalScreen.LessonTimes ||
                        controlEngine.StudentInformationSelection != 4)
                    {
                        throw new InvalidDataException("Lesson Times did not begin on its original Done focus.");
                    }
                    controlEngine.HandleKeyDown(Keys.Tab);
                    if (controlEngine.StudentInformationSelection != 0)
                    {
                        throw new InvalidDataException("Tab did not wrap the five-stop Lesson Times focus to Outside.");
                    }
                    controlEngine.HandleMouseWheel(120);
                    controlEngine.HandleKeyDown(Keys.Home);
                    controlEngine.HandleKeyDown(Keys.PageUp);
                    controlEngine.HandleKeyDown(Keys.Enter);
                    controlEngine.HandleCharacter('3');
                    controlEngine.HandleKeyDown(Keys.Enter);
                    controlEngine.HandleClick(200, 90);
                    if (controlEngine.LessonTimeEditField != 1)
                    {
                        throw new InvalidDataException("Clicking a Lesson Times numeric field did not start editing.");
                    }
                    controlEngine.HandleCharacter('1');
                    controlEngine.HandleCharacter('2');
                    controlEngine.HandleKeyDown(Keys.Enter);
                    controlEngine.HandleKeyDown(Keys.D);
                    if (controlEngine.Screen != OriginalScreen.MainMenu ||
                        !controlEngine.LessonTimesMinutes.SequenceEqual([3, 12, 5, 10]))
                    {
                        throw new InvalidDataException("Lesson-time editing did not commit through mouse and typed input.");
                    }

                    if (NativeGameEngine.ProblemKeySummary(new SessionResult(
                            DateTimeOffset.UtcNow, "Tester", LessonCategory.HomeRow,
                            ActivityMode.LetterDrill, 10, 2, .8, 10, 10, 1, "Outside",
                            new Dictionary<string, int> { ["e"] = 5, ["t"] = 3, ["Space"] = 1 })) !=
                        " e t  ")
                    {
                        throw new InvalidDataException("Original report-card problem-key ranking/format failed.");
                    }

                    controlEngine.HandleKeyDown(Keys.D4);
                    controlEngine.HandleCharacter('4');
                    var practiceLineLength = controlEngine.PracticeLineLength;
                    for (var typed = 0; typed < practiceLineLength; typed++)
                    {
                        var expected = controlEngine.ExpectedCharacter ??
                            throw new InvalidDataException("Practice lost its expected character mid-line.");
                        controlEngine.HandleCharacter(expected);
                    }
                    if (!controlEngine.PracticeAwaitingLineAdvance ||
                        controlEngine.ExpectedCharacter != ' ')
                    {
                        throw new InvalidDataException("Practice did not stop at its original row checkpoint.");
                    }
                    controlEngine.HandleKeyDown(Keys.Enter);
                    if (controlEngine.PracticeAwaitingLineAdvance ||
                        controlEngine.PracticeLineLength == 0)
                    {
                        throw new InvalidDataException("Enter did not advance the original Practice row queue.");
                    }
                    controlEngine.HandleKeyDown(Keys.Escape);

                    controlEngine.HandleKeyDown(Keys.D3);
                    controlEngine.HandleCharacter('3');
                    for (var typed = 0;
                         typed < 1000 && controlEngine.CastleSequencePhase == 0;
                         typed++)
                    {
                        controlEngine.HandleCharacter(controlEngine.ExpectedCharacter ??
                            throw new InvalidDataException("Castle lost its expected character."));
                    }
                    if (controlEngine.CastleSequencePhase != 1)
                    {
                        throw new InvalidDataException("Castle did not enter its first original interlude.");
                    }
                    var castleTypedBeforeInterludeKey = controlEngine.CorrectCharacterCount;
                    controlEngine.HandleCharacter(controlEngine.ExpectedCharacter ?? 'x');
                    if (controlEngine.CorrectCharacterCount != castleTypedBeforeInterludeKey)
                    {
                        throw new InvalidDataException("Castle accepted lesson input during a non-interactive interlude.");
                    }
                    if (controlEngine.CastleConstructionFrameCount is < 40 or > 65 ||
                        controlEngine.CastleConstructionDwellFrames.Any(dwell => dwell is < 4 or > 9) ||
                        controlEngine.CastleConstructionFrameCount !=
                            20 + controlEngine.CastleConstructionDwellFrames.Sum())
                    {
                        throw new InvalidDataException("Castle construction random dwell range failed.");
                    }
                    var firstCastleInterludeFrames = 0;
                    while (controlEngine.CastleSequencePhase == 1 && firstCastleInterludeFrames < 200)
                    {
                        controlEngine.AdvanceCastleFramesForTest(1);
                        firstCastleInterludeFrames++;
                    }
                    if (controlEngine.CastleSequencePhase != 2 ||
                        firstCastleInterludeFrames is < 169 or > 194)
                    {
                        throw new InvalidDataException("Castle did not reach its sinking/fire typing scene.");
                    }
                    for (var typed = 0;
                         typed < 1000 && controlEngine.CastleSequencePhase == 2;
                         typed++)
                    {
                        controlEngine.HandleCharacter(controlEngine.ExpectedCharacter ??
                            throw new InvalidDataException("Castle second scene lost its expected character."));
                    }
                    if (controlEngine.CastleSequencePhase != 3)
                    {
                        throw new InvalidDataException("Castle did not enter its second construction interlude.");
                    }
                    if (controlEngine.CastleConstructionFrameCount is < 40 or > 65 ||
                        controlEngine.CastleConstructionDwellFrames.Any(dwell => dwell is < 4 or > 9))
                    {
                        throw new InvalidDataException("Second castle construction random dwell range failed.");
                    }
                    var secondCastleInterludeFrames = 0;
                    while (controlEngine.CastleSequencePhase == 3 && secondCastleInterludeFrames < 175)
                    {
                        controlEngine.AdvanceCastleFramesForTest(1);
                        secondCastleInterludeFrames++;
                    }
                    if (controlEngine.CastleSequencePhase != 0 ||
                        secondCastleInterludeFrames is < 143 or > 168)
                    {
                        throw new InvalidDataException("Castle did not scroll back to its rock-face scene.");
                    }
                    controlEngine.HandleKeyDown(Keys.Escape);

                    controlEngine.HandleKeyDown(Keys.D2);
                    controlEngine.HandleCharacter('2');
                    controlEngine.AdvanceWaterFramesForTest(1);
                    if (controlEngine.WaterCreatureRenderedKind != 1 ||
                        controlEngine.WaterCreatureRenderedFrame != 1 ||
                        controlEngine.WaterCreatureStateKind != 2)
                    {
                        throw new InvalidDataException("Water did not preserve the first biter frame before switching to the octopus state.");
                    }
                    controlEngine.AdvanceWaterFramesForTest(1);
                    if (controlEngine.WaterCreatureRenderedKind != 2 ||
                        controlEngine.WaterCreatureRenderedFrame != 2)
                    {
                        throw new InvalidDataException("Water did not enter the recovered first octopus frame.");
                    }
                    for (var bubble = 0; bubble < 11; bubble++)
                    {
                        controlEngine.AddWaterBubbleForTest(100 + bubble, 170);
                    }
                    if (controlEngine.WaterBubbleCount != 10)
                    {
                        throw new InvalidDataException("Water ambient-bubble pool did not enforce its original ten-slot limit.");
                    }
                    controlEngine.HandleKeyDown(Keys.Escape);

                    controlEngine.HandleKeyDown(Keys.D2);
                    controlEngine.HandleCharacter('2');
                    controlEngine.AdvanceWaterFramesForTest(20);
                    if (controlEngine.WaterFailurePhase != 0)
                    {
                        throw new InvalidDataException("Water failure started before the original twenty-tick input grace period.");
                    }
                    controlEngine.AdvanceWaterFramesForTest(1);
                    if (controlEngine.WaterFailurePhase != 1)
                    {
                        throw new InvalidDataException("Water failure did not start on tick twenty-one.");
                    }
                    for (var frame = 0;
                         frame < 200 && controlEngine.WaterFailurePhase == 1;
                         frame++)
                    {
                        controlEngine.AdvanceWaterFramesForTest(1);
                    }
                    if (controlEngine.WaterFailurePhase != 2)
                    {
                        throw new InvalidDataException("Water trailing creature never reached Mario's catch boundary.");
                    }
                    for (var frame = 0;
                         frame < 200 && !controlEngine.WaterFailureComplete;
                         frame++)
                    {
                        controlEngine.AdvanceWaterFramesForTest(1);
                    }
                    if (!controlEngine.WaterFailureComplete)
                    {
                        throw new InvalidDataException("Water sink path never reached its original completion boundary.");
                    }
                    controlEngine.HandleKeyDown(Keys.Escape);

                    controlEngine.HandleKeyDown(Keys.D1);
                    controlEngine.HandleCharacter('1');
                    controlEngine.PrepareOutsideTargetForTest(1);
                    if (controlEngine.OutsideCurrentForwardSpeed != 1 ||
                        controlEngine.OutsideCurrentGroundOffset != 0 ||
                        controlEngine.OutsideCursorFrame != 0)
                    {
                        throw new InvalidDataException("Outdoor first-frame speed/cursor state did not match the DOS initialization.");
                    }
                    controlEngine.HandleCharacter(controlEngine.ExpectedCharacter ??
                        throw new InvalidDataException("Forced outdoor brick lost its expected character."));
                    if (controlEngine.OutsideHitFramesRemaining != 6 ||
                        controlEngine.OutsideParticleCount != 0)
                    {
                        throw new InvalidDataException("Outdoor brick did not enter its exact six-tick hit hold.");
                    }
                    controlEngine.AdvanceOutsideFramesForTest(1);
                    if (controlEngine.OutsideHitFramesRemaining != 5 ||
                        controlEngine.OutsideCurrentGroundOffset != 1 ||
                        controlEngine.OutsideCurrentForwardSpeed != 9 ||
                        controlEngine.OutsideCursorFrame != 0)
                    {
                        throw new InvalidDataException("Outdoor first collision tick did not use the recovered update order.");
                    }
                    controlEngine.AdvanceOutsideFramesForTest(4);
                    if (controlEngine.OutsideHitFramesRemaining != 1 ||
                        controlEngine.OutsideParticleCount != 0)
                    {
                        throw new InvalidDataException("Outdoor brick broke before its sixth collision tick.");
                    }
                    controlEngine.AdvanceOutsideFramesForTest(1);
                    if (controlEngine.OutsideHitFramesRemaining != 0 ||
                        controlEngine.OutsideParticleCount != 4)
                    {
                        throw new InvalidDataException("Outdoor brick did not split into four ballistic fragments on tick six.");
                    }
                    controlEngine.PrepareOutsideTargetForTest(2);
                    controlEngine.HandleCharacter(controlEngine.ExpectedCharacter ??
                        throw new InvalidDataException("Forced outdoor turtle lost its expected character."));
                    controlEngine.AdvanceOutsideFramesForTest(6);
                    if (controlEngine.OutsideHitFramesRemaining != 0 ||
                        controlEngine.OutsideParticleCount != 0)
                    {
                        throw new InvalidDataException("Outdoor turtle did not use its six-tick alternate-frame removal path.");
                    }
                    using var outsideBeforeBonus = new Bitmap(controlEngine.CurrentFrame);
                    controlEngine.ActivateBonusForTest('!');
                    if (controlEngine.ActiveBonusCharacter != '!')
                    {
                        throw new InvalidDataException("Outdoor bonus punctuation did not become an alternate target.");
                    }
                    using (var outsideAfterBonus = new Bitmap(controlEngine.CurrentFrame))
                    using (var expectedBonusRaster = new NativeRaster(20, 16))
                    {
                        var outsideScene = CrxImage.Decode(originalAssets.ReadAllBytes("OUTSIDE.CRX"));
                        var outsideFont7 = new OriginalFont(
                            OriginalSpriteBank.DecodeGso(
                                originalAssets.ReadAllBytes("FONT7I.GSO"),
                                originalAssets.ReadAllText("FONT7I.GSK")),
                            "font7_",
                            10,
                            10);
                        expectedBonusRaster.DrawSprite(
                            outsideObjects["brick1_1"], outsideScene.Palette, 0, 0);
                        outsideFont7.Draw(
                            expectedBonusRaster,
                            outsideScene.Palette,
                            "!",
                            6,
                            3,
                            1,
                            foregroundIndex: 15,
                            secondaryIndex: 31);
                        using var expectedBonus = expectedBonusRaster.Snapshot();
                        for (var y = 0; y < 16; y++)
                        {
                            for (var x = 0; x < 20; x++)
                            {
                                var expectedPixel = expectedBonus.GetPixel(x, y);
                                var actualPixel = outsideAfterBonus.GetPixel(100 + x, 80 + y);
                                var requiredPixel = expectedPixel.A == 0
                                    ? outsideBeforeBonus.GetPixel(100 + x, 80 + y)
                                    : expectedPixel;
                                if (actualPixel.ToArgb() != requiredPixel.ToArgb())
                                {
                                    throw new InvalidDataException(
                                        $"Outdoor bonus brick/glyph mismatch at ({x},{y}).");
                                }
                            }
                        }
                    }
                    controlEngine.HandleCharacter('!');
                    if (controlEngine.Screen != OriginalScreen.BonusAnimation ||
                        controlEngine.ActiveBonusAnimationFrameCount < 2 ||
                        !controlEngine.ActiveBonusUsesOutsideSky)
                    {
                        throw new InvalidDataException(
                            "Outdoor bonus did not enter its OUTSKY-based embedded BAN/ANI player.");
                    }
                    controlEngine.StartEndingAnimationForTest();
                    if (controlEngine.Screen != OriginalScreen.EndingAnimation ||
                        controlEngine.EndingAnimationFrameCount != 40)
                    {
                        throw new InvalidDataException("Castle certificate ending did not enter its native BAN player.");
                    }
                    controlEngine.HandleKeyDown(Keys.Space);
                    if (controlEngine.Screen != OriginalScreen.Blackboard)
                    {
                        throw new InvalidDataException("Castle certificate ending did not return to the blackboard.");
                    }
                }

                var reloadedControls = ProgressStore.Load(controlTestPath);
                if (reloadedControls.GoalWordsPerMinute != 40 ||
                    reloadedControls.Character != OriginalCharacter.Luigi ||
                    !reloadedControls.LessonTimesMinutes.SequenceEqual([3, 12, 5, 10]))
                {
                    throw new InvalidDataException("Edited WPM, character, or lesson times did not persist.");
                }
                checks.Add("Student WPM, character, lesson-time controls, Practice checkpoint cue, four-scene castle cycle, 40-frame certificate ending, exact water creature/failure timing, ambient bubble physics, outdoor bonus brick, six-tick target hits, four-fragment brick burst, and native bonus animation route validated through UI input and persistence.");
            }
            finally
            {
                File.Delete(controlTestPath);
                File.Delete(controlTestPath + ".tmp");
            }

            var routeTestPath = Path.Combine(
                Path.GetTempPath(),
                $"mtt-native-routes-{Guid.NewGuid():N}.json");
            try
            {
                var routeProfile = ProgressStore.Load(routeTestPath);
                routeProfile.SetStudentInformation(
                    "RouteTes",
                    45,
                    6,
                    OriginalCharacter.Princess);
                routeProfile.SetCurrentWorld(OriginalScreen.Castle);
                var now = DateTimeOffset.UtcNow;
                routeProfile.Add(new SessionResult(
                    now.AddMinutes(-3), "RouteTes", LessonCategory.HomeRow,
                    ActivityMode.LetterDrill, 120, 4, .97, 12, 60, 6, "Outside",
                    new Dictionary<string, int> { ["a"] = 3, ["Space"] = 1 }));
                routeProfile.Add(new SessionResult(
                    now.AddMinutes(-2), "RouteTes", LessonCategory.TopRow,
                    ActivityMode.WordChallenge, 180, 6, .97, 32, 60, 6, "Water",
                    new Dictionary<string, int> { ["e"] = 4, ["a"] = 2 }));
                routeProfile.Add(new SessionResult(
                    now.AddMinutes(-1), "RouteTes", LessonCategory.BottomRow,
                    ActivityMode.SentenceChallenge, 240, 8, .97, 47, 60, 6, "Castle",
                    new Dictionary<string, int> { ["e"] = 5, ["t"] = 3 }));
                routeProfile.AwardCertificate(OriginalScreen.Outside);
                routeProfile.AwardCertificate(OriginalScreen.Water);
                routeProfile.AwardCertificate(OriginalScreen.Castle);

                using var routeEngine = new NativeGameEngine(
                    enableAudio: false,
                    enableCinematics: false,
                    progressFilePath: routeTestPath);
                routeEngine.HandleKeyDown(Keys.Space);
                if (routeEngine.Screen != OriginalScreen.MainMenu ||
                    routeEngine.CurrentWorld != OriginalScreen.Castle)
                {
                    throw new InvalidDataException("The persisted active world did not load into NEXT sequencing.");
                }

                routeEngine.HandleKeyDown(Keys.M);
                routeEngine.HandleKeyDown(Keys.S);
                routeEngine.HandleKeyDown(Keys.I);
                if (!routeEngine.MusicEnabled || !routeEngine.SoundEffectsEnabled || !routeEngine.SpeechEnabled)
                {
                    throw new InvalidDataException("Independent Music, Sound F/X, or M.I.R.T. speech toggles failed.");
                }
                routeEngine.HandleKeyDown(Keys.M);
                routeEngine.HandleKeyDown(Keys.S);
                routeEngine.HandleKeyDown(Keys.I);

                routeEngine.HandleClick(250, 175);
                if (routeEngine.Screen != OriginalScreen.Castle)
                {
                    throw new InvalidDataException("The main-menu NEXT card did not open the persisted world.");
                }
                routeEngine.HandleKeyDown(Keys.Escape);
                routeEngine.HandleKeyDown(Keys.D);
                routeEngine.HandleClick(250, 55);
                if (routeEngine.Screen != OriginalScreen.Water)
                {
                    throw new InvalidDataException("Demo world selection did not respond to its mouse card.");
                }
                routeEngine.HandleKeyDown(Keys.Escape);

                var worldCards = new[]
                {
                    (20, OriginalScreen.Outside),
                    (55, OriginalScreen.Water),
                    (85, OriginalScreen.Castle),
                    (120, OriginalScreen.Practice),
                };
                foreach (var (y, expectedWorld) in worldCards)
                {
                    routeEngine.HandleClick(250, y);
                    if (routeEngine.Screen != expectedWorld)
                    {
                        throw new InvalidDataException($"{expectedWorld} mouse card route failed.");
                    }
                    routeEngine.HandleKeyDown(Keys.Escape);
                }

                routeEngine.HandleClick(12, 4);
                routeEngine.HandleKeyDown(Keys.A);
                if (routeEngine.Screen != OriginalScreen.About)
                {
                    throw new InvalidDataException("File-menu keyboard accelerator A was swallowed.");
                }
                routeEngine.HandleKeyDown(Keys.Space);

                routeEngine.HandleClick(145, 4);
                routeEngine.HandleKeyDown(Keys.E);
                if (routeEngine.Screen != OriginalScreen.StudentInformation)
                {
                    throw new InvalidDataException("Student-menu keyboard accelerator E was swallowed.");
                }
                routeEngine.HandleKeyDown(Keys.Escape);

                routeEngine.HandleKeyDown(Keys.A);
                if (routeEngine.Screen != OriginalScreen.About)
                {
                    throw new InvalidDataException("About command route failed.");
                }
                routeEngine.HandleClick(1, 1);
                routeEngine.HandleKeyDown(Keys.F1);
                if (routeEngine.Screen != OriginalScreen.Help)
                {
                    throw new InvalidDataException("Help command route failed.");
                }
                routeEngine.HandleMouseWheel(-120);
                routeEngine.HandleClick(280, 185);
                routeEngine.HandleKeyDown(Keys.F2);
                if (routeEngine.Screen != OriginalScreen.Keyboard)
                {
                    throw new InvalidDataException("Keyboard command route failed.");
                }
                routeEngine.HandleClick(1, 1);

                CertificatePrintRequest? certificateRequest = null;
                routeEngine.CertificatePrintRequested += request => certificateRequest = request;
                routeEngine.HandleKeyDown(Keys.C);
                if (routeEngine.Screen != OriginalScreen.Certificate ||
                    routeEngine.AuxiliarySelection != 3)
                {
                    throw new InvalidDataException("Certificates did not start on the original Done focus.");
                }
                routeEngine.HandleKeyDown(Keys.Up);
                if (routeEngine.AuxiliarySelection != 2)
                {
                    throw new InvalidDataException("Certificate Up did not wrap from Done to Underground.");
                }
                routeEngine.HandleKeyDown(Keys.Down);
                if (routeEngine.AuxiliarySelection != 3)
                {
                    throw new InvalidDataException("Certificate Down did not restore Done focus.");
                }
                routeEngine.HandleClick(80, 107);
                if (certificateRequest is null ||
                    certificateRequest.Student != "RouteTes" ||
                    certificateRequest.Level != "Outside")
                {
                    throw new InvalidDataException("Clicking an earned certificate did not request printing.");
                }
                routeEngine.HandleKeyDown(Keys.Escape);

                routeEngine.HandleClick(250, 150);
                if (routeEngine.Screen != OriginalScreen.ReportCard)
                {
                    throw new InvalidDataException("Report Card mouse route failed.");
                }
                routeEngine.HandleKeyDown(Keys.Left);
                if (routeEngine.Screen != OriginalScreen.MainMenu)
                {
                    throw new InvalidDataException("Report Card did not close on the original any-key input.");
                }

                routeEngine.HandleKeyDown(Keys.D5);
                routeEngine.HandleClick(1, 1);
                if (routeEngine.Screen != OriginalScreen.MainMenu)
                {
                    throw new InvalidDataException("Report Card did not close on the original any-click input.");
                }

                routeEngine.StartEndingAnimationForTest();
                routeEngine.HandleKeyDown(Keys.Space);
                var blackboard = routeEngine.BlackboardState;
                if (routeEngine.Screen != OriginalScreen.Blackboard ||
                    blackboard.KeysTyped != " 248" ||
                    blackboard.Errors != "   8" ||
                    blackboard.WordsPerMinute != " 47" ||
                    blackboard.GoalWordsPerMinute != " 45" ||
                    blackboard.Accuracy != " 97%" ||
                    blackboard.LessonTime != "  1:00")
                {
                    throw new InvalidDataException("Original blackboard values or formatting failed.");
                }
                routeEngine.HandleKeyDown(Keys.M);
                if (routeEngine.Screen != OriginalScreen.MainMenu)
                {
                    throw new InvalidDataException("Blackboard Menu key did not return to the main menu.");
                }
                routeEngine.StartEndingAnimationForTest();
                routeEngine.HandleKeyDown(Keys.Space);
                var expectedBlackboardNext = routeEngine.CurrentWorld;
                routeEngine.HandleClick(78, 184);
                if (routeEngine.Screen != expectedBlackboardNext)
                {
                    throw new InvalidDataException("Blackboard Next button did not start the persisted world.");
                }
                routeEngine.HandleKeyDown(Keys.Escape);

                routeEngine.HandleKeyDown(Keys.R);
                routeEngine.HandleKeyDown(Keys.Space);

                var exitRequested = false;
                routeEngine.ExitRequested += (_, _) => exitRequested = true;
                routeEngine.HandleKeyDown(Keys.Q);
                if (!exitRequested)
                {
                    throw new InvalidDataException("Quit command did not raise its native close request.");
                }

                routeEngine.HandleClick(145, 4);
                routeEngine.HandleClick(145, 12);
                if (routeEngine.Screen != OriginalScreen.StudentInformation ||
                    routeEngine.StudentInformationEditField != 0)
                {
                    throw new InvalidDataException("New Student did not enter the original Preferences name field.");
                }
                foreach (var character in "MouseStu")
                {
                    routeEngine.HandleCharacter(character);
                }
                routeEngine.HandleKeyDown(Keys.Enter);
                if (routeEngine.Screen != OriginalScreen.StudentInformation ||
                    routeEngine.StudentInformationSelection != 5 ||
                    routeEngine.StudentName != "MouseStu")
                {
                    throw new InvalidDataException("New Student did not initialize defaults and remain in Preferences.");
                }
                routeEngine.HandleClick(280, 185);
                if (routeEngine.Screen != OriginalScreen.MainMenu)
                {
                    throw new InvalidDataException("New Student Preferences Done control did not return to the menu.");
                }

                var expectedLessonLevels = Enumerable.Range(1, 6).ToArray();
                foreach (var expectedLevel in expectedLessonLevels)
                {
                    routeEngine.HandleClick(250, 4);
                    if (routeEngine.ActiveMenuPopup != MainMenuPopup.Lessons ||
                        routeEngine.ActiveMenuItemCount != 6)
                    {
                        throw new InvalidDataException("Lessons popup did not expose all six original choices.");
                    }
                    routeEngine.HandleClick(180, 9 + (expectedLevel - 1) * 9 + 4);
                    if (routeEngine.Screen != OriginalScreen.MainMenu ||
                        routeEngine.LessonLevel != expectedLevel ||
                        routeEngine.SelectedLesson != expectedLevel - 1)
                    {
                        throw new InvalidDataException($"Lesson menu item {expectedLevel} did not persist its selection.");
                    }
                }

                routeEngine.HandleKeyDown(Keys.T);
                routeEngine.HandleKeyDown(Keys.Tab);
                routeEngine.HandleKeyDown(Keys.Enter);
                routeEngine.HandleCharacter('4');
                routeEngine.HandleKeyDown(Keys.Enter);
                routeEngine.HandleKeyDown(Keys.Escape);
                if (routeEngine.Screen != OriginalScreen.MainMenu ||
                    routeEngine.LessonTimesMinutes[0] != 4)
                {
                    throw new InvalidDataException("Lesson Times Escape did not save through the original Done path.");
                }

                routeEngine.HandleKeyDown(Keys.D6);
                if (routeEngine.Screen != OriginalScreen.Outside)
                {
                    throw new InvalidDataException("Keyboard card 6/NEXT did not open the profile's eligible world.");
                }
                routeEngine.HandleKeyDown(Keys.Escape);

                var reloadedRouteSettings = ProgressStore.Load(routeTestPath);
                if (reloadedRouteSettings.MusicEnabled ||
                    reloadedRouteSettings.SoundEffectsEnabled ||
                    reloadedRouteSettings.SpeechEnabled)
                {
                    throw new InvalidDataException("Global Music, Sound F/X, or Speech settings did not persist.");
                }

                if (reloadedRouteSettings.LessonLevel != 6 ||
                    reloadedRouteSettings.LessonTimesMinutes[0] != 4)
                {
                    throw new InvalidDataException("Lesson menu or Escape-saved lesson time did not survive reload.");
                }

                checks.Add("All six lesson routes, exact main-menu card hitboxes, open-menu accelerators, persisted Music/SFX/M.I.R.T. settings, demo, exact blackboard values and Next/Menu routes, single-page report, four-stop certificate, and mouse dialog routes validated.");
            }
            finally
            {
                File.Delete(routeTestPath);
                File.Delete(routeTestPath + ".tmp");
            }

            var musicEventCount = 0;
            for (var number = 1; number <= 4; number++)
            {
                var sequences = XmidiDecoder.Decode(originalAssets.ReadAllBytes($"RTYP{number}.XMI"));
                if (sequences.Count != 1 || sequences[0].Events.Count == 0 || sequences[0].DurationTicks <= 0)
                {
                    throw new InvalidDataException($"RTYP{number}.XMI failed native XMIDI validation.");
                }
                musicEventCount += sequences[0].Events.Count;
            }
            var effectSequenceCount = 0;
            var effectEventCount = 0;
            for (var number = 1; number <= 10; number++)
            {
                var sequences = XmidiDecoder.Decode(originalAssets.ReadAllBytes($"SFX{number:00}.RLD"));
                if (sequences.Count == 0 || sequences.Any(sequence => sequence.Events.Count == 0))
                {
                    throw new InvalidDataException($"SFX{number:00}.RLD failed native XMIDI validation.");
                }
                effectSequenceCount += sequences.Count;
                effectEventCount += sequences.Sum(sequence => sequence.Events.Count);
            }
            checks.Add(
                $"Original XMIDI music/SFX decoded for native Windows MIDI: " +
                $"{musicEventCount} music events and {effectEventCount} events in " +
                $"{effectSequenceCount} effect variants.");

            using (var cdAudio = EmbeddedAssets.OpenCdAudioTrack())
            {
                if (cdAudio.Length != EmbeddedAssets.ExpectedCdAudioTrackLength)
                {
                    throw new InvalidDataException($"Embedded CD audio length mismatch: {cdAudio.Length}");
                }
            }
            checks.Add($"Embedded CD audio track: {EmbeddedAssets.ExpectedCdAudioTrackLength} bytes");
            checks.Add("Runtime dependency check: native engine only; no emulator process or executable.");

            WriteReport(reportPath, true, checks, null);
            return 0;
        }
        catch (Exception exception)
        {
            WriteReport(reportPath, false, checks, exception.ToString());
            return 1;
        }
    }

    public static int RenderOriginalFrames(string directory)
    {
        string? renderProfilePath = null;
        try
        {
            Directory.CreateDirectory(directory);
            renderProfilePath = Path.Combine(
                Path.GetTempPath(),
                $"mtt-native-render-{Guid.NewGuid():N}.json");
            var renderProfile = ProgressStore.Load(renderProfilePath);
            renderProfile.SetStudentInformation(
                "Mario",
                30,
                6,
                OriginalCharacter.Mario);
            renderProfile.Add(new SessionResult(
                DateTimeOffset.UtcNow,
                "Mario",
                LessonCategory.Symbols,
                ActivityMode.SentenceChallenge,
                240,
                8,
                .97,
                47,
                60,
                6,
                "Castle",
                new Dictionary<string, int> { ["e"] = 5, ["t"] = 3 }));
            using var engine = new NativeGameEngine(
                enableAudio: false,
                enableCinematics: false,
                progressFilePath: renderProfilePath);
            Save(engine, directory, "00-title.png");
            engine.SetTitleAnimationMillisecondsForTest(4_360);
            Save(engine, directory, "00a-title-zoom-small.png");
            engine.SetTitleAnimationMillisecondsForTest(5_460);
            Save(engine, directory, "00b-title-zoom-large.png");
            engine.SetTitleAnimationMillisecondsForTest(6_810);
            Save(engine, directory, "00c-title-first-letter.png");
            engine.SetTitleAnimationMillisecondsForTest(engine.TitleAllLettersVisibleMilliseconds);
            Save(engine, directory, "00d-title-complete.png");
            engine.HandleKeyDown(Keys.Space);
            Save(engine, directory, "01-main-menu.png");

            engine.HandleClick(12, 4);
            Save(engine, directory, "01a-file-menu.png");
            engine.HandleKeyDown(Keys.Escape);
            engine.HandleClick(250, 4);
            Save(engine, directory, "01b-lessons-menu.png");
            engine.HandleKeyDown(Keys.Escape);

            engine.HandleKeyDown(Keys.A);
            Save(engine, directory, "01c-about.png");
            engine.HandleKeyDown(Keys.Space);
            engine.HandleKeyDown(Keys.F1);
            Save(engine, directory, "01d-help.png");
            engine.HandleKeyDown(Keys.PageDown);
            Save(engine, directory, "01e-help-scrolled.png");
            engine.HandleKeyDown(Keys.Escape);
            engine.HandleKeyDown(Keys.F2);
            Save(engine, directory, "01f-keyboard.png");
            engine.HandleKeyDown(Keys.Space);

            engine.HandleClick(145, 4);
            engine.HandleKeyDown(Keys.Down);
            engine.HandleKeyDown(Keys.Enter);
            Save(engine, directory, "01g-load-student.png");
            engine.HandleKeyDown(Keys.Escape);
            engine.HandleClick(145, 4);
            engine.HandleKeyDown(Keys.Down);
            engine.HandleKeyDown(Keys.Down);
            engine.HandleKeyDown(Keys.Enter);
            Save(engine, directory, "01h-student-information.png");
            engine.HandleKeyDown(Keys.Escape);
            engine.HandleClick(145, 4);
            engine.HandleKeyDown(Keys.Down);
            engine.HandleKeyDown(Keys.Down);
            engine.HandleKeyDown(Keys.Down);
            engine.HandleKeyDown(Keys.Enter);
            Save(engine, directory, "01i-lesson-times.png");
            engine.HandleKeyDown(Keys.Escape);
            engine.HandleClick(145, 4);
            for (var item = 0; item < 4; item++)
            {
                engine.HandleKeyDown(Keys.Down);
            }
            engine.HandleKeyDown(Keys.Enter);
            Save(engine, directory, "01j-certificates.png");
            engine.HandleKeyDown(Keys.Escape);

            engine.HandleClick(12, 4);
            for (var item = 0; item < 6; item++)
            {
                engine.HandleKeyDown(Keys.Down);
            }
            Save(engine, directory, "01l-mirt-speech-toggle.png");
            engine.HandleKeyDown(Keys.Enter);

            using (var cinematicEngine = new NativeGameEngine(
                       enableAudio: false,
                       enableCinematics: true,
                       progressFilePath: renderProfilePath))
            {
                cinematicEngine.HandleKeyDown(Keys.Space);
                cinematicEngine.HandleClick(250, 20);
                if (cinematicEngine.Screen != OriginalScreen.AvMovie)
                {
                    throw new InvalidDataException("Outdoor AV cue did not enter the native AV player.");
                }
                Save(cinematicEngine, directory, "01k-outside-av-intro.png");
                cinematicEngine.HandleKeyDown(Keys.Space);
                if (cinematicEngine.Screen != OriginalScreen.Outside)
                {
                    throw new InvalidDataException("Outdoor AV cue did not return to native gameplay.");
                }
            }

            var scenes = new[]
            {
                (Keys.D1, "02-outside.png"),
                (Keys.D2, "03-water.png"),
                (Keys.D3, "04-castle.png"),
                (Keys.D4, "05-practice.png"),
                (Keys.D5, "06-report-card.png"),
            };
            foreach (var (key, fileName) in scenes)
            {
                engine.HandleKeyDown(key);
                if (key is >= Keys.D1 and <= Keys.D4)
                {
                    engine.HandleCharacter((char)('0' + ((int)key - (int)Keys.D0)));
                }
                if (key == Keys.D1)
                {
                    engine.AdvanceOutsideFramesForTest(15);
                }
                else if (key == Keys.D2)
                {
                    engine.AdvanceWaterFramesForTest(10);
                }
                else if (key == Keys.D3)
                {
                    engine.AdvanceCastleFramesForTest(10);
                }
                Save(engine, directory, fileName);
                if (key == Keys.D2)
                {
                    engine.AddWaterBubbleForTest(200, 140);
                    engine.AdvanceWaterBubblesForTest(11);
                    Save(engine, directory, "03a-water-ambient-bubble.png");
                }
                if (key == Keys.D1 && engine.ExpectedCharacter is { } expected)
                {
                    engine.PrepareOutsideTargetForTest(1);
                    Save(engine, directory, "02a-outside-finger-guidance.png");
                    expected = engine.ExpectedCharacter ?? expected;
                    engine.HandleCharacter(expected);
                    Save(engine, directory, "02b-outside-hit.png");
                    engine.AdvanceOutsideFramesForTest(6);
                    Save(engine, directory, "02c-outside-brick-burst.png");
                    engine.ActivateBonusForTest('!');
                    Save(engine, directory, "02d-outside-bonus-target.png");
                    engine.HandleCharacter('!');
                    engine.SetBonusAnimationFrameForTest(0);
                    Save(engine, directory, "02e-outside-bonus-outsky.png");
                    engine.FinishBonusAnimationForTest();
                }
                if (key == Keys.D3)
                {
                    for (var typed = 0;
                         typed < 1000 && engine.CastleSequencePhase == 0;
                         typed++)
                    {
                        engine.HandleCharacter(engine.ExpectedCharacter ??
                            throw new InvalidDataException("Rendered castle route lost its expected character."));
                    }
                    engine.AdvanceCastleFramesForTest(20);
                    Save(engine, directory, "04c-castle-scroll-0-to-1.png");
                    engine.AdvanceCastleFramesForTest(42);
                    Save(engine, directory, "04d-castle-first-construction.png");
                    for (var frame = 0;
                         frame < 200 && engine.CastleSequencePhase == 1;
                         frame++)
                    {
                        engine.AdvanceCastleFramesForTest(1);
                    }
                    Save(engine, directory, "04e-castle-fire-typing.png");

                    for (var typed = 0;
                         typed < 1000 && engine.CastleSequencePhase == 2;
                         typed++)
                    {
                        engine.HandleCharacter(engine.ExpectedCharacter ??
                            throw new InvalidDataException("Rendered castle second scene lost its expected character."));
                    }
                    engine.AdvanceCastleFramesForTest(33);
                    Save(engine, directory, "04f-castle-scroll-2-to-3.png");
                    engine.AdvanceCastleFramesForTest(29);
                    Save(engine, directory, "04g-castle-second-construction.png");
                }
                if (key == Keys.D4)
                {
                    var lineLength = engine.PracticeLineLength;
                    for (var typed = 0; typed < lineLength; typed++)
                    {
                        engine.HandleCharacter(engine.ExpectedCharacter ??
                            throw new InvalidDataException("Rendered Practice route lost its expected character."));
                    }
                    Save(engine, directory, "05a-practice-space-enter-checkpoint.png");
                }
                engine.HandleKeyDown(Keys.Escape);
            }

            engine.StartEndingAnimationForTest();
            engine.HandleKeyDown(Keys.Space);
            Save(engine, directory, "06a-blackboard-results.png");
            engine.HandleKeyDown(Keys.M);

            engine.StartEndingAnimationForTest();
            engine.SetEndingAnimationFrameForTest(0);
            Save(engine, directory, "07a-castle-ending-00.png");
            engine.SetEndingAnimationFrameForTest(20);
            Save(engine, directory, "07b-castle-ending-20.png");
            engine.SetEndingAnimationFrameForTest(39);
            Save(engine, directory, "07c-castle-ending-39.png");
            engine.HandleKeyDown(Keys.Space);

            var luigiProfile = ProgressStore.Load(renderProfilePath);
            luigiProfile.SetStudentInformation("Luigi", 30, 6, OriginalCharacter.Luigi);
            using (var luigiEngine = new NativeGameEngine(
                       enableAudio: false,
                       enableCinematics: false,
                       progressFilePath: renderProfilePath))
            {
                luigiEngine.HandleKeyDown(Keys.Space);
                luigiEngine.HandleKeyDown(Keys.D1);
                luigiEngine.HandleCharacter('1');
                if (luigiEngine.StudentCharacter != OriginalCharacter.Luigi)
                {
                    throw new InvalidDataException("Luigi gameplay profile did not load.");
                }
                Save(luigiEngine, directory, "02b-outside-luigi.png");
            }

            var princessProfile = ProgressStore.Load(renderProfilePath);
            princessProfile.SetStudentInformation("Princess", 30, 6, OriginalCharacter.Princess);
            using (var princessEngine = new NativeGameEngine(
                       enableAudio: false,
                       enableCinematics: false,
                       progressFilePath: renderProfilePath))
            {
                princessEngine.HandleKeyDown(Keys.Space);
                princessEngine.HandleKeyDown(Keys.D2);
                princessEngine.HandleCharacter('2');
                if (princessEngine.StudentCharacter != OriginalCharacter.Princess)
                {
                    throw new InvalidDataException("Princess gameplay profile did not load.");
                }
                Save(princessEngine, directory, "03b-water-princess.png");
                princessEngine.HandleKeyDown(Keys.Escape);
                princessEngine.HandleKeyDown(Keys.D3);
                princessEngine.HandleCharacter('3');
                Save(princessEngine, directory, "04b-castle-princess.png");
            }

            var store = new OriginalAssetStore();
            var movie = MveDecoder.Decode(store.ReadAllBytes("MOVIES/MARIO1.MVE"));
            using var movieRaster = new NativeRaster(movie.Width, movie.Height);
            foreach (var frameIndex in new[] { 0, 100, movie.Frames.Count - 1 })
            {
                var frame = movie.Frames[frameIndex];
                movieRaster.DrawIndexedImage(
                    frame.Pixels, movie.Width, movie.Height, frame.Palette, 0, 0);
                movieRaster.Snapshot().Save(
                    Path.Combine(directory, $"mve1-{frameIndex:000}.png"),
                    System.Drawing.Imaging.ImageFormat.Png);
            }

            foreach (var assetName in new[]
                     {
                         "HELP.CRX", "KEYBOARD.CRX", "PREFS.CRX", "LOAD.CRX",
                         "LESSTIME.CRX", "CERTIF.CRX", "LOGO.CRX", "BLACKBRD.CRX",
                         "GREETBG.CRX", "GREET.CRX", "ENDF.CRX",
                     })
            {
                var image = CrxImage.Decode(store.ReadAllBytes(assetName));
                using var assetRaster = new NativeRaster(320, 200);
                assetRaster.CopyFrom(image);
                assetRaster.Snapshot().Save(
                    Path.Combine(directory, "asset-" + assetName.ToLowerInvariant() + ".png"),
                    System.Drawing.Imaging.ImageFormat.Png);
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (renderProfilePath is not null)
            {
                File.Delete(renderProfilePath);
                File.Delete(renderProfilePath + ".tmp");
            }
        }
    }

    public static int RenderMveFrames(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var store = new OriginalAssetStore();
            for (var movieNumber = 1; movieNumber <= 7; movieNumber++)
            {
                var movie = MveDecoder.Decode(store.ReadAllBytes($"MOVIES/MARIO{movieNumber}.MVE"));
                using var raster = new NativeRaster(movie.Width, movie.Height);
                var indices = new[]
                {
                    0,
                    movie.Frames.Count / 3,
                    movie.Frames.Count * 2 / 3,
                    movie.Frames.Count - 1,
                };
                foreach (var frameIndex in indices)
                {
                    var frame = movie.Frames[frameIndex];
                    raster.DrawIndexedImage(frame.Pixels, movie.Width, movie.Height, frame.Palette, 0, 0);
                    raster.Snapshot().Save(
                        Path.Combine(directory, $"mario{movieNumber}-{frameIndex:000}.png"),
                        System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    public static int RenderAvContactSheets(string directory)
    {
        const int columns = 4;
        const int rows = 4;
        const int thumbnailWidth = OriginalAvMovie.Width;
        const int thumbnailHeight = OriginalAvMovie.Height;
        const int labelHeight = 22;
        const int cellPadding = 6;
        var cellWidth = thumbnailWidth + cellPadding * 2;
        var cellHeight = thumbnailHeight + labelHeight + cellPadding * 2;

        try
        {
            Directory.CreateDirectory(directory);
            var store = new OriginalAssetStore();
            var moviePaths = store.Paths
                .Where(path => path.StartsWith("MOVIES/", StringComparison.OrdinalIgnoreCase) &&
                               path.EndsWith(".AV", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => Path.GetFileNameWithoutExtension(path)[0])
                .ThenBy(path => ParseMovieNumber(Path.GetFileNameWithoutExtension(path)))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var manifest = new List<string> { "file\tframes\tseconds" };

            for (var sheetStart = 0; sheetStart < moviePaths.Length; sheetStart += columns * rows)
            {
                using var sheet = new Bitmap(columns * cellWidth, rows * cellHeight);
                using var graphics = Graphics.FromImage(sheet);
                graphics.Clear(Color.FromArgb(24, 24, 32));
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                using var labelFont = new Font(FontFamily.GenericSansSerif, 8);

                var sheetEnd = Math.Min(moviePaths.Length, sheetStart + columns * rows);
                for (var index = sheetStart; index < sheetEnd; index++)
                {
                    var path = moviePaths[index];
                    var movie = AvDecoder.Decode(store.ReadAllBytes(path));
                    var frameIndex = movie.Frames.Count / 2;
                    using var raster = new NativeRaster(OriginalAvMovie.Width, OriginalAvMovie.Height);
                    raster.DrawIndexedImage(
                        movie.Frames[frameIndex],
                        OriginalAvMovie.Width,
                        OriginalAvMovie.Height,
                        movie.Palette,
                        0,
                        0);

                    var position = index - sheetStart;
                    var x = position % columns * cellWidth + cellPadding;
                    var y = position / columns * cellHeight + cellPadding;
                    graphics.DrawImageUnscaled(raster.Snapshot(), x, y);
                    var name = Path.GetFileName(path);
                    graphics.DrawString(
                        $"{name}  frame {frameIndex}/{movie.Frames.Count - 1}",
                        labelFont,
                        Brushes.White,
                        x,
                        y + thumbnailHeight + 2);
                    manifest.Add(
                        $"{name}\t{movie.Frames.Count}\t" +
                        $"{movie.Frames.Count / (double)OriginalAvMovie.FramesPerSecond:0.000}");
                }

                sheet.Save(
                    Path.Combine(directory, $"av-contact-{sheetStart / (columns * rows) + 1:00}.png"),
                    System.Drawing.Imaging.ImageFormat.Png);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            File.WriteAllLines(Path.Combine(directory, "av-manifest.tsv"), manifest);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int ParseMovieNumber(string name) =>
        int.TryParse(name.AsSpan(1), out var number) ? number : int.MaxValue;

    private static void Save(NativeGameEngine engine, string directory, string fileName)
    {
        engine.CurrentFrame.Save(Path.Combine(directory, fileName), System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void WriteReport(string path, bool success, List<string> checks, string? error)
    {
        var report = JsonSerializer.Serialize(new { success, checks, error }, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        File.WriteAllText(path, report);
    }
}
