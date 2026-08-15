using System.Diagnostics;
using System.Text;
using MarioTypingWindows.Original;

namespace MarioTypingWindows;

internal enum OriginalScreen
{
    Title,
    MainMenu,
    About,
    Help,
    Keyboard,
    StudentInformation,
    LoadStudent,
    LessonTimes,
    Certificate,
    Blackboard,
    Outside,
    Water,
    Castle,
    Practice,
    ReportCard,
    BonusAnimation,
    EndingAnimation,
    AvMovie,
    Movie,
}

internal enum OriginalCharacter
{
    Mario = 1,
    Luigi = 2,
    Princess = 3,
}

internal enum NativeDisplayMode
{
    Classic,
    Widescreen,
    UltraWidescreen,
}

internal enum MainMenuPopup
{
    None,
    File,
    Student,
    Lessons,
}

internal readonly record struct LessonProgressOutcome(
    int LessonLevel,
    int GoalWordsPerMinute,
    bool RewardMovieQualified,
    bool CertificateEarned);

internal readonly record struct OriginalHudState(
    string TimeText,
    string TypedText,
    string WordsPerMinuteText,
    string ErrorsText,
    int WordsPerMinuteX,
    int ErrorsX,
    int FirstRow,
    int SecondRow);

internal readonly record struct OriginalBlackboardState(
    string KeysTyped,
    string Errors,
    string WordsPerMinute,
    string GoalWordsPerMinute,
    string Accuracy,
    string LessonTime);

internal readonly record struct HelpLine(string Text, bool Heading);

internal readonly record struct OriginalHandElement(
    int SpriteNumber,
    bool RightHand,
    int XOffset,
    int YOffset);

internal enum GreetingStage
{
    Interplay,
    Zoom,
    ZoomHold,
    Move,
    Letters,
    FinalHold,
}

internal readonly record struct GreetingTimeline(
    GreetingStage Stage,
    int Frame,
    int LetterCount,
    VgaPalette Palette,
    bool Complete);

internal sealed record CertificatePrintRequest(string Student, string Level);

internal sealed class NativeGameEngine : IDisposable
{
    internal const int ClassicFrameWidth = 320;
    internal const int WidescreenFrameWidth = 427;
    internal const int UltraWidescreenFrameWidth = 560;
    internal const int FrameHeight = 200;
    private const int OriginalFrameMilliseconds = 55;
    private static readonly int[] OutsideJumpYOffsets =
        [0, -19, -26, -28, -28, -25, -20, -2, 0, 0, 0, 0];
    private static readonly int[] OutsideBrickYPositions = [57, 50, 42, 33];
    private static readonly int[] OutsideCursorShapeFrames = [0, 0, 1, 1, 2, 2, 3, 3, 2, 2, 1, 1];
    private static readonly byte[] OriginalHandElementTable = Convert.FromHexString(
        "0A282E2A2B2B31062F2E302E08060706060001020303090908072E0630062F2E" +
        "29282B2A2A2A2B2B313031302F31312F2E282B292B312B29293128060606312E" +
        "0000030202020303090809080709090706000301030903010109002E2E2E28");
    private static readonly int[] OriginalHandXOffsets = [0, 6, 15, 20, 26, 29, 23, 12, 4, 0];
    private static readonly int[] OriginalHandYOffsets = [9, 1, 0, 6, 19, 9, 1, 1, 6, 19];
    private static readonly int[] GreetingTextXPositions =
        [124, 138, 152, 169, 183, 198, 213, 233, 247, 262, 276, 282, 298];
    private const int GreetingPaletteWriteCount = 14;
    private const int GreetingFadeMilliseconds =
        GreetingPaletteWriteCount * 2 * 1000 / 70;
    private const int GreetingLogoHoldTicks = 57;
    private const int GreetingZoomGrowTicks = 20;
    private const int GreetingZoomHoldTicks = 19;
    private const int GreetingZoomMoveTicks = 6;
    private const int GreetingWordBreakTicks = 12;
    private const int GreetingFinalHoldTicks = 114;
    private static readonly int[] WaterOctopusYOffsets =
        [1, 2, 4, 7, 10, 13, 14, 16, 17, 19, 18, 16, 14, 12, 10, 8, 6, 4, 2, 1];
    private static readonly Point[] WaterAmbientBubblePositions =
    [
        new(44, 170), new(46, 186), new(55, 190), new(106, 180), new(127, 191),
        new(148, 183), new(182, 188), new(194, 179), new(232, 177), new(288, 182),
    ];
    private static readonly OriginalSprite WaterSmallBubbleSprite =
        new(0, 0, 2, 2, [0, 0, 0, 15]);
    private static readonly OriginalSprite WaterLargeBubbleSprite =
        new(0, 0, 3, 3, [0, 15, 0, 15, 0, 15, 0, 15, 0]);
    private static readonly int[] CastleRockXPositions = [53, 143, 233];
    private static readonly int[] CastleRockShapeFrames =
        [0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 4, 4, 4, 3, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
    private static readonly int[] CastleRockYOffsets =
        [1, 0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 55, 61, 61, 61, 61, 61, 61, 61, 61, 61, 59, 57, 56, 56, 56, 56, 57, 58, 59, 60, 61, 61];
    private static readonly int[] CastleFirstBuildXPositions = [226, 117, 134, 127, 66];
    private static readonly int[] CastleFirstBuildYPositions = [99, 95, 59, 30, 54];
    private static readonly int[] CastleSecondBuildXPositions = [128, 123, 156, 175, 179];
    private static readonly int[] CastleSecondBuildYPositions = [61, 109, 106, 80, 109];
    private static readonly int[] CastleBonusXPositions = [18, 118, 210];
    private static readonly int[] OutsideBonusSoundFrames = [7, 12, 15, 22, 24, 30, 33, 36, 38, 46, 51];
    private static readonly int[] WaterBonusSoundFrames = [3, 8, 13, 20, 21, 25, 28, 32, 35, 41, 46];
    private static readonly int[] CastleBonusSoundFrames = [4, 9, 12, 19, 21, 27, 30, 33, 35, 43, 48];
    private const int BonusAnimationFrameMilliseconds = OriginalFrameMilliseconds * 2;
    private const int CastleScrollFrames = 40;

    private sealed class OutsideTarget(
        int promptIndex,
        int kind,
        int x,
        int y,
        int spriteXOffset)
    {
        public int PromptIndex { get; } = promptIndex;
        public int Kind { get; } = kind;
        public int X { get; set; } = x;
        public int Y { get; } = y;
        public int SpriteXOffset { get; } = spriteXOffset;
        public bool HasCharacter { get; set; } = true;
        public int HitFramesRemaining { get; set; }
    }

    private sealed class OutsideParticle(int x, int y, int horizontalVelocity, int verticalVelocity)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int HorizontalVelocity { get; } = horizontalVelocity;
        public int VerticalVelocity { get; set; } = verticalVelocity;
    }

    private sealed class WaterBubble(int x, int y)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int HorizontalVelocity { get; set; }
        public int VerticalVelocity { get; set; }
        public bool Large { get; set; }
    }

    private static readonly string[] FileMenuItems =
    [
        "About     [A]", "Help     [F1]", "Demo      [D]", "Keyboard [F2]",
        "Music     [M]", "Sound F/X [S]", "M.I.R.T   [I]", "Rap       [R]",
        "Widescreen [F10]", "Ultra Wide [F9]", "Quit      [Q]",
    ];

    private static readonly string[] StudentMenuItems =
    [
        "New      [W]", "Load     [L]", "Edit     [E]", "Lesson Times", "Certificates",
    ];

    private static readonly string[] LessonMenuItems =
    [
        "Home Row Only", "Add Top Row", "Add Bottom Row", "Add Numbers",
        "Add Symbols", "All Keyboard",
    ];

    private static readonly string[] StudentInformationLessonNames =
    [
        "Home Row", "Home+Top Row", "Home+Top+Bottom", "Add Numbers",
        "Add Numbers+Symbols", "All Keys",
    ];

    private static readonly Point[] StudentInformationFocusPoints =
    [
        new(207, 74), new(154, 94), new(89, 157),
        new(154, 157), new(225, 157), new(292, 186),
    ];

    private static readonly Point[] LessonTimesFocusPoints =
    [
        new(202, 75), new(202, 95), new(202, 115), new(202, 135), new(285, 182),
    ];

    private static readonly Point[] CertificateFocusPoints =
    [
        new(160, 109), new(160, 117), new(160, 125), new(281, 181),
    ];

    private static readonly OriginalScreen[] CertificateWorlds =
    [
        OriginalScreen.Outside, OriginalScreen.Water, OriginalScreen.Castle,
    ];

    private static readonly string[] CertificateLevelNames =
    [
        "Outside", "Underwater", "Underground",
    ];

    private readonly OriginalAssetStore assets = new();
    private NativeRaster raster = new();
    private readonly NativeAudioPlayer audio = new();
    private readonly ProgressStore progress;
    private readonly bool cinematicsEnabled;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly CrxImage title;
    private readonly CrxImage titleBackground;
    private readonly CrxImage mainMenu;
    private readonly CrxImage outside;
    private readonly CrxImage outsideBonusBackdrop;
    private readonly CrxImage water;
    private readonly CrxImage castle;
    private readonly CrxImage practice;
    private readonly CrxImage reportCard;
    private readonly CrxImage help;
    private readonly CrxImage keyboard;
    private readonly CrxImage studentInformation;
    private readonly CrxImage loadStudent;
    private readonly CrxImage lessonTimes;
    private readonly CrxImage certificate;
    private readonly CrxImage ending;
    private readonly CrxImage logo;
    private readonly CrxImage blackboard;
    private readonly OriginalSpriteBank walking;
    private readonly OriginalSpriteBank greetingObjects;
    private readonly OriginalSpriteBank greetingUiObjects;
    private readonly OriginalSpriteBank outsideObjects;
    private readonly OriginalSpriteBank waterObjects;
    private readonly OriginalSpriteBank castleObjects;
    private readonly OriginalSpriteBank fingers;
    private readonly OriginalSpriteBank idle;
    private readonly OriginalSpriteBank practiceObjects;
    private readonly OriginalFont font3;
    private readonly OriginalFont font5;
    private readonly OriginalFont font7;
    private readonly OriginalFont font9;
    private readonly VgaPalette marioPalette;
    private readonly VgaPalette luigiPalette;
    private readonly VgaPalette princessPalette;
    private readonly VgaPalette turtlePalette;
    private readonly VgaPalette biterPalette;
    private readonly VgaPalette castlePalette;
    private readonly VgaPalette fingerPalette;
    private readonly VgaPalette[] outsidePalettes = new VgaPalette[4];
    private readonly VgaPalette[] outsideBonusPalettes = new VgaPalette[4];
    private readonly VgaPalette[] waterPalettes = new VgaPalette[4];
    private readonly VgaPalette[] castleScenePalettes = new VgaPalette[4];
    private readonly byte[][] castleBackgroundScenes;
    private OriginalAvMovie menuMovie;
    private VgaPalette menuMoviePalette;
    private bool[] menuMovieTransparentPaletteEntries;
    private readonly OriginalXmidiSequence[] musicSequences;
    private readonly OriginalXmidiSequence[][] soundEffectSequences;
    private int musicPlayRequestCount;
    private int lastMusicSequenceNumber;
    private readonly HelpLine[] helpLines;
    private readonly OriginalSprite[] titleZoomFrames;
    private readonly int[] titleLetterDelayTicks;
    private string prompt;
    private OriginalScreen screen = OriginalScreen.Title;
    private MainMenuPopup menuPopup;
    private readonly StringBuilder studentNameEntry = new();
    private readonly StringBuilder auxiliaryNumericEntry = new();
    private int menuSelection;
    private int auxiliarySelection;
    private int helpScroll;
    private int studentGoalDraft;
    private int studentLessonDraft;
    private OriginalCharacter studentCharacterDraft = OriginalCharacter.Mario;
    private int studentInformationEditField = -1;
    private string studentInformationEditName = string.Empty;
    private int studentInformationEditGoal;
    private bool newStudentNamePending;
    private int[] lessonTimesDraft = [2, 5, 5, 10];
    private int lessonTimeEditField = -1;
    private int lessonTimeEditOriginalValue;
    private string[] loadStudentNames = [];
    private int lastLoadStudentClick = -1;
    private long lastLoadStudentClickAt = long.MinValue;
    private OriginalScreen auxiliaryReturnScreen = OriginalScreen.MainMenu;
    private int selectedLesson;
    private LessonCategory activeLessonCategory = LessonCategory.HomeRow;
    private int position;
    private int errors;
    private readonly Dictionary<char, int> problemKeys = [];
    private long sceneStarted;
    private long titleAnimationStarted;
    private int titleLastSoundLetterCount;
    private int titleSoundEffectCount;
    private long typingStarted = -1;
    private long lessonCompleteAt = -1;
    private readonly Random outsideRandom = new();
    private readonly Random bonusRandom = new();
    private readonly Random waterRandom = new();
    private readonly List<OutsideTarget> outsideTargets = [];
    private readonly List<OutsideParticle> outsideParticles = [];
    private OutsideTarget? outsideSelectedTarget;
    private int outsideNextPromptIndex;
    private int outsideMarioX = 20;
    private int outsideMarioY = 90;
    private int outsideMarioPhase;
    private int outsideJumpFrame;
    private int outsideDisplayedJumpFrame = -1;
    private int outsideMarioJumpX;
    private int outsideForwardSpeed = 5;
    private int outsideRunCounter;
    private int outsideIdleFrames;
    private int outsideGroundOffset;
    private int outsideAnimationCounter;
    private long outsideLastStateUpdate;
    private int waterMarioX = 20;
    private int waterMarioY = 112;
    private int waterSwimFrame;
    private int waterSwimCounter;
    private int waterHorizontalDelay;
    private int waterPeakWordsPerMinute = 10;
    private int waterCreatureX;
    private int waterCreatureY;
    private int waterCreatureFrame;
    private int waterCreatureKind = 1;
    private int waterCreaturePreviousPlotX;
    private int waterCreatureDisplayX;
    private int waterCreatureDisplayY;
    private int waterCreatureDisplayFrame;
    private int waterCreatureDisplayKind = 1;
    private readonly List<WaterBubble> waterBubbles = [];
    private int waterFailurePhase;
    private bool waterLost;
    private long waterLastKeyAt;
    private long waterLastStateUpdate;
    private readonly int[] castleRockProgress = new int[3];
    private int castlePromptPosition;
    private int castleBatchStart;
    private int castleBatchEndExclusive;
    private int castleBatchCorrect;
    private int castleMarioX;
    private int castleMarioY = 92;
    private int castleTargetX = 67;
    private int castleVelocity;
    private int castleRunCounter;
    private int castleSequencePhase;
    private int castleSequenceFrame;
    private readonly int[] castleBuildDwellFrames = [4, 4, 4, 4, 4];
    private int castleConstructionFrames = 40;
    private int castleFailurePhase;
    private long castleLastKeyAt;
    private long castleLastStateUpdate;
    private int bonusFlag;
    private int bonusX;
    private int bonusY;
    private int bonusTimer;
    private int bonusMotionFrame;
    private char bonusCharacter;
    private long bonusLastSecondAt;
    private IReadOnlyList<byte[]>? activeBonusBackgroundFrames;
    private IReadOnlyList<OriginalAnimationFrame>? activeBonusCharacterFrames;
    private VgaPalette? activeBonusPalette;
    private OriginalScreen bonusAnimationDestination;
    private int[] activeBonusSoundFrames = [];
    private int bonusAnimationLastFrame = -1;
    private long bonusAnimationStarted;
    private IReadOnlyList<byte[]>? endingAnimationFrames;
    private long endingAnimationStarted;
    private int practicePromptPosition;
    private int practiceLineEndExclusive;
    private int practiceLineTyped;
    private string practiceLine = string.Empty;
    private long practiceLastCorrectAt;
    private SessionResult? lastResult;
    private int completedLessonLevel = 1;
    private bool rewardMovieQualified;
    private bool earnedCertificateThisLesson;
    private OriginalScreen lastCompletedWorld = OriginalScreen.Outside;
    private string? statusMessage;
    private bool rapPlaying;
    private bool demoAwaitingWorld;
    private bool demoMode;
    private bool suppressNextCharacter;
    private long demoNextKeyAt;
    private OriginalMveMovie? activeMovie;
    private OriginalScreen movieDestination;
    private OriginalAvMovie? activeAvMovie;
    private VgaPalette? activeAvPalette;
    private CrxImage? activeAvBackdrop;
    private OriginalScreen avMovieDestination;
    private Action? avMovieFinished;
    private int activeAvX;
    private int activeAvY;
    private bool debriefMoviePlayed;
    private bool menuMoviePlaying;
    private bool menuWelcomePlayed;
    private long menuMovieStarted;
    private long menuIdleMovieDue;

    public NativeGameEngine(
        bool enableAudio = true,
        bool enableCinematics = true,
        string? progressFilePath = null)
    {
        audio.Enabled = enableAudio;
        cinematicsEnabled = enableCinematics;
        progress = ProgressStore.Load(progressFilePath);
        if (enableAudio)
        {
            audio.MusicEnabled = progress.MusicEnabled;
            audio.SoundEffectsEnabled = progress.SoundEffectsEnabled;
            audio.SpeechEnabled = progress.SpeechEnabled;
        }
        selectedLesson = progress.LessonLevel - 1;
        lastCompletedWorld = progress.CurrentWorld;
        title = LoadCrx("GREET.CRX");
        titleBackground = LoadCrx("GREETBG.CRX");
        mainMenu = LoadCrx("MAINMENU.CRX");
        outside = LoadCrx("OUTSIDE.CRX");
        outsideBonusBackdrop = LoadCrx("OUTSKY.CRX");
        water = LoadCrx("WATER.CRX");
        castle = LoadCrx("CASTLE.CRX");
        castleBackgroundScenes = CastleBackgroundDecoder.Decode(
                assets.ReadAllBytes("CASTLE.BGS"),
                castle.Pixels)
            .Select(pixels => pixels)
            .ToArray();
        practice = LoadCrx("PRACTICE.CRX");
        reportCard = LoadCrx("RCARD.CRX");
        help = LoadCrx("HELP.CRX");
        keyboard = LoadCrx("KEYBOARD.CRX");
        studentInformation = LoadCrx("PREFS.CRX");
        loadStudent = LoadCrx("LOAD.CRX");
        lessonTimes = LoadCrx("LESSTIME.CRX");
        certificate = LoadCrx("CERTIF.CRX");
        ending = LoadCrx("ENDF.CRX");
        logo = LoadCrx("LOGO.CRX");
        blackboard = LoadCrx("BLACKBRD.CRX");
        helpLines = ParseHelp(assets.ReadAllText("MARIO1.HLP"));

        walking = LoadGso("WALKING.GSO", "WALKING.GSK");
        greetingObjects = LoadGso("GREET.GSO", "GREET.GSK");
        greetingUiObjects = LoadGso("GREET2.GSO", "GREET2.GSK");
        titleZoomFrames = CreateGreetingZoomFrames(greetingObjects["lmario_1"]);
        titleLetterDelayTicks = CreateGreetingLetterDelayTicks();
        outsideObjects = LoadGso("OUTSIDE.GSO", "OUTSIDE.GSK");
        waterObjects = LoadGso("WATER.GSO", "WATER.GSK");
        castleObjects = LoadGso("CASTLE.GSO", "CASTLE.GSK");
        fingers = LoadGso("FINGER.GSO", "FINGER.GSK");
        idle = LoadGco("IDLE.GCO", "IDLE.GCK");
        practiceObjects = LoadGso("PRACTICE.GSO", "PRACTICE.GSK");

        font3 = new OriginalFont(LoadGso("FONT3I.GSO", "FONT3I.GSK"), "font3_", 8, 10);
        font5 = new OriginalFont(LoadGso("FONT5I.GSO", "FONT5I.GSK"), "font5_", 8, 8);
        font7 = new OriginalFont(LoadGso("FONT7I.GSO", "FONT7I.GSK"), "font7_", 10, 10);
        font9 = new OriginalFont(LoadGso("FONT9I.GSO", "FONT9I.GSK"), "font9_", 8, 13);

        marioPalette = LoadPalette("MPALLET.GPO");
        luigiPalette = LoadPalette("LPALLET.GPO");
        princessPalette = LoadPalette("PPALLET.GPO");
        turtlePalette = LoadPalette("TURTLE.GPO");
        biterPalette = LoadPalette("BITER.GPO");
        castlePalette = LoadPalette("CPALLET.GPO");
        fingerPalette = LoadPalette("FINGER.GPO");

        var characterPalettes = new[]
        {
            marioPalette,
            marioPalette,
            luigiPalette,
            princessPalette,
        };
        foreach (var character in Enum.GetValues<OriginalCharacter>())
        {
            var characterPalette = characterPalettes[(int)character];
            outsidePalettes[(int)character] = outside.Palette
                .WithRange(characterPalette, 32, 16)
                .WithRange(turtlePalette, 48, 16);
            outsideBonusPalettes[(int)character] = outsideBonusBackdrop.Palette
                .WithRange(characterPalette, 32, 16);
            waterPalettes[(int)character] = water.Palette
                .WithRange(characterPalette, 32, 16)
                .WithRange(biterPalette, 48, 16)
                .WithRange(biterPalette, 112, 8);
            castleScenePalettes[(int)character] = castle.Palette.WithRange(characterPalette, 32, 16);
        }

        menuMovie = AvDecoder.Decode(assets.ReadAllBytes("MOVIES/Z8.AV"));
        // The DOS callback writes only VGA entries 44..255 so the menu/UI
        // colors in the lower palette remain unchanged during AV playback.
        menuMoviePalette = mainMenu.Palette.WithRange(menuMovie.Palette, 44, 212);
        menuMovieTransparentPaletteEntries = CreateMenuMovieTransparency(menuMoviePalette);
        musicSequences = Enumerable.Range(1, 4)
            .Select(number => XmidiDecoder.Decode(assets.ReadAllBytes($"RTYP{number}.XMI"))[0])
            .ToArray();
        soundEffectSequences = Enumerable.Range(1, 10)
            .Select(number => XmidiDecoder.Decode(assets.ReadAllBytes($"SFX{number:00}.RLD")).ToArray())
            .ToArray();

        // MARCD.EXE _main pushes song 1 immediately before calling MAINMENU,
        // so the original outdoor theme begins under the greeting/title and
        // continues into the menu.
        PlayMusicSequence(0);

        // This is the original embedded lesson corpus, not replacement prose.
        prompt = LessonLibrary.CreatePrompt(LessonCategory.HomeRow, ActivityMode.LetterDrill, 0);
        titleAnimationStarted = clock.ElapsedMilliseconds;
        Render();
    }

    public event EventHandler? FrameChanged;

    public event EventHandler? DisplayModeChanged;

    public event EventHandler? ExitRequested;

    public event Action<CertificatePrintRequest>? CertificatePrintRequested;

    public event Action<Point>? StudentInformationCursorRequested;

    public event Action<Point>? LessonTimesCursorRequested;

    public event Action<Point>? CertificateCursorRequested;

    public OriginalScreen Screen => screen;

    internal int GoalWordsPerMinute => progress.GoalWordsPerMinute;

    internal int StudentInformationSelection => auxiliarySelection;

    internal int AuxiliarySelection => auxiliarySelection;

    internal MainMenuPopup ActiveMenuPopup => menuPopup;

    internal int MenuSelection => menuSelection;

    internal int ActiveMenuItemCount => PopupItems(menuPopup).Length;

    internal int StudentInformationEditField => studentInformationEditField;

    internal int LessonTimeEditField => lessonTimeEditField;

    internal string StudentName => progress.StudentName;

    internal int LessonLevel => progress.LessonLevel;

    internal OriginalCharacter StudentCharacter => progress.Character;

    internal OriginalScreen CurrentWorld => progress.CurrentWorld;

    internal IReadOnlyList<int> LessonTimesMinutes => progress.LessonTimesMinutes;

    internal bool MusicEnabled => audio.MusicEnabled;

    internal bool SoundEffectsEnabled => audio.SoundEffectsEnabled;

    internal bool SpeechEnabled => audio.SpeechEnabled;

    internal int MusicPlayRequestCount => musicPlayRequestCount;

    internal int LastMusicSequenceNumber => lastMusicSequenceNumber;

    internal NativeDisplayMode DisplayMode => progress.DisplayMode;

    internal bool WidescreenEnabled => progress.DisplayMode == NativeDisplayMode.Widescreen;

    internal bool UltraWidescreenEnabled =>
        progress.DisplayMode == NativeDisplayMode.UltraWidescreen;

    internal int FrameWidth => raster.Width;

    internal int OriginalContentOffsetX => CenteredContentOffsetX;

    internal int MainMenuCardHitX => MainMenuCardStripX + 18;

    internal int OutsideTargetCount => outsideTargets.Count;

    internal int OutsideFarthestTargetX =>
        outsideTargets.Count == 0 ? int.MinValue : outsideTargets.Max(target => target.X);

    internal int SelectedLesson => selectedLesson;

    internal char? ExpectedCharacter => CurrentExpectedCharacter();

    internal bool PracticeAwaitingLineAdvance =>
        screen == OriginalScreen.Practice && practiceLineTyped >= practiceLine.Length;

    internal int PracticeLineLength =>
        screen == OriginalScreen.Practice ? practiceLine.Length : 0;

    internal int PracticeColumnCount => PracticeTextColumns;

    internal char? ActiveBonusCharacter =>
        screen is (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle) && bonusFlag == 2
            ? bonusCharacter
            : null;

    internal int CastleSequencePhase => castleSequencePhase;

    internal int CastleSequenceFrame => castleSequenceFrame;

    internal int CastleConstructionFrameCount => castleConstructionFrames;

    internal IReadOnlyList<int> CastleConstructionDwellFrames => castleBuildDwellFrames;

    internal int CorrectCharacterCount => position;

    internal OriginalBlackboardState BlackboardState => BuildOriginalBlackboardState(
        lastResult ?? progress.Results.LastOrDefault(),
        progress.GoalWordsPerMinute);

    internal int OutsideHitFramesRemaining =>
        outsideTargets.FirstOrDefault(target => target.HitFramesRemaining > 0)?.HitFramesRemaining ?? 0;

    internal int OutsideParticleCount => outsideParticles.Count;

    internal int OutsideCursorFrame => OriginalOutsideCursorFrame(outsideAnimationCounter);

    internal int OutsideCurrentForwardSpeed => outsideForwardSpeed;

    internal int OutsideCurrentGroundOffset => outsideGroundOffset;

    internal int WaterCreatureStateKind => waterCreatureKind;

    internal int WaterCreatureRenderedKind => waterCreatureDisplayKind;

    internal int WaterCreatureRenderedFrame => waterCreatureDisplayFrame;

    internal int WaterFailurePhase => waterFailurePhase;

    internal bool WaterFailureComplete => waterLost;

    internal int WaterBubbleCount => waterBubbles.Count;

    internal int ActiveBonusAnimationFrameCount =>
        Math.Max(activeBonusBackgroundFrames?.Count ?? 0, activeBonusCharacterFrames?.Count ?? 0);

    internal int TitleSoundEffectCount => titleSoundEffectCount;

    internal IReadOnlyList<int> TitleLetterDelayTicks => titleLetterDelayTicks;

    internal long TitleAllLettersVisibleMilliseconds =>
        GreetingPreLetterMilliseconds +
        (titleLetterDelayTicks.Take(12).Sum() + GreetingWordBreakTicks) *
        OriginalFrameMilliseconds;

    private static long GreetingPreLetterMilliseconds =>
        GreetingFadeMilliseconds * 3L +
        (GreetingLogoHoldTicks + GreetingZoomGrowTicks + GreetingZoomHoldTicks +
         GreetingZoomMoveTicks) * OriginalFrameMilliseconds;

    internal bool ActiveBonusUsesOutsideSky =>
        screen == OriginalScreen.BonusAnimation &&
        bonusAnimationDestination == OriginalScreen.Outside &&
        activeBonusPalette is not null &&
        activeBonusPalette.Argb.SequenceEqual(
            outsideBonusPalettes[(int)progress.Character].Argb);

    public Bitmap CurrentFrame => raster.Snapshot();

    public void Tick()
    {
        if (screen == OriginalScreen.Title)
        {
            UpdateTitleAnimation();
            return;
        }

        if (screen == OriginalScreen.EndingAnimation)
        {
            UpdateEndingAnimation();
            return;
        }

        if (screen == OriginalScreen.BonusAnimation)
        {
            UpdateBonusAnimation();
            return;
        }

        if (screen == OriginalScreen.AvMovie)
        {
            if (activeAvMovie is null)
            {
                FinishAvMovie();
                return;
            }
            var elapsedFrames = Math.Max(0, clock.ElapsedMilliseconds - sceneStarted) *
                                OriginalAvMovie.FramesPerSecond / 1000;
            if (elapsedFrames >= activeAvMovie.Frames.Count)
            {
                FinishAvMovie();
                return;
            }
            RenderAndNotify();
            return;
        }

        if (screen == OriginalScreen.Movie)
        {
            if (activeMovie is null)
            {
                FinishMovie();
                return;
            }
            var elapsedMicroseconds = Math.Max(0, clock.ElapsedMilliseconds - sceneStarted) * 1000;
            if (elapsedMicroseconds >= activeMovie.FrameDurationMicroseconds * activeMovie.Frames.Count)
            {
                FinishMovie();
                return;
            }
            RenderAndNotify();
            return;
        }

        if (screen == OriginalScreen.MainMenu)
        {
            UpdateMainMenuMovie();
        }

        if (screen == OriginalScreen.Outside)
        {
            UpdateOutsideState(clock.ElapsedMilliseconds);
        }
        else if (screen == OriginalScreen.Water)
        {
            UpdateWaterState(clock.ElapsedMilliseconds);
            if (waterLost && lessonCompleteAt < 0)
            {
                CompleteLesson();
                RenderAndNotify();
                return;
            }
        }
        else if (screen == OriginalScreen.Castle)
        {
            UpdateCastleState(clock.ElapsedMilliseconds);
            if (castleFailurePhase == 2 && lessonCompleteAt < 0)
            {
                CompleteLesson();
                RenderAndNotify();
                return;
            }
        }

        if (typingStarted >= 0 &&
            lessonCompleteAt < 0 &&
            screen is OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice &&
            OriginalElapsedSeconds(OriginalElapsedTicks(clock.ElapsedMilliseconds - typingStarted)) >=
                CurrentLessonTimeMinutes() * 60)
        {
            CompleteLesson();
            RenderAndNotify();
            return;
        }

        if (demoMode &&
            clock.ElapsedMilliseconds >= demoNextKeyAt &&
            screen is OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice &&
            ExpectedCharacter is { } demoCharacter &&
            lessonCompleteAt < 0)
        {
            demoNextKeyAt = clock.ElapsedMilliseconds + 240;
            HandleCharacter(demoCharacter);
            return;
        }

        if (lessonCompleteAt >= 0 &&
            screen is OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice &&
            ((screen == OriginalScreen.Outside &&
              outsideMarioPhase == 3 &&
              clock.ElapsedMilliseconds - lessonCompleteAt >= OriginalFrameMilliseconds) ||
             (screen != OriginalScreen.Outside &&
              clock.ElapsedMilliseconds - lessonCompleteAt >= 1100)))
        {
            StartClosingMovieOrReport();
            return;
        }
        Render();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    public void HandleKeyDown(Keys key)
    {
        NoteMainMenuActivity();
        if (screen == OriginalScreen.EndingAnimation)
        {
            FinishEndingAnimation();
            return;
        }
        if (screen == OriginalScreen.BonusAnimation)
        {
            return;
        }
        if (screen == OriginalScreen.AvMovie)
        {
            FinishAvMovie();
            return;
        }

        if (screen == OriginalScreen.Movie)
        {
            FinishMovie();
            return;
        }

        if (rapPlaying)
        {
            rapPlaying = false;
            audio.Stop();
            ResumeWorldMusic();
            ResumeMainMenuMovieAudio();
            RenderAndNotify();
            return;
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            if (demoAwaitingWorld)
            {
                var demoWorld = key switch
                {
                    Keys.D1 or Keys.NumPad1 => OriginalScreen.Outside,
                    Keys.D2 or Keys.NumPad2 => OriginalScreen.Water,
                    Keys.D3 or Keys.NumPad3 => OriginalScreen.Castle,
                    Keys.D4 or Keys.NumPad4 => OriginalScreen.Practice,
                    _ => (OriginalScreen?)null,
                };
                if (demoWorld.HasValue)
                {
                    statusMessage = null;
                    demoAwaitingWorld = false;
                    demoMode = true;
                    demoNextKeyAt = clock.ElapsedMilliseconds + 400;
                    Show(demoWorld.Value);
                    return;
                }
            }
            statusMessage = null;
            demoAwaitingWorld = false;
            RenderAndNotify();
            return;
        }

        if (key == Keys.Escape)
        {
            if (screen == OriginalScreen.StudentInformation)
            {
                if (studentInformationEditField >= 0)
                {
                    if (newStudentNamePending)
                    {
                        newStudentNamePending = false;
                        studentInformationEditField = -1;
                        Show(OriginalScreen.MainMenu);
                    }
                    else
                    {
                        CancelStudentInformationEdit();
                    }
                }
                else
                {
                    CommitStudentInformation();
                }
                return;
            }
            if (screen == OriginalScreen.LessonTimes && lessonTimeEditField >= 0)
            {
                CancelLessonTimeEdit();
                return;
            }
            if (screen == OriginalScreen.LessonTimes)
            {
                CommitLessonTimes();
                return;
            }
            if (screen == OriginalScreen.MainMenu && menuPopup != MainMenuPopup.None)
            {
                menuPopup = MainMenuPopup.None;
                menuSelection = 0;
                RenderAndNotify();
                return;
            }
            if (screen == OriginalScreen.LoadStudent && auxiliaryReturnScreen != OriginalScreen.MainMenu)
            {
                Show(auxiliaryReturnScreen);
                return;
            }
            if (screen == OriginalScreen.MainMenu || screen == OriginalScreen.Title)
            {
                return;
            }
            Show(OriginalScreen.MainMenu);
            return;
        }

        if (screen == OriginalScreen.Title)
        {
            Show(OriginalScreen.MainMenu);
            return;
        }

        if (screen == OriginalScreen.Practice &&
            key == Keys.Enter &&
            practiceLineTyped >= practiceLine.Length)
        {
            HandleCharacter(' ');
            return;
        }

        if (HandleAuxiliaryKey(key))
        {
            return;
        }

        if (screen == OriginalScreen.MainMenu)
        {
            if (menuPopup != MainMenuPopup.None)
            {
                if (TryActivateMenuShortcut(menuPopup, key))
                {
                    return;
                }
                var itemCount = PopupItems(menuPopup).Length;
                if (key == Keys.Down)
                {
                    menuSelection = (menuSelection + 1) % itemCount;
                    RenderAndNotify();
                }
                else if (key == Keys.Up)
                {
                    menuSelection = (menuSelection + itemCount - 1) % itemCount;
                    RenderAndNotify();
                }
                else if (key == Keys.Enter)
                {
                    ActivateMenuItem(menuPopup, menuSelection);
                }
                return;
            }

            switch (key)
            {
                case Keys.D1:
                case Keys.NumPad1:
                    suppressNextCharacter = true;
                    StartWorld(OriginalScreen.Outside);
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    suppressNextCharacter = true;
                    StartWorld(OriginalScreen.Water);
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    suppressNextCharacter = true;
                    StartWorld(OriginalScreen.Castle);
                    break;
                case Keys.D4:
                case Keys.NumPad4:
                    suppressNextCharacter = true;
                    StartPractice();
                    break;
                case Keys.D5:
                case Keys.NumPad5:
                    Show(OriginalScreen.ReportCard);
                    break;
                case Keys.D6:
                case Keys.NumPad6:
                    suppressNextCharacter = true;
                    StartNextLesson();
                    break;
                case Keys.W:
                    suppressNextCharacter = true;
                    OpenNewStudentInformation();
                    break;
                case Keys.A:
                    Show(OriginalScreen.About);
                    break;
                case Keys.F1:
                case Keys.H:
                    OpenHelp();
                    break;
                case Keys.D:
                    ShowDemoInstructions();
                    break;
                case Keys.F2:
                case Keys.K:
                    Show(OriginalScreen.Keyboard);
                    break;
                case Keys.L:
                    OpenLoadStudent();
                    break;
                case Keys.E:
                    suppressNextCharacter = true;
                    OpenStudentInformation();
                    break;
                case Keys.T:
                    OpenLessonTimes();
                    break;
                case Keys.C:
                    Show(OriginalScreen.Certificate);
                    break;
                case Keys.M:
                    ToggleMusic();
                    break;
                case Keys.S:
                    ToggleSoundEffects();
                    break;
                case Keys.I:
                    ToggleSpeech();
                    break;
                case Keys.R:
                    PlayRap();
                    break;
                case Keys.F10:
                    ToggleWidescreen();
                    break;
                case Keys.F9:
                    ToggleUltraWidescreen();
                    break;
                case Keys.Q:
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.N:
                    suppressNextCharacter = true;
                    StartNextLesson();
                    break;
            }
        }
    }

    public void HandleCharacter(char character)
    {
        if (suppressNextCharacter)
        {
            suppressNextCharacter = false;
            return;
        }

        if (screen == OriginalScreen.StudentInformation)
        {
            if (studentInformationEditField == 0 &&
                !char.IsControl(character) &&
                studentNameEntry.Length < 8)
            {
                studentNameEntry.Append(character);
                RenderAndNotify();
            }
            else if (studentInformationEditField == 1 && char.IsAsciiDigit(character))
            {
                AppendNumericDigit(character, value => studentGoalDraft = value, 150);
            }
            else if (studentInformationEditField < 0 &&
                     auxiliarySelection == 1 &&
                     char.IsAsciiDigit(character))
            {
                BeginStudentInformationEdit(1);
                AppendNumericDigit(character, value => studentGoalDraft = value, 150);
            }
            return;
        }

        if (screen == OriginalScreen.LessonTimes && char.IsAsciiDigit(character))
        {
            if (lessonTimeEditField < 0 && auxiliarySelection < lessonTimesDraft.Length)
            {
                BeginLessonTimeEdit(auxiliarySelection);
            }
            if (lessonTimeEditField < 0)
            {
                return;
            }
            AppendNumericDigit(
                character,
                value => lessonTimesDraft[lessonTimeEditField] = value,
                60);
            return;
        }

        if (screen is not (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice)
            || prompt.Length == 0
            || lessonCompleteAt >= 0
            || char.IsControl(character)
            || (screen == OriginalScreen.Castle &&
                (castleFailurePhase != 0 || castleSequencePhase is 1 or 3)))
        {
            return;
        }

        if (screen is OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle &&
            bonusFlag == 2 && character == bonusCharacter)
        {
            StartBonusAnimation();
            return;
        }

        var expected = CurrentExpectedCharacter();
        if (!expected.HasValue)
        {
            return;
        }
        if (screen == OriginalScreen.Water)
        {
            waterLastKeyAt = clock.ElapsedMilliseconds;
        }
        else if (screen == OriginalScreen.Castle)
        {
            castleLastKeyAt = clock.ElapsedMilliseconds;
        }
        typingStarted = typingStarted < 0 ? clock.ElapsedMilliseconds : typingStarted;
        if (character == expected.Value)
        {
            position++;
            if (screen == OriginalScreen.Castle)
            {
                castlePromptPosition++;
                castleBatchCorrect++;
                castleTargetX = OriginalCastleTargetX(
                    castleBatchCorrect,
                    castleBatchEndExclusive - castleBatchStart);
                if (castlePromptPosition >= castleBatchEndExclusive)
                {
                    BeginCastleConstruction();
                }
            }
            else if (screen == OriginalScreen.Practice)
            {
                if (practiceLineTyped < practiceLine.Length)
                {
                    practiceLineTyped++;
                    practicePromptPosition++;
                }
                else
                {
                    BeginNextPracticeLine();
                }
                practiceLastCorrectAt = clock.ElapsedMilliseconds;
            }
            if (screen == OriginalScreen.Outside && outsideSelectedTarget is { HasCharacter: true } target)
            {
                target.HasCharacter = false;
                target.HitFramesRemaining = 6;
                if (target.Kind == 1 && outsideMarioPhase == 0)
                {
                    outsideMarioPhase = 1;
                    outsideJumpFrame = 0;
                    outsideDisplayedJumpFrame = 0;
                    outsideMarioY = 90;
                }
            }
            PlaySoundEffect(1);
        }
        else
        {
            errors++;
            problemKeys[expected.Value] = problemKeys.GetValueOrDefault(expected.Value) + 1;
            PlaySoundEffect(11);
        }
        Render();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    public void HandleClick(int x, int y)
    {
        NoteMainMenuActivity();
        if (screen == OriginalScreen.EndingAnimation)
        {
            FinishEndingAnimation();
            return;
        }
        if (screen == OriginalScreen.BonusAnimation)
        {
            return;
        }
        if (screen == OriginalScreen.AvMovie)
        {
            FinishAvMovie();
            return;
        }

        if (screen == OriginalScreen.Movie)
        {
            FinishMovie();
            return;
        }

        if (screen is not (
                OriginalScreen.MainMenu or
                OriginalScreen.Outside or
                OriginalScreen.Water or
                OriginalScreen.Castle))
        {
            x -= CenteredContentOffsetX;
        }

        if (rapPlaying)
        {
            rapPlaying = false;
            audio.Stop();
            ResumeWorldMusic();
            ResumeMainMenuMovieAudio();
            RenderAndNotify();
            return;
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            if (demoAwaitingWorld)
            {
                var demoWorld = MainMenuWorldAt(x, y);
                if (demoWorld.HasValue)
                {
                    statusMessage = null;
                    demoAwaitingWorld = false;
                    demoMode = true;
                    demoNextKeyAt = clock.ElapsedMilliseconds + 400;
                    Show(demoWorld.Value);
                    return;
                }
            }
            statusMessage = null;
            demoAwaitingWorld = false;
            RenderAndNotify();
            return;
        }

        if (screen == OriginalScreen.Title)
        {
            Show(OriginalScreen.MainMenu);
            return;
        }

        if (HandleAuxiliaryClick(x, y))
        {
            return;
        }

        if (screen != OriginalScreen.MainMenu)
        {
            return;
        }

        if (y < 9)
        {
            var studentHeaderLeft = raster.Width == ClassicFrameWidth
                ? 104
                : (raster.Width - 104) / 2;
            var lessonsHeaderLeft = raster.Width == ClassicFrameWidth
                ? 224
                : raster.Width - 96;
            if (x < 72)
            {
                OpenMenu(MainMenuPopup.File);
            }
            else if (x >= studentHeaderLeft && x < studentHeaderLeft + 104)
            {
                OpenMenu(MainMenuPopup.Student);
            }
            else if (x >= lessonsHeaderLeft)
            {
                OpenMenu(MainMenuPopup.Lessons);
            }
            return;
        }

        if (menuPopup != MainMenuPopup.None)
        {
            var (left, top, width, _) = PopupBounds(menuPopup);
            var items = PopupItems(menuPopup);
            if (x >= left && x < left + width && y >= top && y < top + items.Length * 9)
            {
                ActivateMenuItem(menuPopup, Math.Clamp((y - top) / 9, 0, items.Length - 1));
            }
            else
            {
                menuPopup = MainMenuPopup.None;
                RenderAndNotify();
            }
            return;
        }

        if (x < MainMenuCardStripX + 4 || x >= MainMenuCardStripX + 67)
        {
            return;
        }

        if (y is >= 12 and < 39)
        {
            StartWorld(OriginalScreen.Outside);
        }
        else if (y is >= 43 and < 70)
        {
            StartWorld(OriginalScreen.Water);
        }
        else if (y is >= 74 and < 101)
        {
            StartWorld(OriginalScreen.Castle);
        }
        else if (y is >= 105 and < 132)
        {
            StartPractice();
        }
        else if (y is >= 136 and < 163)
        {
            Show(OriginalScreen.ReportCard);
        }
        else if (y is >= 167 and < 183)
        {
            StartNextLesson();
        }
    }

    public void HandleMouseWheel(int delta)
    {
        var direction = Math.Sign(delta);
        if (direction == 0)
        {
            return;
        }
        auxiliaryNumericEntry.Clear();
        if (screen == OriginalScreen.Help)
        {
            var maximumScroll = Math.Max(0, helpLines.Length - 17);
            helpScroll = Math.Clamp(helpScroll - direction * 3, 0, maximumScroll);
            RenderAndNotify();
        }
        else if (screen == OriginalScreen.LoadStudent && loadStudentNames.Length > 0)
        {
            auxiliarySelection = Math.Clamp(
                auxiliarySelection - direction,
                0,
                loadStudentNames.Length - 1);
            RenderAndNotify();
        }
        else if (screen == OriginalScreen.StudentInformation &&
                 (studentInformationEditField == 1 ||
                  (studentInformationEditField < 0 && auxiliarySelection == 1)))
        {
            if (studentInformationEditField < 0)
            {
                BeginStudentInformationEdit(1);
            }
            AdjustStudentGoal(direction);
        }
        else if (screen == OriginalScreen.LessonTimes &&
                 (lessonTimeEditField >= 0 || auxiliarySelection < lessonTimesDraft.Length))
        {
            if (lessonTimeEditField < 0)
            {
                BeginLessonTimeEdit(auxiliarySelection);
            }
            AdjustLessonTime(direction);
        }
    }

    private bool HandleAuxiliaryKey(Keys key)
    {
        switch (screen)
        {
            case OriginalScreen.About:
            case OriginalScreen.Keyboard:
                Show(OriginalScreen.MainMenu);
                return true;
            case OriginalScreen.Help:
            {
                var maximumScroll = Math.Max(0, helpLines.Length - 17);
                helpScroll = key switch
                {
                    Keys.Down => Math.Min(maximumScroll, helpScroll + 1),
                    Keys.Up => Math.Max(0, helpScroll - 1),
                    Keys.PageDown => Math.Min(maximumScroll, helpScroll + 15),
                    Keys.PageUp => Math.Max(0, helpScroll - 15),
                    Keys.Home => 0,
                    Keys.End => maximumScroll,
                    _ => helpScroll,
                };
                if (key == Keys.Enter)
                {
                    Show(OriginalScreen.MainMenu);
                }
                else
                {
                    RenderAndNotify();
                }
                return true;
            }
            case OriginalScreen.StudentInformation:
                if (studentInformationEditField >= 0)
                {
                    if (key == Keys.Enter)
                    {
                        FinishStudentInformationEdit();
                    }
                    else if (key == Keys.Back &&
                             studentInformationEditField == 0 &&
                             studentNameEntry.Length > 0)
                    {
                        studentNameEntry.Length--;
                        RenderAndNotify();
                    }
                    else if (key == Keys.Back && studentInformationEditField == 1)
                    {
                        BackspaceNumericEntry(value => studentGoalDraft = value, 5);
                    }
                    else if (studentInformationEditField == 1 &&
                             TryNumericAdjustment(key, 5, 150, studentGoalDraft, out var goal))
                    {
                        auxiliaryNumericEntry.Clear();
                        studentGoalDraft = goal;
                        RenderAndNotify();
                    }
                    return true;
                }
                if (key is Keys.Down or Keys.Right or Keys.Tab)
                {
                    MoveStudentInformationSelection(1);
                    return true;
                }
                else if (key is Keys.Up or Keys.Left)
                {
                    MoveStudentInformationSelection(-1);
                    return true;
                }
                else if (key == Keys.Enter)
                {
                    switch (auxiliarySelection)
                    {
                        case 0:
                        case 1:
                            BeginStudentInformationEdit(auxiliarySelection);
                            return true;
                        case 2:
                        case 3:
                        case 4:
                            studentCharacterDraft = (OriginalCharacter)(auxiliarySelection - 1);
                            break;
                        case 5:
                            CommitStudentInformation();
                            return true;
                    }
                }
                else if (key == Keys.D)
                {
                    CommitStudentInformation();
                    return true;
                }
                RenderAndNotify();
                return true;
            case OriginalScreen.LoadStudent:
                if (key == Keys.Down && loadStudentNames.Length > 0)
                {
                    auxiliarySelection = auxiliarySelection + 1 < loadStudentNames.Length
                        ? auxiliarySelection + 1
                        : 0;
                    RenderAndNotify();
                }
                else if (key == Keys.Up && loadStudentNames.Length > 0)
                {
                    auxiliarySelection = auxiliarySelection > 0
                        ? auxiliarySelection - 1
                        : loadStudentNames.Length - 1;
                    RenderAndNotify();
                }
                else if (key == Keys.Right && loadStudentNames.Length > 0)
                {
                    if (auxiliarySelection + 10 < loadStudentNames.Length)
                    {
                        auxiliarySelection += 10;
                        RenderAndNotify();
                    }
                }
                else if (key == Keys.Left && loadStudentNames.Length > 0)
                {
                    if (auxiliarySelection >= 10)
                    {
                        auxiliarySelection -= 10;
                        RenderAndNotify();
                    }
                }
                else if (key is Keys.Enter or Keys.D)
                {
                    CommitLoadStudent();
                }
                else if (key == Keys.C)
                {
                    Show(auxiliaryReturnScreen);
                }
                return true;
            case OriginalScreen.LessonTimes:
                if (lessonTimeEditField >= 0)
                {
                    if (key == Keys.Enter)
                    {
                        FinishLessonTimeEdit();
                    }
                    else if (key == Keys.Back)
                    {
                        BackspaceNumericEntry(
                            value => lessonTimesDraft[lessonTimeEditField] = value,
                            1);
                    }
                    else if (TryNumericAdjustment(
                                 key,
                                 1,
                                 60,
                                 lessonTimesDraft[lessonTimeEditField],
                                 out var minutes))
                    {
                        auxiliaryNumericEntry.Clear();
                        lessonTimesDraft[lessonTimeEditField] = minutes;
                        RenderAndNotify();
                    }
                    return true;
                }
                if (key is Keys.Down or Keys.Right or Keys.Tab)
                {
                    MoveLessonTimesSelection(1);
                    return true;
                }
                if (key is Keys.Up or Keys.Left)
                {
                    MoveLessonTimesSelection(-1);
                    return true;
                }
                if (key == Keys.Enter)
                {
                    if (auxiliarySelection < lessonTimesDraft.Length)
                    {
                        BeginLessonTimeEdit(auxiliarySelection);
                    }
                    else
                    {
                        CommitLessonTimes();
                    }
                    return true;
                }
                if (key == Keys.D)
                {
                    CommitLessonTimes();
                    return true;
                }
                return true;
            case OriginalScreen.ReportCard:
                suppressNextCharacter = key is >= Keys.A and <= Keys.Z or
                    >= Keys.D0 and <= Keys.D9 or >= Keys.NumPad0 and <= Keys.NumPad9;
                Show(OriginalScreen.MainMenu);
                return true;
            case OriginalScreen.Certificate:
            {
                if (!HasAnyCertificate())
                {
                    suppressNextCharacter = key is >= Keys.A and <= Keys.Z or
                        >= Keys.D0 and <= Keys.D9 or >= Keys.NumPad0 and <= Keys.NumPad9;
                    Show(OriginalScreen.MainMenu);
                    return true;
                }
                if (key is Keys.Down or Keys.Right or Keys.Tab)
                {
                    MoveCertificateSelection(1);
                }
                else if (key is Keys.Up or Keys.Left)
                {
                    MoveCertificateSelection(-1);
                }
                else if (key == Keys.Enter)
                {
                    if (auxiliarySelection == 3)
                    {
                        Show(OriginalScreen.MainMenu);
                    }
                    else
                    {
                        PrintSelectedCertificate();
                    }
                }
                else if (key == Keys.D)
                {
                    Show(OriginalScreen.MainMenu);
                }
                else
                {
                    RenderAndNotify();
                }
                return true;
            }
            case OriginalScreen.Blackboard:
                if (key is Keys.N or Keys.Enter)
                {
                    StartNextLesson();
                }
                else if (key == Keys.M)
                {
                    Show(OriginalScreen.MainMenu);
                }
                return true;
            default:
                return false;
        }
    }

    private bool HandleAuxiliaryClick(int x, int y)
    {
        switch (screen)
        {
            case OriginalScreen.About:
            case OriginalScreen.Keyboard:
                Show(OriginalScreen.MainMenu);
                return true;
            case OriginalScreen.Help:
            {
                var maximumScroll = Math.Max(0, helpLines.Length - 17);
                if (y is >= 176 and <= 194 && x is >= 14 and <= 58)
                {
                    helpScroll = Math.Min(maximumScroll, helpScroll + 15);
                    RenderAndNotify();
                }
                else if (y is >= 176 and <= 194 && x is >= 68 and <= 114)
                {
                    helpScroll = Math.Max(0, helpScroll - 15);
                    RenderAndNotify();
                }
                else if (y is >= 176 and <= 194 && x is >= 257 and <= 305)
                {
                    Show(OriginalScreen.MainMenu);
                }
                return true;
            }
            case OriginalScreen.StudentInformation:
                if (newStudentNamePending)
                {
                    return true;
                }
                if (x is >= 273 and < 312 && y is >= 179 and < 193)
                {
                    auxiliarySelection = 5;
                    CommitStudentInformation();
                }
                else if (y is >= 130 and < 180 &&
                         x is (> 65 and < 116) or (> 131 and < 182) or (> 207 and < 258))
                {
                    studentCharacterDraft = x switch
                    {
                        < 116 => OriginalCharacter.Mario,
                        < 182 => OriginalCharacter.Luigi,
                        >= 207 => OriginalCharacter.Princess,
                        _ => studentCharacterDraft,
                    };
                    RenderAndNotify();
                }
                else if (x is >= 64 and <= 256 && y is >= 68 and < 81)
                {
                    BeginStudentInformationEdit(0);
                }
                else if (x is >= 64 and <= 224 && y is >= 88 and < 101)
                {
                    BeginStudentInformationEdit(1);
                }
                return true;
            case OriginalScreen.LoadStudent:
                if (x is >= 40 and < 280 && y is >= 64 and < 144 && loadStudentNames.Length > 0)
                {
                    var clicked = (x - 40) / 80 * 10 + (y - 64) / 8;
                    if (clicked < loadStudentNames.Length)
                    {
                        var now = clock.ElapsedMilliseconds;
                        if (clicked == auxiliarySelection &&
                            clicked == lastLoadStudentClick &&
                            now - lastLoadStudentClickAt <= OriginalFrameMilliseconds * 10L)
                        {
                            CommitLoadStudent();
                            return true;
                        }
                        auxiliarySelection = clicked;
                        lastLoadStudentClick = clicked;
                        lastLoadStudentClickAt = now;
                        RenderAndNotify();
                    }
                }
                else if (y is >= 158 and <= 172 && x is >= 181 and <= 234)
                {
                    Show(auxiliaryReturnScreen);
                }
                else if (y is >= 158 and <= 172 && x is >= 246 and <= 285)
                {
                    CommitLoadStudent();
                }
                return true;
            case OriginalScreen.LessonTimes:
                if (x is >= 266 and <= 305 && y is >= 177 and <= 187)
                {
                    CommitLessonTimes();
                }
                else
                {
                    for (var index = 0; index < lessonTimesDraft.Length; index++)
                    {
                        var fieldY = 70 + index * 20;
                        if (x is >= 190 and <= 214 && y >= fieldY && y <= fieldY + 10)
                        {
                            auxiliarySelection = index;
                            BeginLessonTimeEdit(index);
                            break;
                        }
                    }
                }
                return true;
            case OriginalScreen.Certificate:
                if (!HasAnyCertificate())
                {
                    Show(OriginalScreen.MainMenu);
                }
                else if (y is >= 104 and < 128 && x is >= 36 and <= 288)
                {
                    var clicked = (y - 104) / 8;
                    if ((uint)clicked < CertificateWorlds.Length &&
                        progress.HasCertificate(CertificateWorlds[clicked]))
                    {
                        auxiliarySelection = clicked;
                        PrintSelectedCertificate();
                    }
                }
                else if (x is >= 254 and <= 308 && y is >= 174 and <= 188)
                {
                    Show(OriginalScreen.MainMenu);
                }
                return true;
            case OriginalScreen.ReportCard:
                Show(OriginalScreen.MainMenu);
                return true;
            case OriginalScreen.Blackboard:
                if (x is >= 51 and <= 108 && y is >= 173 and <= 197)
                {
                    StartNextLesson();
                }
                else if (x is >= 130 and <= 185 && y is >= 173 and <= 197)
                {
                    Show(OriginalScreen.MainMenu);
                }
                return true;
            default:
                return false;
        }
    }

    private void OpenHelp()
    {
        helpScroll = 0;
        Show(OriginalScreen.Help);
    }

    private void AppendNumericDigit(char character, Action<int> assign, int maximum)
    {
        if (auxiliaryNumericEntry.Length >= 3)
        {
            return;
        }
        auxiliaryNumericEntry.Append(character);
        if (int.TryParse(auxiliaryNumericEntry.ToString(), out var value))
        {
            assign(Math.Min(maximum, value));
        }
        RenderAndNotify();
    }

    private static bool TryNumericAdjustment(
        Keys key,
        int minimum,
        int maximum,
        int current,
        out int adjusted)
    {
        adjusted = key switch
        {
            Keys.Up or Keys.Right => current + 1,
            Keys.Down or Keys.Left => current - 1,
            Keys.PageUp => current + 5,
            Keys.PageDown => current - 5,
            Keys.Home => minimum,
            Keys.End => maximum,
            _ => current,
        };
        if (adjusted == current && key is not (Keys.Home or Keys.End))
        {
            return false;
        }
        if (key is not (Keys.Up or Keys.Right or Keys.Down or Keys.Left or
                        Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End))
        {
            return false;
        }
        adjusted = Math.Clamp(adjusted, minimum, maximum);
        return true;
    }

    private void AdjustStudentGoal(int direction)
    {
        auxiliaryNumericEntry.Clear();
        studentGoalDraft = Math.Clamp(studentGoalDraft + Math.Sign(direction), 5, 150);
        RenderAndNotify();
    }

    private void AdjustLessonTime(int direction)
    {
        auxiliaryNumericEntry.Clear();
        lessonTimesDraft[lessonTimeEditField] = Math.Clamp(
            lessonTimesDraft[lessonTimeEditField] + Math.Sign(direction),
            1,
            60);
        RenderAndNotify();
    }

    private void BackspaceNumericEntry(Action<int> assign, int fallback)
    {
        if (auxiliaryNumericEntry.Length > 0)
        {
            auxiliaryNumericEntry.Length--;
        }
        assign(auxiliaryNumericEntry.Length > 0 &&
               int.TryParse(auxiliaryNumericEntry.ToString(), out var value)
            ? value
            : fallback);
    }

    private void OpenStudentInformation()
    {
        studentNameEntry.Clear();
        studentNameEntry.Append(progress.StudentName);
        auxiliaryNumericEntry.Clear();
        studentGoalDraft = progress.GoalWordsPerMinute;
        studentLessonDraft = progress.LessonLevel;
        studentCharacterDraft = progress.Character;
        studentInformationEditField = -1;
        // The original preference routine positions the cursor on Done first.
        auxiliarySelection = 5;
        Show(OriginalScreen.StudentInformation);
        RequestStudentInformationCursor();
    }

    private void MoveStudentInformationSelection(int direction)
    {
        auxiliaryNumericEntry.Clear();
        auxiliarySelection = (auxiliarySelection + direction +
                              StudentInformationFocusPoints.Length) %
                             StudentInformationFocusPoints.Length;
        RenderAndNotify();
        RequestStudentInformationCursor();
    }

    private void RequestStudentInformationCursor() =>
        StudentInformationCursorRequested?.Invoke(
            CenteredPoint(StudentInformationFocusPoints[Math.Clamp(
                auxiliarySelection,
                0,
                StudentInformationFocusPoints.Length - 1)]));

    private void BeginStudentInformationEdit(int field)
    {
        studentInformationEditField = Math.Clamp(field, 0, 1);
        studentInformationEditName = studentNameEntry.ToString();
        studentInformationEditGoal = studentGoalDraft;
        auxiliaryNumericEntry.Clear();
        RenderAndNotify();
    }

    private void FinishStudentInformationEdit()
    {
        if (newStudentNamePending && studentInformationEditField == 0)
        {
            var name = studentNameEntry.ToString().Trim();
            if (name.Length == 0)
            {
                newStudentNamePending = false;
                studentInformationEditField = -1;
                Show(OriginalScreen.MainMenu);
                return;
            }
            newStudentNamePending = false;
            if (!progress.LoadStudent(name))
            {
                progress.StartNewStudent(name);
            }
            studentNameEntry.Clear();
            studentNameEntry.Append(progress.StudentName);
            studentGoalDraft = progress.GoalWordsPerMinute;
            studentLessonDraft = progress.LessonLevel;
            studentCharacterDraft = progress.Character;
            selectedLesson = progress.LessonLevel - 1;
            auxiliarySelection = 5;
        }
        studentInformationEditField = -1;
        auxiliaryNumericEntry.Clear();
        studentGoalDraft = Math.Clamp(studentGoalDraft, 5, 150);
        RenderAndNotify();
        RequestStudentInformationCursor();
    }

    private void CancelStudentInformationEdit()
    {
        studentNameEntry.Clear();
        studentNameEntry.Append(studentInformationEditName);
        studentGoalDraft = studentInformationEditGoal;
        studentInformationEditField = -1;
        auxiliaryNumericEntry.Clear();
        RenderAndNotify();
        RequestStudentInformationCursor();
    }

    private void OpenLoadStudent(OriginalScreen returnScreen = OriginalScreen.MainMenu)
    {
        auxiliaryReturnScreen = returnScreen;
        loadStudentNames = progress.StudentNames.Take(30).ToArray();
        auxiliarySelection = Array.FindIndex(loadStudentNames, name =>
            name.Equals(progress.StudentName, StringComparison.OrdinalIgnoreCase));
        auxiliarySelection = Math.Max(0, auxiliarySelection);
        lastLoadStudentClick = -1;
        lastLoadStudentClickAt = long.MinValue;
        Show(OriginalScreen.LoadStudent);
    }

    private void CommitLoadStudent()
    {
        if (loadStudentNames.Length > 0 &&
            progress.LoadStudent(loadStudentNames[Math.Clamp(auxiliarySelection, 0, loadStudentNames.Length - 1)]))
        {
            selectedLesson = progress.LessonLevel - 1;
        }
        Show(auxiliaryReturnScreen);
    }

    private void CommitStudentInformation()
    {
        studentInformationEditField = -1;
        progress.SetStudentInformation(
            studentNameEntry.ToString(),
            studentGoalDraft,
            studentLessonDraft,
            studentCharacterDraft);
        selectedLesson = progress.LessonLevel - 1;
        Show(OriginalScreen.MainMenu);
    }

    private void OpenNewStudentInformation()
    {
        studentNameEntry.Clear();
        auxiliaryNumericEntry.Clear();
        studentGoalDraft = 5;
        studentLessonDraft = 1;
        studentCharacterDraft = OriginalCharacter.Mario;
        studentInformationEditField = 0;
        studentInformationEditName = string.Empty;
        studentInformationEditGoal = 5;
        newStudentNamePending = true;
        auxiliarySelection = 0;
        Show(OriginalScreen.StudentInformation);
    }

    private void OpenLessonTimes()
    {
        lessonTimesDraft = (int[])progress.LessonTimesMinutes.Clone();
        auxiliaryNumericEntry.Clear();
        lessonTimeEditField = -1;
        // The DOS routine initializes its five-stop cursor on Done.
        auxiliarySelection = 4;
        Show(OriginalScreen.LessonTimes);
        RequestLessonTimesCursor();
    }

    private void MoveLessonTimesSelection(int direction)
    {
        auxiliaryNumericEntry.Clear();
        auxiliarySelection = (auxiliarySelection + direction + LessonTimesFocusPoints.Length) %
                             LessonTimesFocusPoints.Length;
        RenderAndNotify();
        RequestLessonTimesCursor();
    }

    private void RequestLessonTimesCursor() =>
        LessonTimesCursorRequested?.Invoke(
            CenteredPoint(LessonTimesFocusPoints[Math.Clamp(
                auxiliarySelection,
                0,
                LessonTimesFocusPoints.Length - 1)]));

    private void BeginLessonTimeEdit(int field)
    {
        lessonTimeEditField = Math.Clamp(field, 0, lessonTimesDraft.Length - 1);
        lessonTimeEditOriginalValue = lessonTimesDraft[lessonTimeEditField];
        auxiliaryNumericEntry.Clear();
        RenderAndNotify();
    }

    private void FinishLessonTimeEdit()
    {
        lessonTimesDraft[lessonTimeEditField] = Math.Clamp(
            lessonTimesDraft[lessonTimeEditField],
            1,
            60);
        lessonTimeEditField = -1;
        auxiliaryNumericEntry.Clear();
        RenderAndNotify();
        RequestLessonTimesCursor();
    }

    private void CancelLessonTimeEdit()
    {
        lessonTimesDraft[lessonTimeEditField] = lessonTimeEditOriginalValue;
        lessonTimeEditField = -1;
        auxiliaryNumericEntry.Clear();
        RenderAndNotify();
        RequestLessonTimesCursor();
    }

    private void CommitLessonTimes()
    {
        lessonTimeEditField = -1;
        progress.SetLessonTimes(lessonTimesDraft);
        Show(OriginalScreen.MainMenu);
    }

    private void ShowDemoInstructions()
    {
        menuPopup = MainMenuPopup.None;
        demoAwaitingWorld = true;
        statusMessage = "Demo Mode Selected.\nPress 1, 2, 3 or 4.";
        RenderAndNotify();
    }

    private void StartNextLesson()
    {
        var current = progress.CurrentWorld;
        var next = WorldForNext(current, progress.GoalWordsPerMinute);
        if (ShouldResetLessonForWorldChange(current, next))
        {
            progress.SetLessonLevel(1);
            selectedLesson = 0;
        }
        if (next == OriginalScreen.Practice)
        {
            StartPractice();
        }
        else
        {
            StartWorld(next);
        }
    }

    internal static OriginalScreen WorldForNext(OriginalScreen currentWorld, int goalWordsPerMinute)
    {
        if (currentWorld == OriginalScreen.Practice)
        {
            return OriginalScreen.Practice;
        }
        return goalWordsPerMinute switch
        {
            < 10 => OriginalScreen.Outside,
            < 30 => OriginalScreen.Water,
            _ => OriginalScreen.Castle,
        };
    }

    internal static bool ShouldResetLessonForWorldChange(
        OriginalScreen currentWorld,
        OriginalScreen nextWorld) =>
        currentWorld != nextWorld &&
        nextWorld is OriginalScreen.Water or OriginalScreen.Castle;

    internal static ActivityMode PracticeModeForGoal(int goalWordsPerMinute) => goalWordsPerMinute switch
    {
        < 10 => ActivityMode.LetterDrill,
        < 30 => ActivityMode.WordChallenge,
        _ => ActivityMode.SentenceChallenge,
    };

    private void StartPractice()
    {
        demoMode = false;
        progress.SetCurrentWorld(OriginalScreen.Practice);
        lastCompletedWorld = OriginalScreen.Practice;
        Show(OriginalScreen.Practice);
    }

    private void Show(OriginalScreen next)
    {
        var previous = screen;
        var preserveMenuMusic = IsMenuMusicScreen(previous) && IsMenuMusicScreen(next);
        if (screen != next)
        {
            if (preserveMenuMusic)
            {
                audio.StopSpeech();
                audio.StopSoundEffects();
            }
            else
            {
                audio.Stop();
            }
        }
        screen = next;
        if (next == OriginalScreen.Certificate)
        {
            auxiliarySelection = 3;
            CertificateCursorRequested?.Invoke(
                CenteredPoint(CertificateFocusPoints[auxiliarySelection]));
        }
        if (next is not (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice))
        {
            demoMode = false;
        }
        statusMessage = null;
        rapPlaying = false;
        menuPopup = MainMenuPopup.None;
        position = 0;
        errors = 0;
        problemKeys.Clear();
        typingStarted = -1;
        lessonCompleteAt = -1;
        debriefMoviePlayed = false;
        sceneStarted = clock.ElapsedMilliseconds;
        prompt = next switch
        {
            OriginalScreen.Outside => CreateLessonPrompt(ActivityMode.LetterDrill),
            OriginalScreen.Water => CreateLessonPrompt(ActivityMode.WordChallenge),
            OriginalScreen.Castle => CreateLessonPrompt(ActivityMode.SentenceChallenge),
            OriginalScreen.Practice => CreateLessonPrompt(PracticeModeForGoal(progress.GoalWordsPerMinute)),
            _ => prompt,
        };
        if (next is OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice)
        {
            typingStarted = sceneStarted;
        }
        if (next == OriginalScreen.Outside)
        {
            InitializeOutsideState();
        }
        else if (next == OriginalScreen.Water)
        {
            InitializeWaterState();
        }
        else if (next == OriginalScreen.Castle)
        {
            InitializeCastleState();
        }
        else if (next == OriginalScreen.Practice)
        {
            InitializePracticeState();
        }
        if (next == OriginalScreen.MainMenu)
        {
            if (!preserveMenuMusic)
            {
                PlayMusicSequence(0);
            }
            if (!menuWelcomePlayed)
            {
                menuWelcomePlayed = true;
                StartMainMenuMovie(PickCue(OriginalAvCues.MainMenuRandom));
            }
            else
            {
                menuIdleMovieDue = clock.ElapsedMilliseconds + NextMainMenuIdleDelay();
            }
        }
        else
        {
            var musicIndex = next switch
            {
                OriginalScreen.Outside => 0,
                OriginalScreen.Water => 1,
                OriginalScreen.Castle => 2,
                OriginalScreen.Practice => 3,
                _ => -1,
            };
            if (musicIndex >= 0)
            {
                PlayMusicSequence(musicIndex);
            }
        }
        Render();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsMenuMusicScreen(OriginalScreen value) => value is
        OriginalScreen.Title or
        OriginalScreen.MainMenu or
        OriginalScreen.About or
        OriginalScreen.Help or
        OriginalScreen.Keyboard or
        OriginalScreen.StudentInformation or
        OriginalScreen.LoadStudent or
        OriginalScreen.LessonTimes or
        OriginalScreen.Certificate or
        OriginalScreen.ReportCard;

    private void StartWorld(OriginalScreen world)
    {
        demoMode = false;
        var requiredGoal = world switch
        {
            OriginalScreen.Water => 10,
            OriginalScreen.Castle => 30,
            _ => 0,
        };
        if (progress.GoalWordsPerMinute < requiredGoal)
        {
            menuPopup = MainMenuPopup.None;
            statusMessage = $"Goal must be {requiredGoal} WPM for this world.";
            RenderAndNotify();
            return;
        }
        progress.SetCurrentWorld(world);
        lastCompletedWorld = world;
        if (cinematicsEnabled)
        {
            var cues = EligibleWorldCues(world, progress.Character);
            if (cues is not null)
            {
                var backdrop = world switch
                {
                    OriginalScreen.Outside => outside,
                    OriginalScreen.Water => water,
                    OriginalScreen.Castle => castle,
                    _ => mainMenu,
                };
                PlayAvMovie(PickCue(cues), world, backdrop, 44, 24);
                return;
            }
        }
        Show(world);
    }

    private void StartClosingMovieOrReport()
    {
        if (cinematicsEnabled && !demoMode && !debriefMoviePlayed && lastResult is not null)
        {
            debriefMoviePlayed = true;
            var previousResult = progress.Results.Count >= 2
                ? progress.Results[^2]
                : null;
            var cues = EligibleDebriefCues(
                lastCompletedWorld,
                previousResult?.WordsPerMinute ?? 0,
                lastResult.WordsPerMinute);
            PlayAvMovie(
                PickCue(cues),
                OriginalScreen.Blackboard,
                blackboard,
                34,
                34,
                StartRewardMovieOrReport);
            return;
        }

        StartRewardMovieOrReport();
    }

    private void StartRewardMovieOrReport()
    {
        var rewardMovie = RewardMovieNumber(lastCompletedWorld, completedLessonLevel - 1);
        var endingFollows = earnedCertificateThisLesson &&
                            lastCompletedWorld == OriginalScreen.Castle &&
                            completedLessonLevel == 6;
        if (!cinematicsEnabled || !rewardMovieQualified || rewardMovie == 0)
        {
            if (endingFollows)
            {
                StartEndingAnimation();
            }
            else
            {
                Show(OriginalScreen.Blackboard);
            }
        }
        else
        {
            PlayMovie(
                rewardMovie,
                endingFollows ? OriginalScreen.EndingAnimation : OriginalScreen.Blackboard);
        }
    }

    internal static int RewardMovieNumber(OriginalScreen world, int zeroBasedLessonLevel)
    {
        if (zeroBasedLessonLevel is < 0 or > 5)
        {
            return 0;
        }

        return world switch
        {
            OriginalScreen.Outside when zeroBasedLessonLevel == 0 => 1,
            OriginalScreen.Outside when zeroBasedLessonLevel < 5 => 2,
            OriginalScreen.Outside => 3,
            OriginalScreen.Water when zeroBasedLessonLevel < 5 => 4,
            OriginalScreen.Water => 5,
            OriginalScreen.Castle when zeroBasedLessonLevel < 5 => 6,
            OriginalScreen.Castle => 7,
            _ => 0,
        };
    }

    internal static LessonProgressOutcome AdvanceLessonProgress(
        OriginalScreen world,
        int completedLessonLevel,
        int currentGoalWordsPerMinute,
        int measuredWordsPerMinute)
    {
        completedLessonLevel = Math.Clamp(completedLessonLevel, 1, 6);
        currentGoalWordsPerMinute = Math.Clamp(currentGoalWordsPerMinute, 5, 150);
        measuredWordsPerMinute = Math.Max(0, measuredWordsPerMinute);
        if (world is not (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle) ||
            measuredWordsPerMinute < currentGoalWordsPerMinute)
        {
            return new LessonProgressOutcome(
                completedLessonLevel,
                currentGoalWordsPerMinute,
                RewardMovieQualified: false,
                CertificateEarned: false);
        }

        if (completedLessonLevel < 6)
        {
            return new LessonProgressOutcome(
                completedLessonLevel + 1,
                currentGoalWordsPerMinute,
                RewardMovieQualified: true,
                CertificateEarned: false);
        }

        return world switch
        {
            OriginalScreen.Outside => FinalWorldProgress(10, 2),
            OriginalScreen.Water => FinalWorldProgress(30, 5),
            OriginalScreen.Castle => new LessonProgressOutcome(
                6,
                Math.Min(150, measuredWordsPerMinute + 5),
                RewardMovieQualified: true,
                CertificateEarned: measuredWordsPerMinute + 5 >= 45),
            _ => new LessonProgressOutcome(6, currentGoalWordsPerMinute, false, false),
        };

        LessonProgressOutcome FinalWorldProgress(int cap, int increment)
        {
            var unclampedGoal = measuredWordsPerMinute + increment;
            var nextGoal = Math.Min(cap, unclampedGoal);
            return new LessonProgressOutcome(
                6,
                nextGoal,
                RewardMovieQualified: measuredWordsPerMinute >= nextGoal,
                CertificateEarned: unclampedGoal > cap);
        }
    }

    private void PlayMovie(int movieNumber, OriginalScreen destination)
    {
        if (!cinematicsEnabled)
        {
            Show(destination);
            return;
        }

        audio.Stop();
        activeMovie = MveDecoder.Decode(assets.ReadAllBytes($"MOVIES/MARIO{movieNumber}.MVE"));
        movieDestination = destination;
        screen = OriginalScreen.Movie;
        sceneStarted = clock.ElapsedMilliseconds;
        audio.PlayPcm(
            activeMovie.PcmAudio,
            activeMovie.AudioSampleRate,
            activeMovie.AudioBitsPerSample,
            activeMovie.AudioChannels,
            loop: false);
        RenderAndNotify();
    }

    internal void StartMovieForTest(int movieNumber = 1) =>
        PlayMovie(movieNumber, OriginalScreen.MainMenu);

    private void PlayAvMovie(
        string cue,
        OriginalScreen destination,
        CrxImage backdrop,
        int x,
        int y,
        Action? finished = null)
    {
        if (!cinematicsEnabled)
        {
            Show(destination);
            return;
        }

        audio.Stop();
        activeAvMovie = AvDecoder.Decode(assets.ReadAllBytes($"MOVIES/{cue}.AV"));
        activeAvPalette = backdrop.Palette.WithRange(activeAvMovie.Palette, 44, 212);
        activeAvBackdrop = backdrop;
        activeAvX = x;
        activeAvY = y;
        avMovieDestination = destination;
        avMovieFinished = finished;
        screen = OriginalScreen.AvMovie;
        sceneStarted = clock.ElapsedMilliseconds;
        audio.PlayAv(activeAvMovie, loop: false);
        RenderAndNotify();
    }

    private void FinishAvMovie()
    {
        var destination = avMovieDestination;
        var finished = avMovieFinished;
        audio.Stop();
        activeAvMovie = null;
        activeAvPalette = null;
        activeAvBackdrop = null;
        avMovieFinished = null;
        if (finished is not null)
        {
            finished();
        }
        else
        {
            Show(destination);
        }
    }

    private void FinishMovie()
    {
        var destination = movieDestination;
        audio.Stop();
        activeMovie = null;
        if (destination == OriginalScreen.EndingAnimation)
        {
            StartEndingAnimation();
        }
        else
        {
            Show(destination);
        }
    }

    private void Render()
    {
        EnsureFramebufferSize();
        switch (screen)
        {
            case OriginalScreen.Title:
                RenderTitleAnimation();
                break;
            case OriginalScreen.MainMenu:
                RenderMainMenu();
                break;
            case OriginalScreen.About:
                DrawCenteredFrame(logo);
                break;
            case OriginalScreen.Help:
                RenderHelp();
                break;
            case OriginalScreen.Keyboard:
                DrawCenteredUiFrame(keyboard);
                break;
            case OriginalScreen.StudentInformation:
                RenderStudentInformation();
                break;
            case OriginalScreen.LoadStudent:
                RenderLoadStudent();
                break;
            case OriginalScreen.LessonTimes:
                RenderLessonTimes();
                break;
            case OriginalScreen.Certificate:
                RenderCertificate();
                break;
            case OriginalScreen.Blackboard:
                RenderBlackboard();
                break;
            case OriginalScreen.Outside:
                RenderOutside();
                break;
            case OriginalScreen.Water:
                RenderWater();
                break;
            case OriginalScreen.Castle:
                RenderCastle();
                break;
            case OriginalScreen.Practice:
                RenderPractice();
                break;
            case OriginalScreen.ReportCard:
                RenderReportCard();
                break;
            case OriginalScreen.BonusAnimation:
                RenderBonusAnimation();
                break;
            case OriginalScreen.EndingAnimation:
                RenderEndingAnimation();
                break;
            case OriginalScreen.AvMovie:
                RenderAvMovie();
                break;
            case OriginalScreen.Movie:
                RenderMovie();
                break;
        }
    }

    private void EnsureFramebufferSize()
    {
        var width = DisplayFrameWidth(progress.DisplayMode);
        if (raster.Width == width)
        {
            return;
        }

        raster.Dispose();
        raster = new NativeRaster(width, FrameHeight);
    }

    internal static int DisplayFrameWidth(NativeDisplayMode mode) => mode switch
    {
        NativeDisplayMode.Widescreen => WidescreenFrameWidth,
        NativeDisplayMode.UltraWidescreen => UltraWidescreenFrameWidth,
        _ => ClassicFrameWidth,
    };

    private int CenteredContentOffsetX => Math.Max(0, (raster.Width - ClassicFrameWidth) / 2);

    private int CenteredX(int originalX) => CenteredContentOffsetX + originalX;

    private Point CenteredPoint(Point original) =>
        new(CenteredX(original.X), original.Y);

    private void DrawCenteredFrame(CrxImage image) =>
        raster.DrawIndexedImageCenteredWithEdgeExtension(
            image.Pixels,
            CrxImage.Width,
            CrxImage.Height,
            image.Palette);

    private void DrawCenteredFrame(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        VgaPalette palette) =>
        raster.DrawIndexedImageCenteredWithEdgeExtension(
            pixels,
            width,
            height,
            palette);

    private void DrawMarioWallpaper()
    {
        raster.DrawIndexedRegionHorizontallyTiled(
            mainMenu.Pixels,
            ClassicFrameWidth,
            FrameHeight,
            mainMenu.Palette,
            sourceX: 0,
            regionWidth: 232);
    }

    private void DrawCenteredUiFrame(CrxImage image)
    {
        if (raster.Width == ClassicFrameWidth)
        {
            raster.CopyFrom(image);
            return;
        }

        DrawMarioWallpaper();
        raster.DrawIndexedImage(
            image.Pixels,
            ClassicFrameWidth,
            FrameHeight,
            image.Palette,
            CenteredContentOffsetX,
            0);
    }

    private void RenderAvMovie()
    {
        if (activeAvMovie is null || activeAvPalette is null || activeAvBackdrop is null)
        {
            raster.FillRectangle(0, 0, raster.Width, FrameHeight, Color.Black.ToArgb());
            return;
        }

        var worldBackdrop = ReferenceEquals(activeAvBackdrop, outside) ||
                            ReferenceEquals(activeAvBackdrop, water) ||
                            ReferenceEquals(activeAvBackdrop, castle);
        if (ReferenceEquals(activeAvBackdrop, outside))
        {
            raster.CopyFrom(outside, repeatHorizontally: raster.Width > ClassicFrameWidth);
        }
        else if (ReferenceEquals(activeAvBackdrop, water))
        {
            raster.CopyFrom(
                water,
                repeatHorizontally: raster.Width > ClassicFrameWidth,
                extensionSourceX: Math.Max(0, ClassicFrameWidth * 2 - raster.Width));
        }
        else if (ReferenceEquals(activeAvBackdrop, castle))
        {
            raster.DrawIndexedImageHorizontallyTiled(
                castle.Pixels,
                ClassicFrameWidth,
                FrameHeight,
                castle.Palette);
        }
        else
        {
            DrawCenteredFrame(activeAvBackdrop);
        }
        if (ReferenceEquals(activeAvBackdrop, blackboard))
        {
            DrawBlackboardContents();
        }
        var frame = (int)Math.Min(
            activeAvMovie.Frames.Count - 1,
            Math.Max(0, clock.ElapsedMilliseconds - sceneStarted) *
            OriginalAvMovie.FramesPerSecond / 1000);
        raster.DrawIndexedImage(
            activeAvMovie.Frames[frame],
            OriginalAvMovie.Width,
            OriginalAvMovie.Height,
            activeAvPalette,
            worldBackdrop ? activeAvX : CenteredX(activeAvX),
            activeAvY);
    }

    private void UpdateTitleAnimation()
    {
        var timeline = GreetingTimelineAtMilliseconds(
            Math.Max(0, clock.ElapsedMilliseconds - titleAnimationStarted),
            titleLetterDelayTicks);
        while (titleLastSoundLetterCount < timeline.LetterCount)
        {
            PlaySoundEffect(1);
            titleLastSoundLetterCount++;
            titleSoundEffectCount++;
        }
        if (timeline.Complete)
        {
            Show(OriginalScreen.MainMenu);
            return;
        }
        RenderAndNotify();
    }

    private void RenderTitleAnimation()
    {
        var timeline = GreetingTimelineAtMilliseconds(
            Math.Max(0, clock.ElapsedMilliseconds - titleAnimationStarted),
            titleLetterDelayTicks);
        switch (timeline.Stage)
        {
            case GreetingStage.Interplay:
                DrawTitleFrame(logo, timeline.Palette);
                return;
            case GreetingStage.Zoom:
            {
                DrawTitleFrame(titleBackground, timeline.Palette);
                var sprite = titleZoomFrames[19 - timeline.Frame];
                raster.DrawSprite(
                    sprite,
                    timeline.Palette,
                    CenteredX(160 - sprite.Width / 2),
                    100 - sprite.Height / 2);
                return;
            }
            case GreetingStage.ZoomHold:
            {
                DrawTitleFrame(titleBackground, timeline.Palette);
                var sprite = titleZoomFrames[0];
                raster.DrawSprite(
                    sprite,
                    timeline.Palette,
                    CenteredX(160 - sprite.Width / 2),
                    100 - sprite.Height / 2);
                return;
            }
            case GreetingStage.Move:
            {
                DrawTitleFrame(titleBackground, timeline.Palette);
                if (timeline.Frame < 5)
                {
                    var sprite = titleZoomFrames[timeline.Frame + 1];
                    raster.DrawSprite(
                        sprite,
                        timeline.Palette,
                        CenteredX(160 - sprite.Width / 2 - (timeline.Frame + 1) * 14),
                        100 - sprite.Height / 2 - (timeline.Frame + 1) * 11);
                }
                else
                {
                    raster.DrawSprite(
                        greetingObjects["smario_1"],
                        timeline.Palette,
                        CenteredX(7),
                        2);
                }
                return;
            }
        }

        DrawTitleFrame(title, timeline.Palette);
        var letters = timeline.LetterCount;
        const string greetingText = "TEACHESTYPING";
        for (var index = 0; index < letters; index++)
        {
            raster.DrawSprite(
                greetingObjects[$"greettxt_{greetingText[index]}"],
                timeline.Palette,
                CenteredX(GreetingTextXPositions[index]),
                7);
        }
    }

    private void DrawTitleFrame(CrxImage image, VgaPalette palette)
    {
        if (raster.Width == ClassicFrameWidth)
        {
            raster.DrawIndexedImage(
                image.Pixels,
                CrxImage.Width,
                CrxImage.Height,
                palette,
                0,
                0);
            return;
        }

        // Title artwork occupies almost the complete source width, so extending
        // an edge column produces conspicuous horizontal bands. A palette-aware
        // solid stage color keeps every fade clean and the original frame exact.
        raster.FillRectangle(
            0,
            0,
            raster.Width,
            FrameHeight,
            palette.Argb[image.Pixels[0]]);
        raster.DrawIndexedImage(
            image.Pixels,
            CrxImage.Width,
            CrxImage.Height,
            palette,
            CenteredContentOffsetX,
            0);
    }

    private static int[] CreateGreetingLetterDelayTicks(Random? random = null)
    {
        random ??= Random.Shared;
        return Enumerable.Range(0, 13)
            .Select(_ => random.Next(4) + 4)
            .ToArray();
    }

    internal static int[] CreateGreetingLetterDelayTicksForTest(params int[] randomValues) =>
        randomValues.Select(value => Math.Clamp(value, 0, 3) + 4).ToArray();

    internal GreetingTimeline GreetingTimelineAtTick(
        int tick,
        IReadOnlyList<int> letterDelayTicks) => GreetingTimelineAtMilliseconds(
            Math.Max(0, tick) * OriginalFrameMilliseconds,
            letterDelayTicks);

    internal GreetingTimeline GreetingTimelineAtMillisecondsForTest(
        long elapsedMilliseconds,
        IReadOnlyList<int> letterDelayTicks) =>
        GreetingTimelineAtMilliseconds(elapsedMilliseconds, letterDelayTicks);

    private GreetingTimeline GreetingTimelineAtMilliseconds(
        long elapsedMilliseconds,
        IReadOnlyList<int> letterDelayTicks)
    {
        if (letterDelayTicks.Count != 13)
        {
            throw new ArgumentException("The title needs exactly thirteen letter delays.", nameof(letterDelayTicks));
        }
        // The original greeting uses two distinct solid VGA palettes.  core_palette(3)
        // fills every DAC channel with 3 before LOGO.CRX is faded in, while the
        // scene-to-scene bridge explicitly writes the DAC triplet (29, 0, 45).
        var neutral = SolidVgaPalette(3, 3, 3);
        var purple = SolidVgaPalette(29, 0, 45);
        var elapsed = Math.Max(0, elapsedMilliseconds);
        const int fadeMilliseconds = GreetingFadeMilliseconds;
        if (elapsed < fadeMilliseconds)
        {
            return new GreetingTimeline(
                GreetingStage.Interplay, 0, 0,
                OriginalFadePalette(neutral, logo.Palette, elapsed), false);
        }
        elapsed -= fadeMilliseconds;
        var holdMilliseconds = GreetingLogoHoldTicks * OriginalFrameMilliseconds;
        if (elapsed < holdMilliseconds)
        {
            return new GreetingTimeline(GreetingStage.Interplay, 0, 0, logo.Palette, false);
        }
        elapsed -= holdMilliseconds;
        if (elapsed < fadeMilliseconds)
        {
            return new GreetingTimeline(
                GreetingStage.Interplay, 0, 0,
                OriginalFadePalette(logo.Palette, purple, elapsed), false);
        }
        elapsed -= fadeMilliseconds;
        if (elapsed < fadeMilliseconds)
        {
            return new GreetingTimeline(
                GreetingStage.Zoom, 0, 0,
                OriginalFadePalette(purple, titleBackground.Palette, elapsed), false);
        }
        elapsed -= fadeMilliseconds;
        var tick = (int)(elapsed / OriginalFrameMilliseconds);
        if (tick < GreetingZoomGrowTicks)
        {
            return new GreetingTimeline(GreetingStage.Zoom, tick, 0, titleBackground.Palette, false);
        }
        tick -= GreetingZoomGrowTicks;
        if (tick < GreetingZoomHoldTicks)
        {
            return new GreetingTimeline(GreetingStage.ZoomHold, tick, 0, titleBackground.Palette, false);
        }
        tick -= GreetingZoomHoldTicks;
        if (tick < GreetingZoomMoveTicks)
        {
            return new GreetingTimeline(GreetingStage.Move, tick, 0, titleBackground.Palette, false);
        }
        tick -= GreetingZoomMoveTicks;
        var letters = 0;
        for (var index = 0; index < letterDelayTicks.Count; index++)
        {
            letters++;
            var delay = letterDelayTicks[index] + (index == 6 ? GreetingWordBreakTicks : 0);
            if (tick < delay)
            {
                return new GreetingTimeline(GreetingStage.Letters, 0, letters, title.Palette, false);
            }
            tick -= delay;
        }
        if (tick < GreetingFinalHoldTicks)
        {
            return new GreetingTimeline(GreetingStage.FinalHold, 0, 13, title.Palette, false);
        }
        elapsed = (tick - GreetingFinalHoldTicks) * OriginalFrameMilliseconds;
        if (elapsed < fadeMilliseconds)
        {
            return new GreetingTimeline(
                GreetingStage.FinalHold, 0, 13,
                OriginalFadePalette(title.Palette, purple, elapsed), false);
        }
        return new GreetingTimeline(GreetingStage.FinalHold, 0, 13, purple, true);
    }

    private static VgaPalette SolidVgaPalette(byte red, byte green, byte blue)
    {
        static byte ExpandDac(byte value) => (byte)((value << 2) | (value >> 4));
        var color = Color.FromArgb(
            255,
            ExpandDac(red),
            ExpandDac(green),
            ExpandDac(blue)).ToArgb();
        return new VgaPalette(Enumerable.Repeat(color, 256).ToArray());
    }

    private static VgaPalette OriginalFadePalette(
        VgaPalette from,
        VgaPalette to,
        long elapsedMilliseconds)
    {
        var write = (int)Math.Clamp(
            elapsedMilliseconds * 70 / 2000,
            0,
            GreetingPaletteWriteCount - 1);
        if (write >= GreetingPaletteWriteCount - 1)
        {
            return to;
        }
        var numerator = Math.Clamp(write - 1, 0, 11);
        const int denominator = 12;
        var colors = new int[256];
        for (var index = 0; index < colors.Length; index++)
        {
            var first = Color.FromArgb(from.Argb[index]);
            var last = Color.FromArgb(to.Argb[index]);
            byte Channel(byte start, byte end) =>
                (byte)(start + (end - start) * numerator / denominator);
            colors[index] = Color.FromArgb(
                255,
                Channel(first.R, last.R),
                Channel(first.G, last.G),
                Channel(first.B, last.B)).ToArgb();
        }
        return new VgaPalette(colors);
    }

    internal static OriginalSprite[] CreateGreetingZoomFrames(OriginalSprite largeMario)
    {
        var frames = new OriginalSprite[20];
        frames[0] = largeMario;
        for (var frame = 1; frame < frames.Length; frame++)
        {
            frames[frame] = ScaleSevenEighths(frames[frame - 1]);
        }
        return frames;
    }

    private static OriginalSprite ScaleSevenEighths(OriginalSprite source)
    {
        var width = source.Width * 7 / 8;
        var height = source.Height * 7 / 8;
        var pixels = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var sourceY = y + (y + 6) / 7;
            for (var x = 0; x < width; x++)
            {
                var sourceX = x + (x + 6) / 7;
                pixels[y * width + x] = source.Pixels[sourceY * source.Width + sourceX];
            }
        }
        return new OriginalSprite(0, 0, (ushort)width, (ushort)height, pixels);
    }

    internal int TitleZoomFrameCount => titleZoomFrames.Length;

    internal (int Width, int Height) TitleZoomFrameSize(int frame)
    {
        var sprite = titleZoomFrames[Math.Clamp(frame, 0, titleZoomFrames.Length - 1)];
        return (sprite.Width, sprite.Height);
    }

    internal void SetTitleAnimationTickForTest(int tick)
    {
        if (screen != OriginalScreen.Title)
        {
            throw new InvalidOperationException("Title animation is not active.");
        }
        titleAnimationStarted = clock.ElapsedMilliseconds - Math.Max(0, tick) * OriginalFrameMilliseconds;
        Render();
    }

    internal void SetTitleAnimationMillisecondsForTest(long elapsedMilliseconds)
    {
        if (screen != OriginalScreen.Title)
        {
            throw new InvalidOperationException("Title animation is not active.");
        }
        titleAnimationStarted = clock.ElapsedMilliseconds - Math.Max(0, elapsedMilliseconds);
        Render();
    }

    internal void AdvanceTitleAnimationMillisecondsForTest(long elapsedMilliseconds)
    {
        SetTitleAnimationMillisecondsForTest(elapsedMilliseconds);
        UpdateTitleAnimation();
    }

    private void RenderBonusAnimation()
    {
        if (activeBonusPalette is null || ActiveBonusAnimationFrameCount == 0)
        {
            raster.FillRectangle(0, 0, raster.Width, FrameHeight, Color.Black.ToArgb());
            return;
        }

        var frame = (int)Math.Min(
            ActiveBonusAnimationFrameCount - 1,
            Math.Max(0, clock.ElapsedMilliseconds - bonusAnimationStarted) /
            BonusAnimationFrameMilliseconds);
        if (activeBonusBackgroundFrames is { Count: > 0 })
        {
            var background = activeBonusBackgroundFrames[
                Math.Min(frame, activeBonusBackgroundFrames.Count - 1)];
            if (bonusAnimationDestination == OriginalScreen.Castle)
            {
                raster.DrawIndexedImageHorizontallyTiled(
                    background,
                    ClassicFrameWidth,
                    FrameHeight,
                    activeBonusPalette);
            }
            else
            {
                raster.DrawIndexedImageHorizontallyExtended(
                    background,
                    ClassicFrameWidth,
                    FrameHeight,
                    activeBonusPalette,
                    extensionSourceX: Math.Max(0, ClassicFrameWidth * 2 - raster.Width));
            }
        }
        else
        {
            raster.FillRectangle(0, 0, raster.Width, FrameHeight, Color.Black.ToArgb());
        }

        if (activeBonusCharacterFrames is { Count: > 0 } && frame < activeBonusCharacterFrames.Count)
        {
            var characterFrame = activeBonusCharacterFrames[frame];
            raster.DrawSprite(
                characterFrame.Sprite,
                activeBonusPalette,
                characterFrame.X,
                characterFrame.Y);
        }
    }

    private void StartEndingAnimation()
    {
        audio.Stop();
        endingAnimationFrames = BackAnimationDecoder.DecodeBan(
            assets.ReadAllBytes("ENDF.BAN"),
            ending.Pixels);
        endingAnimationStarted = clock.ElapsedMilliseconds;
        screen = OriginalScreen.EndingAnimation;
        RenderAndNotify();
    }

    private void UpdateEndingAnimation()
    {
        if (endingAnimationFrames is null || endingAnimationFrames.Count == 0 ||
            clock.ElapsedMilliseconds - endingAnimationStarted >=
                endingAnimationFrames.Count * BonusAnimationFrameMilliseconds)
        {
            FinishEndingAnimation();
            return;
        }
        RenderAndNotify();
    }

    private void FinishEndingAnimation()
    {
        endingAnimationFrames = null;
        Show(OriginalScreen.Blackboard);
    }

    private void RenderEndingAnimation()
    {
        raster.FillRectangle(0, 0, raster.Width, FrameHeight, Color.Black.ToArgb());
        if (endingAnimationFrames is null || endingAnimationFrames.Count == 0)
        {
            raster.DrawIndexedImage(
                ending.Pixels,
                CrxImage.Width,
                CrxImage.Height,
                ending.Palette,
                CenteredContentOffsetX,
                0);
            return;
        }
        var frame = (int)Math.Min(
            endingAnimationFrames.Count - 1,
            Math.Max(0, clock.ElapsedMilliseconds - endingAnimationStarted) /
            BonusAnimationFrameMilliseconds);
        raster.DrawIndexedImage(
            endingAnimationFrames[frame],
            CrxImage.Width,
            CrxImage.Height,
            ending.Palette,
            CenteredContentOffsetX,
            0);
    }

    internal int EndingAnimationFrameCount => endingAnimationFrames?.Count ?? 0;

    internal void StartEndingAnimationForTest() => StartEndingAnimation();

    internal void SetEndingAnimationFrameForTest(int frame)
    {
        if (screen != OriginalScreen.EndingAnimation || endingAnimationFrames is null)
        {
            throw new InvalidOperationException("Ending animation is not active.");
        }
        endingAnimationStarted = clock.ElapsedMilliseconds -
                                 Math.Clamp(frame, 0, endingAnimationFrames.Count - 1) *
                                 BonusAnimationFrameMilliseconds;
        Render();
    }

    private void RenderMovie()
    {
        if (activeMovie is null || activeMovie.Frames.Count == 0)
        {
            raster.FillRectangle(0, 0, raster.Width, FrameHeight, Color.Black.ToArgb());
            return;
        }
        var elapsedMicroseconds = Math.Max(0, clock.ElapsedMilliseconds - sceneStarted) * 1000;
        var frameDuration = Math.Max(1, activeMovie.FrameDurationMicroseconds);
        var frameIndex = (int)Math.Min(
            activeMovie.Frames.Count - 1,
            elapsedMicroseconds / frameDuration);
        var frame = activeMovie.Frames[frameIndex];
        DrawCenteredFrame(
            frame.Pixels,
            activeMovie.Width,
            activeMovie.Height,
            frame.Palette);
    }

    private void RenderHelp()
    {
        DrawCenteredUiFrame(help);
        var black = help.Palette.FindNearest(0, 0, 0);
        var blue = help.Palette.FindNearest(0, 80, 255);
        for (var row = 0; row < 17 && helpScroll + row < helpLines.Length; row++)
        {
            var line = helpLines[helpScroll + row];
            font5.Draw(
                raster,
                help.Palette,
                line.Text,
                CenteredX(16),
                29 + row * 8,
                35,
                foregroundIndex: line.Heading ? blue : black);
        }
    }

    private void RenderStudentInformation()
    {
        DrawCenteredUiFrame(studentInformation);
        var white = studentInformation.Palette.FindNearest(255, 255, 255);
        if (newStudentNamePending)
        {
            font3.Draw(
                raster,
                studentInformation.Palette,
                "New Student Name:",
                CenteredX(64),
                96,
                foregroundIndex: white);
            font3.Draw(
                raster,
                studentInformation.Palette,
                studentNameEntry +
                ((clock.ElapsedMilliseconds / 350 & 1) == 0 ? "_" : " "),
                CenteredX(200),
                96,
                9,
                foregroundIndex: white);
            return;
        }
        var values = new[]
        {
            $"Student Name: {studentNameEntry.ToString().PadRight(8)}" +
            (studentInformationEditField == 0 && (clock.ElapsedMilliseconds / 350 & 1) == 0 ? "_" : string.Empty),
            $"Goal WPM: {studentGoalDraft,3}",
            $"Level: {WorldDisplayName(progress.CurrentWorld)}",
            $"Lesson: {StudentInformationLessonNames[Math.Clamp(studentLessonDraft, 1, 6) - 1]}",
        };
        var yPositions = new[] { 56, 72, 80, 88 };
        for (var index = 0; index < values.Length; index++)
        {
            font5.Draw(
                raster,
                studentInformation.Palette,
                values[index],
                CenteredX(64),
                yPositions[index],
                foregroundIndex: white);
        }

        var characterX = studentCharacterDraft switch
        {
            OriginalCharacter.Mario => 66,
            OriginalCharacter.Luigi => 132,
            _ => 208,
        };
        raster.DrawSprite(
            greetingUiObjects["prefbox_1"],
            studentInformation.Palette,
            CenteredX(characterX),
            130);
    }

    private void RenderLoadStudent()
    {
        DrawCenteredUiFrame(loadStudent);
        var white = loadStudent.Palette.FindNearest(255, 255, 255);
        var black = loadStudent.Palette.FindNearest(0, 0, 0);
        var blue = loadStudent.Palette.FindNearest(0, 0, 180);
        if (loadStudentNames.Length == 0)
        {
            font5.Draw(
                raster,
                loadStudent.Palette,
                "No Students!",
                CenteredX(112),
                96,
                foregroundIndex: black);
            return;
        }
        for (var index = 0; index < Math.Min(30, loadStudentNames.Length); index++)
        {
            var column = index / 10;
            var row = index % 10;
            var x = CenteredX(40 + column * 80);
            var y = 64 + row * 8;
            if (index == auxiliarySelection)
            {
                raster.FillRectangle(x, y, 75, 8, loadStudent.Palette.Argb[blue]);
            }
            font5.Draw(
                raster,
                loadStudent.Palette,
                loadStudentNames[index],
                x,
                y,
                8,
                foregroundIndex: index == auxiliarySelection ? white : black);
        }
    }

    private void RenderLessonTimes()
    {
        DrawCenteredUiFrame(lessonTimes);
        var black = lessonTimes.Palette.FindNearest(0, 0, 0);
        var labels = new[] { "Outside", "Underwater", "Underground", "Practice" };
        for (var index = 0; index < lessonTimesDraft.Length; index++)
        {
            var y = 70 + index * 20;
            font5.Draw(
                raster,
                lessonTimes.Palette,
                labels[index],
                CenteredX(66),
                y,
                foregroundIndex: black);
            font5.Draw(
                raster,
                lessonTimes.Palette,
                $"{lessonTimesDraft[index],2}  mins",
                CenteredX(190),
                y,
                foregroundIndex: black);
        }
    }

    private void RenderCertificate()
    {
        DrawCenteredUiFrame(certificate);
        var black = certificate.Palette.FindNearest(0, 0, 0);

        if (!HasAnyCertificate())
        {
            font5.Draw(
                raster,
                certificate.Palette,
                "You have not yet earned a\ncertificate! Keep trying!",
                CenteredX(42),
                91,
                foregroundIndex: black);
            return;
        }

        font5.Draw(
            raster,
            certificate.Palette,
            "You may print the following\ncertificates:",
            CenteredX(42),
            80,
            foregroundIndex: black);
        for (var index = 0; index < CertificateWorlds.Length; index++)
        {
            if (!progress.HasCertificate(CertificateWorlds[index]))
            {
                continue;
            }
            font5.Draw(
                raster,
                certificate.Palette,
                CertificateLevelNames[index],
                CenteredX(80),
                104 + index * 8,
                foregroundIndex: black);
        }
        font5.Draw(
            raster,
            certificate.Palette,
            "Make sure your printer is ready,\nand select line to print!",
            CenteredX(48),
            144,
            foregroundIndex: black);
    }

    private void RenderBlackboard()
    {
        DrawCenteredFrame(blackboard);
        DrawBlackboardContents();
    }

    private void DrawBlackboardContents()
    {
        // gamestat_services(0x64) renders the original debrief with FONT5.
        // Its text-grid coordinates are one-based, so (6,5) maps to (40,32)
        // and the value column at 18 maps to x=136.
        const byte labelColor = 16;
        const byte valueColor = 10;
        var result = lastResult ?? progress.Results.LastOrDefault();
        if (result is null)
        {
            return;
        }

        var labels = new[]
        {
            ("Keys Typed:", 32),
            ("Errors:", 48),
            ("Word/Min:", 64),
            ("Goal WPM:", 80),
            ("Accuracy:", 96),
            ("Lesson Time:", 120),
        };
        foreach (var (text, y) in labels)
        {
            font5.Draw(
                raster,
                blackboard.Palette,
                text,
                CenteredX(40),
                y,
                foregroundIndex: labelColor);
        }

        var state = BuildOriginalBlackboardState(result, progress.GoalWordsPerMinute);
        var values = new[]
        {
            (state.KeysTyped, 32),
            (state.Errors, 48),
            (state.WordsPerMinute, 64),
            (state.GoalWordsPerMinute, 80),
            (state.Accuracy, 96),
            (state.LessonTime, 120),
        };
        foreach (var (text, y) in values)
        {
            font5.Draw(
                raster,
                blackboard.Palette,
                text,
                CenteredX(136),
                y,
                foregroundIndex: valueColor);
        }
    }

    internal static OriginalBlackboardState BuildOriginalBlackboardState(
        SessionResult? result,
        int goalWordsPerMinute)
    {
        if (result is null)
        {
            return new OriginalBlackboardState("", "", "", "", "", "");
        }

        var accuracyPercent = Math.Clamp((int)Math.Round(result.Accuracy * 100), 0, 100);
        var seconds = Math.Max(0, (long)result.Seconds);
        return new OriginalBlackboardState(
            $"{Math.Max(0, result.AttemptedKeystrokes),4}",
            $"{Math.Max(0, result.Errors),4}",
            $"{Math.Max(0, (int)result.WordsPerMinute),3}",
            $"{Math.Clamp(goalWordsPerMinute, 5, 150),3}",
            $"{accuracyPercent,3}%",
            $"{seconds / 60,3}:{seconds % 60:00}");
    }

    private void RenderMainMenu()
    {
        DrawMainMenuBackdrop();
        var yellow = mainMenu.Palette.Argb[mainMenu.Palette.FindNearest(255, 255, 0)];
        raster.FillRectangle(0, 0, raster.Width, 8, yellow);
        font5.Draw(raster, mainMenu.Palette, "File", 0, 0);
        font5.Draw(raster, mainMenu.Palette, "Student", MainMenuStudentHeaderX, 0);
        font5.Draw(raster, mainMenu.Palette, "Lessons", MainMenuLessonsHeaderX, 0);

        // The original executable selects one Z8/Z9/Z52 welcome clip, then
        // selects from its 24-entry idle cue table after a randomized pause.
        // Both calls place the native AV decoder at x=5,y=20. The original
        // _movie_display routine saves the menu before playback and restores it
        // when playback ends, so no movie frame remains on the idle menu.
        if (menuMoviePlaying)
        {
            var frame = Math.Min(
                menuMovie.Frames.Count - 1,
                (int)(Math.Max(0, clock.ElapsedMilliseconds - menuMovieStarted) *
                      OriginalAvMovie.FramesPerSecond / 1000));
            raster.DrawIndexedImageWithTransparency(
                menuMovie.Frames[frame],
                OriginalAvMovie.Width,
                OriginalAvMovie.Height,
                menuMoviePalette,
                menuMovieTransparentPaletteEntries,
                MainMenuMovieX,
                20);
        }

        DrawMainMenuOverlay();
    }

    private int MainMenuCardStripX => raster.Width == ClassicFrameWidth
        ? 232
        : raster.Width - 88;

    private int MainMenuMovieX => raster.Width == ClassicFrameWidth
        ? 5
        : 5 + (raster.Width - ClassicFrameWidth) / 4;

    private int MainMenuStudentHeaderX => raster.Width == ClassicFrameWidth
        ? 112
        : (raster.Width - 56) / 2;

    private int MainMenuLessonsHeaderX => raster.Width == ClassicFrameWidth
        ? 232
        : raster.Width - 88;

    private void DrawMainMenuBackdrop()
    {
        if (raster.Width == ClassicFrameWidth)
        {
            raster.CopyFrom(mainMenu);
            return;
        }

        // The left 232 pixels contain the original repeating purple wallpaper;
        // the final 88 contain the lesson-card rail and its shadow. Recompose
        // those regions so the controls occupy the full selected canvas instead
        // of leaving a centered 4:3 island or cloning clickable cards.
        DrawMarioWallpaper();
        raster.DrawIndexedRegion(
            mainMenu.Pixels,
            ClassicFrameWidth,
            FrameHeight,
            mainMenu.Palette,
            new Rectangle(232, 0, 88, FrameHeight),
            MainMenuCardStripX,
            0);
    }

    private void DrawMainMenuOverlay()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            var yellow = mainMenu.Palette.FindNearest(255, 255, 0);
            var black = mainMenu.Palette.FindNearest(0, 0, 0);
            raster.FillRectangle(CenteredX(39), 72, 242, 52, mainMenu.Palette.Argb[black]);
            raster.FillRectangle(CenteredX(41), 74, 238, 48, mainMenu.Palette.Argb[yellow]);
            font5.Draw(
                raster,
                mainMenu.Palette,
                statusMessage,
                CenteredX(57),
                84,
                27,
                foregroundIndex: black);
            return;
        }

        if (rapPlaying)
        {
            var yellow = mainMenu.Palette.FindNearest(255, 255, 0);
            var black = mainMenu.Palette.FindNearest(0, 0, 0);
            raster.FillRectangle(CenteredX(47), 76, 226, 45, mainMenu.Palette.Argb[black]);
            raster.FillRectangle(CenteredX(49), 78, 222, 41, mainMenu.Palette.Argb[yellow]);
            font5.Draw(
                raster,
                mainMenu.Palette,
                "Playing CD Track",
                CenteredX(89),
                84,
                foregroundIndex: black);
            font5.Draw(
                raster,
                mainMenu.Palette,
                "Press any key to stop",
                CenteredX(77),
                102,
                foregroundIndex: black);
            return;
        }

        if (menuPopup == MainMenuPopup.None)
        {
            return;
        }

        var items = PopupItems(menuPopup);
        var (left, top, width, height) = PopupBounds(menuPopup);
        var yellowIndex = mainMenu.Palette.FindNearest(255, 255, 0);
        var blackIndex = mainMenu.Palette.FindNearest(0, 0, 0);
        raster.FillRectangle(left - 1, top - 1, width + 2, height + 2, mainMenu.Palette.Argb[blackIndex]);
        raster.FillRectangle(left, top, width, height, mainMenu.Palette.Argb[yellowIndex]);
        for (var index = 0; index < items.Length; index++)
        {
            if (index == menuSelection)
            {
                raster.FillRectangle(left + 1, top + index * 9, width - 2, 9, mainMenu.Palette.Argb[blackIndex]);
            }
            var marker = menuPopup switch
            {
                MainMenuPopup.Lessons when index == selectedLesson => "*",
                MainMenuPopup.File when index == 4 && audio.MusicEnabled => "*",
                MainMenuPopup.File when index == 5 && audio.SoundEffectsEnabled => "*",
                MainMenuPopup.File when index == 6 && audio.SpeechEnabled => "*",
                MainMenuPopup.File when index == 8 &&
                    progress.DisplayMode == NativeDisplayMode.Widescreen => "*",
                MainMenuPopup.File when index == 9 &&
                    progress.DisplayMode == NativeDisplayMode.UltraWidescreen => "*",
                _ => " ",
            };
            font5.Draw(
                raster,
                mainMenu.Palette,
                marker + items[index],
                left + 3,
                top + index * 9,
                foregroundIndex: index == menuSelection ? yellowIndex : blackIndex);
        }
    }

    private void OpenMenu(MainMenuPopup popup)
    {
        statusMessage = null;
        menuPopup = menuPopup == popup ? MainMenuPopup.None : popup;
        menuSelection = popup == MainMenuPopup.Lessons ? selectedLesson : 0;
        RenderAndNotify();
    }

    private bool TryActivateMenuShortcut(MainMenuPopup popup, Keys key)
    {
        var item = popup switch
        {
            MainMenuPopup.File => key switch
            {
                Keys.A => 0,
                Keys.F1 or Keys.H => 1,
                Keys.D => 2,
                Keys.F2 or Keys.K => 3,
                Keys.M => 4,
                Keys.S => 5,
                Keys.I => 6,
                Keys.R => 7,
                Keys.F10 => 8,
                Keys.F9 => 9,
                Keys.Q => 10,
                _ => -1,
            },
            MainMenuPopup.Student => key switch
            {
                Keys.W => 0,
                Keys.L => 1,
                Keys.E => 2,
                Keys.T => 3,
                Keys.C => 4,
                _ => -1,
            },
            _ => -1,
        };
        if (item < 0)
        {
            return false;
        }
        suppressNextCharacter = key is >= Keys.A and <= Keys.Z or
            >= Keys.D0 and <= Keys.D9 or >= Keys.NumPad0 and <= Keys.NumPad9;
        ActivateMenuItem(popup, item);
        return true;
    }

    private void ActivateMenuItem(MainMenuPopup popup, int index)
    {
        menuSelection = index;
        switch (popup)
        {
            case MainMenuPopup.File:
                if (index == 0)
                {
                    Show(OriginalScreen.About);
                }
                else if (index == 1)
                {
                    OpenHelp();
                }
                else if (index == 2)
                {
                    ShowDemoInstructions();
                }
                else if (index == 3)
                {
                    Show(OriginalScreen.Keyboard);
                }
                else if (index == 4)
                {
                    ToggleMusic();
                }
                else if (index == 5)
                {
                    ToggleSoundEffects();
                }
                else if (index == 6)
                {
                    ToggleSpeech();
                }
                else if (index == 7)
                {
                    PlayRap();
                }
                else if (index == 8)
                {
                    ToggleWidescreen();
                }
                else if (index == 9)
                {
                    ToggleUltraWidescreen();
                }
                else if (index == 10)
                {
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    menuPopup = MainMenuPopup.None;
                    RenderAndNotify();
                }
                break;
            case MainMenuPopup.Student:
                if (index == 0)
                {
                    OpenNewStudentInformation();
                }
                else if (index == 1)
                {
                    OpenLoadStudent();
                }
                else if (index == 2)
                {
                    OpenStudentInformation();
                }
                else if (index == 3)
                {
                    OpenLessonTimes();
                }
                else if (index == 4)
                {
                    Show(OriginalScreen.Certificate);
                }
                else
                {
                    menuPopup = MainMenuPopup.None;
                    RenderAndNotify();
                }
                break;
            case MainMenuPopup.Lessons:
                selectedLesson = Math.Clamp(index, 0, LessonMenuItems.Length - 1);
                progress.SetLessonLevel(selectedLesson + 1);
                menuPopup = MainMenuPopup.None;
                RenderAndNotify();
                break;
        }
    }

    private void ToggleMusic()
    {
        audio.MusicEnabled = !audio.MusicEnabled;
        if (!audio.MusicEnabled)
        {
            audio.StopMusic();
        }
        else
        {
            ResumeWorldMusic();
        }
        PersistAudioSettings();
        menuPopup = MainMenuPopup.None;
        RenderAndNotify();
    }

    private void ToggleSoundEffects()
    {
        audio.SoundEffectsEnabled = !audio.SoundEffectsEnabled;
        if (!audio.SoundEffectsEnabled)
        {
            audio.StopSoundEffects();
        }
        PersistAudioSettings();
        menuPopup = MainMenuPopup.None;
        RenderAndNotify();
    }

    private OriginalScreen? MainMenuWorldAt(int x, int y)
    {
        if (x < MainMenuCardStripX + 4 || x >= MainMenuCardStripX + 67)
        {
            return null;
        }
        return y switch
        {
            >= 12 and < 39 => OriginalScreen.Outside,
            >= 43 and < 70 => OriginalScreen.Water,
            >= 74 and < 101 => OriginalScreen.Castle,
            >= 105 and < 132 => OriginalScreen.Practice,
            _ => null,
        };
    }

    private void ToggleSpeech()
    {
        audio.SpeechEnabled = !audio.SpeechEnabled;
        if (audio.SpeechEnabled)
        {
            ResumeMainMenuMovieAudio();
        }
        else
        {
            audio.StopSpeech();
        }
        PersistAudioSettings();
        menuPopup = MainMenuPopup.None;
        RenderAndNotify();
    }

    private void ToggleWidescreen()
    {
        SetDisplayMode(progress.DisplayMode == NativeDisplayMode.Widescreen
            ? NativeDisplayMode.Classic
            : NativeDisplayMode.Widescreen);
    }

    private void ToggleUltraWidescreen()
    {
        SetDisplayMode(progress.DisplayMode == NativeDisplayMode.UltraWidescreen
            ? NativeDisplayMode.Classic
            : NativeDisplayMode.UltraWidescreen);
    }

    private void SetDisplayMode(NativeDisplayMode mode)
    {
        progress.SetDisplaySettings(mode);
        menuPopup = MainMenuPopup.None;
        DisplayModeChanged?.Invoke(this, EventArgs.Empty);
        RenderAndNotify();
    }

    private void PersistAudioSettings() => progress.SetAudioSettings(
        audio.MusicEnabled,
        audio.SoundEffectsEnabled,
        audio.SpeechEnabled);

    private void ResumeWorldMusic()
    {
        var musicIndex = screen switch
        {
            OriginalScreen.Title or OriginalScreen.MainMenu => 0,
            OriginalScreen.Outside => 0,
            OriginalScreen.Water => 1,
            OriginalScreen.Castle => 2,
            OriginalScreen.Practice => 3,
            _ => -1,
        };
        if (musicIndex >= 0)
        {
            PlayMusicSequence(musicIndex);
        }
    }

    private void PlayMusicSequence(int zeroBasedIndex)
    {
        var index = Math.Clamp(zeroBasedIndex, 0, musicSequences.Length - 1);
        musicPlayRequestCount++;
        lastMusicSequenceNumber = index + 1;
        audio.PlayMusic(musicSequences[index], loop: true);
    }

    internal static IReadOnlyList<string>? EligibleWorldCues(
        OriginalScreen world,
        OriginalCharacter character)
    {
        var source = world switch
        {
            OriginalScreen.Outside => OriginalAvCues.OutsideWorld,
            OriginalScreen.Water => OriginalAvCues.WaterWorld,
            OriginalScreen.Castle => OriginalAvCues.CastleWorld,
            _ => null,
        };
        if (source is null)
        {
            return null;
        }

        return source.Where(cue =>
                (!cue.EndsWith("65", StringComparison.Ordinal) || character == OriginalCharacter.Luigi) &&
                (!cue.EndsWith("66", StringComparison.Ordinal) || character == OriginalCharacter.Princess) &&
                (!cue.Equals("Z68", StringComparison.Ordinal) || character == OriginalCharacter.Mario))
            .ToArray();
    }

    internal static IReadOnlyList<string> EligibleDebriefCues(
        OriginalScreen world,
        double previousWpm,
        double currentWpm)
    {
        if (previousWpm > currentWpm)
        {
            return OriginalAvCues.ReportFail
                .Where(cue => cue != "Z76" || world is OriginalScreen.Outside or OriginalScreen.Water)
                .ToArray();
        }

        var improvement = currentWpm - previousWpm;
        return OriginalAvCues.WaterDebrief
            .Where(cue => improvement < 10 || cue is not ("Z80" or "Z83"))
            .ToArray();
    }

    private static string PickCue(IReadOnlyList<string> cues) =>
        cues[Random.Shared.Next(cues.Count)];

    private static int NextMainMenuIdleDelay() =>
        Random.Shared.Next(30, 50) * 1000;

    private void StartMainMenuMovie(string cue)
    {
        menuMovie = AvDecoder.Decode(assets.ReadAllBytes($"MOVIES/{cue}.AV"));
        menuMoviePalette = mainMenu.Palette.WithRange(menuMovie.Palette, 44, 212);
        menuMovieTransparentPaletteEntries = CreateMenuMovieTransparency(menuMoviePalette);
        menuMovieStarted = clock.ElapsedMilliseconds;
        menuMoviePlaying = true;
        menuIdleMovieDue = long.MaxValue;
        audio.PlayAv(menuMovie, loop: false);
    }

    private bool[] CreateMenuMovieTransparency(VgaPalette palette)
    {
        // Every menu AV was recorded over the four-shade purple wallpaper.
        // Its palette preserves the red/green DAC channels but slightly shifts
        // blue, so drawing the recorded background makes the lettering appear
        // pasted into a rectangle. Key those four shades out and let the one
        // continuous responsive menu wallpaper show through behind Mario.
        var wallpaperChannels = new HashSet<(byte Red, byte Green)>();
        for (var y = 8; y < FrameHeight; y++)
        {
            for (var x = 0; x < 232; x++)
            {
                var color = Color.FromArgb(
                    mainMenu.Palette.Argb[mainMenu.Pixels[y * ClassicFrameWidth + x]]);
                if (color.B > color.R)
                {
                    wallpaperChannels.Add((color.R, color.G));
                }
            }
        }

        var transparent = new bool[palette.Argb.Length];
        for (var index = 0; index < transparent.Length; index++)
        {
            var color = Color.FromArgb(palette.Argb[index]);
            transparent[index] = color.B > color.R &&
                                 wallpaperChannels.Contains((color.R, color.G));
        }
        return transparent;
    }

    internal void SetMainMenuMovieFrameForTest(int frame)
    {
        if (screen != OriginalScreen.MainMenu || !menuMoviePlaying || menuMovie.Frames.Count == 0)
        {
            throw new InvalidOperationException("The main-menu movie is not active.");
        }

        var clampedFrame = Math.Clamp(frame, 0, menuMovie.Frames.Count - 1);
        var elapsedMilliseconds =
            (clampedFrame * 1000L + OriginalAvMovie.FramesPerSecond - 1) /
            OriginalAvMovie.FramesPerSecond;
        menuMovieStarted = clock.ElapsedMilliseconds - elapsedMilliseconds;
        Render();
    }

    private void UpdateMainMenuMovie()
    {
        if (menuMoviePlaying)
        {
            var frame = (int)(Math.Max(0, clock.ElapsedMilliseconds - menuMovieStarted) *
                              OriginalAvMovie.FramesPerSecond / 1000);
            if (frame < menuMovie.Frames.Count)
            {
                return;
            }

            menuMoviePlaying = false;
            menuIdleMovieDue = clock.ElapsedMilliseconds + NextMainMenuIdleDelay();
        }

        if (menuPopup == MainMenuPopup.None &&
            string.IsNullOrEmpty(statusMessage) &&
            !rapPlaying &&
            clock.ElapsedMilliseconds >= menuIdleMovieDue)
        {
            StartMainMenuMovie(PickCue(OriginalAvCues.MainMenuIdle));
        }
    }

    private void NoteMainMenuActivity()
    {
        if (screen == OriginalScreen.MainMenu && !menuMoviePlaying)
        {
            menuIdleMovieDue = clock.ElapsedMilliseconds + NextMainMenuIdleDelay();
        }
    }

    private void ResumeMainMenuMovieAudio()
    {
        if (screen == OriginalScreen.MainMenu && menuMoviePlaying)
        {
            audio.PlayAv(menuMovie, loop: false);
        }
    }

    private void PlaySoundEffect(int oneBasedNumber)
    {
        var soundIndex = OriginalSoundEffectSlot(oneBasedNumber) - 1;
        var variants = soundEffectSequences[soundIndex];
        if (variants.Length == 0)
        {
            return;
        }
        // The DOS AIL call registers sequence zero from the selected RLD.
        // Additional EVNT chunks are not randomized alternates.
        audio.PlayEffect(variants[0]);
    }

    internal static int OriginalSoundEffectSlot(int requestedNumber) =>
        requestedNumber == 11 ? 10 : Math.Clamp(requestedNumber, 1, 10);

    private void PlayRap()
    {
        menuPopup = MainMenuPopup.None;
        rapPlaying = true;
        audio.Stop();
        audio.PlayMusicWave(EmbeddedAssets.OpenCdAudioTrack(), loop: false);
        RenderAndNotify();
    }

    private static string[] PopupItems(MainMenuPopup popup) => popup switch
    {
        MainMenuPopup.File => FileMenuItems,
        MainMenuPopup.Student => StudentMenuItems,
        MainMenuPopup.Lessons => LessonMenuItems,
        _ => [],
    };

    private (int Left, int Top, int Width, int Height) PopupBounds(MainMenuPopup popup)
    {
        var items = PopupItems(popup);
        return popup switch
        {
            MainMenuPopup.File => (0, 9, 144, items.Length * 9),
            MainMenuPopup.Student => (
                raster.Width == ClassicFrameWidth ? 104 : (raster.Width - 120) / 2,
                9,
                120,
                items.Length * 9),
            MainMenuPopup.Lessons => (
                raster.Width == ClassicFrameWidth ? 168 : raster.Width - 152,
                9,
                151,
                items.Length * 9),
            _ => (0, 0, 0, 0),
        };
    }

    private void InitializeBonusState()
    {
        var lessonSeconds = Math.Max(31, CurrentLessonTimeMinutes() * 60);
        bonusFlag = 1;
        bonusX = bonusRandom.Next(250) + 20;
        bonusY = bonusRandom.Next(30) + 5;
        bonusTimer = OriginalBonusDelaySeconds(lessonSeconds, bonusRandom.Next(0x8000));
        bonusMotionFrame = 0;
        bonusCharacter = OriginalBonusCharacter(bonusRandom.Next(0x8000));
        bonusLastSecondAt = sceneStarted;
    }

    internal static int OriginalBonusDelaySeconds(int lessonSeconds, int randomValue) =>
        Math.Clamp(randomValue, 0, 0x7fff) * Math.Max(1, lessonSeconds - 30) / 0x8000 + 15;

    internal static char OriginalBonusCharacter(int randomValue) =>
        (char)(Math.Clamp(randomValue, 0, 0x7fff) * 15 / 0x8000 + 0x21);

    private bool TickBonusCountdown(long stateTime)
    {
        while (bonusLastSecondAt + 1000 <= stateTime && bonusTimer > 0)
        {
            bonusLastSecondAt += 1000;
            bonusTimer--;
        }
        return bonusTimer <= 0;
    }

    private void StepOutsideBonus(long stateTime)
    {
        if (bonusFlag == 1 && TickBonusCountdown(stateTime))
        {
            bonusFlag = 2;
            bonusTimer = 45;
        }
        else if (bonusFlag == 2)
        {
            bonusX += Math.Sign(outsideMarioX - bonusX);
            bonusTimer--;
            if (bonusTimer < 1)
            {
                bonusFlag = 0;
            }
        }
    }

    private void StepWaterBonus()
    {
        if (bonusFlag == 1 && waterMarioX > 239)
        {
            bonusFlag = 2;
            bonusX = bonusRandom.Next(100) + 20;
            bonusY = 200;
        }
        if (bonusFlag != 2)
        {
            return;
        }

        if (bonusRandom.Next(2) != 0)
        {
            bonusX += bonusRandom.Next(2) == 0 ? 1 : -1;
        }
        bonusX = Math.Clamp(bonusX, 10, 130);
        bonusY -= 3;
        if (bonusY < 80)
        {
            bonusFlag = 0;
            bonusY = 200;
        }
    }

    private void StepCastleBonus(long stateTime)
    {
        if (castleSequencePhase is 1 or 3)
        {
            return;
        }
        if (bonusFlag == 1 && TickBonusCountdown(stateTime))
        {
            bonusFlag = 2;
            bonusMotionFrame = 0;
            if (castleSequencePhase == 0)
            {
                bonusX = CastleBonusXPositions[bonusRandom.Next(CastleBonusXPositions.Length)];
                bonusY = 90;
            }
            else
            {
                bonusX = 61;
                bonusY = 15;
            }
        }
        if (bonusFlag != 2)
        {
            return;
        }

        if (castleSequencePhase == 0)
        {
            bonusY--;
            if (bonusY < 45)
            {
                bonusFlag = 0;
            }
            return;
        }

        bonusMotionFrame++;
        bonusX += 2;
        if (bonusMotionFrame < 20)
        {
            bonusY += 2;
        }
        else if (bonusMotionFrame > 59)
        {
            bonusY -= 2;
        }
        if (bonusMotionFrame > 80)
        {
            bonusFlag = 0;
        }
    }

    private void StartBonusAnimation()
    {
        if (screen is not (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle))
        {
            return;
        }

        bonusAnimationDestination = screen;
        bonusFlag = screen == OriginalScreen.Outside ? 2 : 3;
        bonusTimer = 0;
        var characterPrefix = CharacterPrefix(progress.Character);
        var suffix = screen switch
        {
            OriginalScreen.Water => "SWM",
            OriginalScreen.Castle => "CAS",
            _ => "OUT",
        };
        var backdrop = screen switch
        {
            OriginalScreen.Water => water,
            OriginalScreen.Castle => new CrxImage(
                castleScenePalettes[(int)progress.Character],
                CurrentCastleBackdropPixels()),
            _ => outsideBonusBackdrop,
        };
        activeBonusPalette = screen switch
        {
            OriginalScreen.Water => waterPalettes[(int)progress.Character],
            OriginalScreen.Castle => castleScenePalettes[(int)progress.Character],
            _ => outsideBonusPalettes[(int)progress.Character],
        };
        activeBonusBackgroundFrames = BackAnimationDecoder.DecodeBan(
            assets.ReadAllBytes($"{characterPrefix}{suffix}.BAN"),
            backdrop.Pixels);
        activeBonusCharacterFrames = BackAnimationDecoder.DecodeAni(
            assets.ReadAllBytes($"{characterPrefix}{suffix}.ANI"));
        activeBonusSoundFrames = screen switch
        {
            OriginalScreen.Water => WaterBonusSoundFrames,
            OriginalScreen.Castle => CastleBonusSoundFrames,
            _ => OutsideBonusSoundFrames,
        };
        bonusAnimationLastFrame = -1;
        bonusAnimationStarted = clock.ElapsedMilliseconds;
        screen = OriginalScreen.BonusAnimation;
        RenderAndNotify();
    }

    private void UpdateBonusAnimation()
    {
        var frameCount = ActiveBonusAnimationFrameCount;
        if (frameCount == 0)
        {
            FinishBonusAnimation();
            return;
        }
        var frame = (int)((clock.ElapsedMilliseconds - bonusAnimationStarted) /
                          BonusAnimationFrameMilliseconds);
        for (var crossed = bonusAnimationLastFrame + 1;
             crossed <= Math.Min(frame, frameCount - 1);
             crossed++)
        {
            if (Array.IndexOf(activeBonusSoundFrames, crossed + 1) >= 0)
            {
                PlaySoundEffect(7);
            }
        }
        bonusAnimationLastFrame = Math.Max(bonusAnimationLastFrame, frame);
        if (frame >= frameCount)
        {
            FinishBonusAnimation();
            return;
        }
        RenderAndNotify();
    }

    private void FinishBonusAnimation()
    {
        var now = clock.ElapsedMilliseconds;
        var paused = Math.Max(0, now - bonusAnimationStarted);
        if (typingStarted >= 0)
        {
            typingStarted += paused;
        }
        if (lessonCompleteAt >= 0)
        {
            lessonCompleteAt += paused;
        }
        waterLastKeyAt += paused;
        castleLastKeyAt += paused;
        outsideLastStateUpdate = now;
        waterLastStateUpdate = now;
        castleLastStateUpdate = now;
        screen = bonusAnimationDestination;
        bonusFlag = 0;
        activeBonusBackgroundFrames = null;
        activeBonusCharacterFrames = null;
        activeBonusPalette = null;
        activeBonusSoundFrames = [];
        bonusAnimationLastFrame = -1;
        RenderAndNotify();
    }

    internal void ActivateBonusForTest(char character = '!')
    {
        if (screen is not (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle))
        {
            throw new InvalidOperationException("A gameplay world must be active.");
        }
        bonusCharacter = character;
        bonusFlag = 2;
        bonusTimer = 45;
        bonusX = 100;
        bonusY = 80;
        Render();
    }

    internal void SetBonusAnimationFrameForTest(int frame)
    {
        if (screen != OriginalScreen.BonusAnimation || ActiveBonusAnimationFrameCount == 0)
        {
            throw new InvalidOperationException("Bonus animation is not active.");
        }
        bonusAnimationStarted = clock.ElapsedMilliseconds -
                                Math.Clamp(frame, 0, ActiveBonusAnimationFrameCount - 1) *
                                BonusAnimationFrameMilliseconds;
        Render();
    }

    internal void FinishBonusAnimationForTest()
    {
        if (screen != OriginalScreen.BonusAnimation)
        {
            throw new InvalidOperationException("Bonus animation is not active.");
        }
        FinishBonusAnimation();
    }

    private void InitializeOutsideState()
    {
        outsideTargets.Clear();
        outsideParticles.Clear();
        outsideSelectedTarget = null;
        outsideNextPromptIndex = 0;
        outsideMarioX = 20;
        outsideMarioY = 90;
        outsideMarioPhase = 0;
        outsideJumpFrame = 0;
        outsideDisplayedJumpFrame = -1;
        outsideMarioJumpX = 0;
        // The DOS world starts with one tick of ground motion before Mario's
        // first update derives the normal speed from the leading target.
        outsideForwardSpeed = 1;
        outsideRunCounter = 0;
        outsideIdleFrames = 0;
        outsideGroundOffset = 0;
        outsideAnimationCounter = 0;
        outsideLastStateUpdate = sceneStarted;
        InitializeBonusState();
        SpawnOutsideTarget(initial: true);
        SelectOutsideTarget();
    }

    private void UpdateOutsideState(long now)
    {
        if (outsideLastStateUpdate <= 0)
        {
            outsideLastStateUpdate = now;
            return;
        }

        var steps = 0;
        while (outsideLastStateUpdate + OriginalFrameMilliseconds <= now && steps < 200)
        {
            outsideLastStateUpdate += OriginalFrameMilliseconds;
            StepOutsideState();
            steps++;
        }
        if (steps == 200)
        {
            outsideLastStateUpdate = now;
        }
    }

    private void StepOutsideState()
    {
        StepOutsideBonus(outsideLastStateUpdate);
        outsideGroundOffset = (outsideGroundOffset + outsideForwardSpeed) % 20;
        StepOutsideMario();

        if (outsideTargets.Count < 10 &&
            outsideTargets.All(target => target.X <= OutsideViewportWidth - 100))
        {
            SpawnOutsideTarget();
        }

        foreach (var target in outsideTargets)
        {
            var previousX = target.X;
            target.X -= outsideForwardSpeed;
            if (target.HitFramesRemaining > 0)
            {
                target.HitFramesRemaining--;
                if (target.HitFramesRemaining == 0)
                {
                    if (target.Kind == 1)
                    {
                        SpawnOutsideBrickParticles(previousX, target.Y);
                        PlaySoundEffect(4);
                    }
                    else
                    {
                        PlaySoundEffect(3);
                    }
                    target.X = -1;
                }
            }
        }
        outsideTargets.RemoveAll(target => target.X < 0);
        StepOutsideParticles();
        SelectOutsideTarget();
        outsideAnimationCounter = (outsideAnimationCounter + 1) % 12;
    }

    private void SpawnOutsideBrickParticles(int x, int y)
    {
        outsideParticles.Add(new OutsideParticle(x, y, -2, -6));
        outsideParticles.Add(new OutsideParticle(x, y + 8, -2, -3));
        outsideParticles.Add(new OutsideParticle(x + 10, y, 2, -6));
        outsideParticles.Add(new OutsideParticle(x + 10, y + 8, 2, -3));
    }

    private void StepOutsideParticles()
    {
        foreach (var particle in outsideParticles)
        {
            particle.X += particle.HorizontalVelocity - outsideForwardSpeed;
            particle.Y += particle.VerticalVelocity;
            particle.VerticalVelocity++;
        }
        outsideParticles.RemoveAll(particle =>
            particle.X < 0 || particle.X > OutsideViewportWidth - 11 ||
            particle.Y < 0 || particle.Y > 131);
    }

    private void StepOutsideMario()
    {
        if (outsideMarioPhase == 0 && outsideSelectedTarget is not null)
        {
            outsideMarioJumpX = outsideSelectedTarget.X - 10;
        }
        outsideMarioJumpX -= outsideForwardSpeed;

        var horizontalDelta = outsideSelectedTarget is null
            ? 0
            : OriginalOutsideHorizontalDelta(outsideMarioX, outsideMarioJumpX);
        outsideMarioX = Math.Max(0, outsideMarioX + horizontalDelta);

        var targetX = outsideSelectedTarget?.X ?? 319;
        // Wide mode may stage the next target beyond x=319. Preserve the DOS
        // speed ceiling until that target reaches the original playfield.
        outsideForwardSpeed = OriginalOutsideForwardSpeed(
            Math.Min(targetX, ClassicFrameWidth - 1));

        if (outsideMarioPhase == 0)
        {
            outsideDisplayedJumpFrame = -1;
            outsideMarioY = 90;
            if (horizontalDelta == 0)
            {
                outsideIdleFrames = Math.Min(10, outsideIdleFrames + 1);
            }
            else
            {
                outsideIdleFrames = 0;
                outsideRunCounter += Math.Abs(horizontalDelta);
            }
            return;
        }

        if (outsideMarioPhase is not (1 or 2))
        {
            return;
        }

        outsideDisplayedJumpFrame = outsideJumpFrame;
        outsideMarioY = 90 + OriginalOutsideJumpYOffset(outsideJumpFrame);
        outsideJumpFrame++;
        if (outsideJumpFrame > 11)
        {
            outsideJumpFrame = 0;
            outsideMarioY = 85;
            outsideMarioPhase = outsideMarioPhase == 2 ? 3 : 0;
        }
    }

    private int OutsideViewportWidth => DisplayFrameWidth(progress.DisplayMode);

    private void SpawnOutsideTarget(bool initial = false)
    {
        if (prompt.Length == 0 || outsideTargets.Count >= 10)
        {
            return;
        }

        int promptIndex;
        var attempts = 0;
        do
        {
            promptIndex = outsideNextPromptIndex++;
            attempts++;
        }
        while (PromptCharacterAt(prompt, promptIndex) <= ' ' && attempts <= prompt.Length);

        var kind = outsideRandom.Next(2) + 1;
        var viewportWidth = initial ? ClassicFrameWidth : OutsideViewportWidth;
        outsideTargets.Add(kind == 1
            ? new OutsideTarget(
                promptIndex,
                kind,
                x: viewportWidth - 27,
                OutsideBrickYPositions[outsideRandom.Next(OutsideBrickYPositions.Length)],
                spriteXOffset: 0)
            : new OutsideTarget(
                promptIndex,
                kind,
                x: viewportWidth - 37,
                y: 123,
                spriteXOffset: 30));
    }

    private void SelectOutsideTarget()
    {
        outsideSelectedTarget = outsideTargets.FirstOrDefault(target => target.HasCharacter);
    }

    internal static int OriginalOutsideJumpYOffset(int frame) =>
        OutsideJumpYOffsets[Math.Clamp(frame, 0, OutsideJumpYOffsets.Length - 1)];

    internal static int OriginalOutsideForwardSpeed(int targetX) =>
        5 * (targetX < 0 ? 319 : targetX) / 160;

    internal static int OriginalOutsideCursorFrame(int animationCounter) =>
        OutsideCursorShapeFrames[Math.Abs(animationCounter % OutsideCursorShapeFrames.Length)];

    internal static int OriginalOutsideHorizontalDelta(int marioX, int marioJumpX)
    {
        if (marioX == marioJumpX)
        {
            return 0;
        }
        var delta = (marioJumpX - marioX) / 8;
        delta += marioX < marioJumpX ? 1 : -2;
        return Math.Clamp(delta, -8, 8);
    }

    internal static OriginalHandElement? OriginalHandForCharacter(char character)
    {
        if (character is < ' ' or > '~')
        {
            return null;
        }

        var element = OriginalHandElementTable[character - ' '];
        if (element > 0x27)
        {
            element -= 0x28;
        }

        var rightHand = element > 5;
        if (rightHand)
        {
            element--;
        }
        if (element > 9)
        {
            return null;
        }

        return new OriginalHandElement(
            element + 1,
            rightHand,
            OriginalHandXOffsets[element],
            OriginalHandYOffsets[element]);
    }

    internal void AdvanceOutsideFramesForTest(int count)
    {
        if (screen != OriginalScreen.Outside)
        {
            throw new InvalidOperationException("Outdoor state is not active.");
        }
        for (var frame = 0; frame < Math.Max(0, count); frame++)
        {
            outsideLastStateUpdate += OriginalFrameMilliseconds;
            StepOutsideState();
        }
        Render();
    }

    internal void PrepareOutsideTargetForTest(int kind)
    {
        if (screen != OriginalScreen.Outside || kind is not (1 or 2))
        {
            throw new InvalidOperationException("An outdoor brick or turtle target is required.");
        }
        outsideTargets.Clear();
        outsideParticles.Clear();
        outsideTargets.Add(new OutsideTarget(
            position,
            kind,
            x: kind == 1 ? 293 : 283,
            y: kind == 1 ? OutsideBrickYPositions[0] : 123,
            spriteXOffset: kind == 1 ? 0 : 30));
        outsideSelectedTarget = outsideTargets[0];
        outsideNextPromptIndex = position + 1;
        outsideMarioPhase = 0;
        outsideJumpFrame = 0;
        outsideDisplayedJumpFrame = -1;
        outsideMarioX = 20;
        outsideMarioY = 90;
        outsideMarioJumpX = outsideTargets[0].X - 10;
        outsideForwardSpeed = 1;
        outsideGroundOffset = 0;
        outsideAnimationCounter = 0;
        Render();
    }

    private void InitializeWaterState()
    {
        waterMarioX = 20;
        waterMarioY = 112;
        waterSwimFrame = 0;
        waterSwimCounter = 0;
        waterHorizontalDelay = 0;
        waterPeakWordsPerMinute = 10;
        waterCreatureX = WaterCreatureX(waterMarioX, waterMarioY);
        waterCreatureY = waterMarioY - 5;
        waterCreatureFrame = 0;
        waterCreatureKind = 1;
        waterCreaturePreviousPlotX = 0;
        waterCreatureDisplayX = waterCreatureX - 2;
        waterCreatureDisplayY = waterCreatureY;
        waterCreatureDisplayFrame = 0;
        waterCreatureDisplayKind = 1;
        waterBubbles.Clear();
        waterFailurePhase = 0;
        waterLost = false;
        waterLastKeyAt = sceneStarted;
        waterLastStateUpdate = sceneStarted;
        InitializeBonusState();
    }

    private void UpdateWaterState(long now)
    {
        if (waterLastStateUpdate <= 0)
        {
            waterLastStateUpdate = now;
            return;
        }

        var steps = 0;
        while (waterLastStateUpdate + OriginalFrameMilliseconds <= now && steps < 200)
        {
            waterLastStateUpdate += OriginalFrameMilliseconds;
            StepWaterState(waterLastStateUpdate);
            steps++;
        }
        if (steps == 200)
        {
            waterLastStateUpdate = now;
        }
    }

    private void StepWaterState(long stateTime)
    {
        if (waterFailurePhase == 0 &&
            OriginalElapsedTicks(stateTime - waterLastKeyAt) > 20)
        {
            waterFailurePhase = 1;
        }

        var currentWordsPerMinute = CalculateOriginalWordsPerMinuteFromTicks(
            position,
            errors,
            OriginalElapsedTicks(stateTime - typingStarted));
        waterPeakWordsPerMinute = Math.Max(waterPeakWordsPerMinute, currentWordsPerMinute);
        var targetY = waterFailurePhase == 0
            ? OriginalWaterTargetY(currentWordsPerMinute, waterPeakWordsPerMinute)
            : waterFailurePhase == 2 ? 200 : waterMarioY;
        if (waterMarioY > targetY)
        {
            waterMarioY--;
        }
        else if (waterMarioY < targetY)
        {
            waterMarioY++;
        }

        waterSwimCounter++;
        if ((waterSwimCounter & 1) == 0)
        {
            waterSwimFrame = (waterSwimFrame + 1) % 6;
        }

        waterHorizontalDelay--;
        if (waterHorizontalDelay < 0)
        {
            if (waterFailurePhase == 0)
            {
                waterMarioX++;
            }
            waterHorizontalDelay = (waterMarioY - 79) / 16 / 5 + 1;
        }
        if (waterMarioX > 319)
        {
            waterMarioX = -55;
        }

        if (waterRandom.Next(10) == 0)
        {
            var position = WaterAmbientBubblePositions[waterRandom.Next(WaterAmbientBubblePositions.Length)];
            SpawnWaterBubble(position.X, position.Y);
        }
        if (waterRandom.Next(15) == 0)
        {
            SpawnWaterBubble(waterMarioX + 38, waterMarioY + 15);
            PlaySoundEffect(5);
        }

        StepWaterCreature();
        StepWaterBubbles();
        StepWaterBonus();
        if (waterFailurePhase == 2 && waterMarioY > 198)
        {
            waterLost = true;
        }
    }

    private void StepWaterCreature()
    {
        if (waterCreatureKind == 0)
        {
            waterCreatureKind = 1;
        }
        var kindForFrame = waterCreatureKind;
        var creatureCycle = kindForFrame == 1 ? 10 : 20;
        var frame = waterCreatureFrame + 1;
        if (frame >= creatureCycle)
        {
            frame = 0;
        }

        int x;
        var baseY = waterCreatureY;
        if (waterFailurePhase == 0)
        {
            x = WaterCreatureX(waterMarioX, waterMarioY);
            baseY = waterMarioY - 5;
        }
        else
        {
            x = waterCreatureX;
            if (waterFailurePhase == 1)
            {
                x = waterCreaturePreviousPlotX + 3;
                if (x >= waterMarioX - 35)
                {
                    waterFailurePhase = 2;
                }
            }
        }

        var horizontalOffset = kindForFrame == 1 && frame / 2 != 3 ? -2 : 0;
        var verticalOffset = kindForFrame == 2 ? WaterOctopusYOffsets[frame] : 0;
        waterCreatureDisplayKind = kindForFrame;
        waterCreatureDisplayFrame = frame;
        waterCreatureDisplayX = x + horizontalOffset;
        waterCreatureDisplayY = baseY + verticalOffset;

        if (x < waterCreaturePreviousPlotX - 40)
        {
            waterCreatureKind = kindForFrame == 1 ? 2 : 1;
        }
        waterCreaturePreviousPlotX = waterCreatureDisplayX;
        waterCreatureX = x;
        waterCreatureY = baseY;
        waterCreatureFrame = frame;
    }

    private void SpawnWaterBubble(int x, int y)
    {
        if (waterBubbles.Count < 10)
        {
            waterBubbles.Add(new WaterBubble(x, y));
        }
    }

    private void StepWaterBubbles()
    {
        foreach (var bubble in waterBubbles)
        {
            var state = OriginalWaterBubbleStep(
                bubble.X,
                bubble.Y,
                bubble.HorizontalVelocity,
                bubble.VerticalVelocity);
            bubble.X = state.X;
            bubble.Y = state.Y;
            bubble.VerticalVelocity = state.VerticalVelocity;
            bubble.Large = state.Large;
        }
        waterBubbles.RemoveAll(bubble =>
            bubble.X < 0 || bubble.X > 317 || bubble.Y < 79 || bubble.Y > 197);
    }

    internal static int OriginalWaterTargetY(
        int currentWordsPerMinute,
        int peakWordsPerMinute) =>
        157 - Math.Max(0, currentWordsPerMinute) * 63 /
            Math.Max(10, peakWordsPerMinute);

    internal static int WaterCreatureX(int marioX, int marioY)
    {
        var x = marioX - 48 - (160 - marioY) / 2;
        return x < -55 ? x + 368 : x;
    }

    internal static (int X, int Y, int VerticalVelocity, bool Large) OriginalWaterBubbleStep(
        int x,
        int y,
        int horizontalVelocity,
        int verticalVelocity)
    {
        x += horizontalVelocity;
        y += verticalVelocity / 8 - 1;
        verticalVelocity--;
        return (x, y, verticalVelocity, verticalVelocity < -10);
    }

    internal void AdvanceWaterFramesForTest(int count)
    {
        if (screen != OriginalScreen.Water)
        {
            throw new InvalidOperationException("Water state is not active.");
        }
        for (var frame = 0; frame < Math.Max(0, count); frame++)
        {
            StepWaterState(waterLastStateUpdate += OriginalFrameMilliseconds);
        }
        Render();
    }

    internal void AddWaterBubbleForTest(int x, int y)
    {
        if (screen != OriginalScreen.Water)
        {
            throw new InvalidOperationException("Water state is not active.");
        }
        SpawnWaterBubble(x, y);
        Render();
    }

    internal void AdvanceWaterBubblesForTest(int count)
    {
        if (screen != OriginalScreen.Water)
        {
            throw new InvalidOperationException("Water state is not active.");
        }
        for (var frame = 0; frame < Math.Max(0, count); frame++)
        {
            StepWaterBubbles();
        }
        Render();
    }

    private void InitializeCastleState()
    {
        castlePromptPosition = 0;
        castleMarioX = 0;
        castleMarioY = 92;
        castleTargetX = 67;
        castleVelocity = 0;
        castleRunCounter = 0;
        castleSequencePhase = 0;
        castleSequenceFrame = 0;
        castleFailurePhase = 0;
        Array.Clear(castleRockProgress);
        InitializeCastleBatch(castlePromptPosition);
        castleLastKeyAt = sceneStarted;
        castleLastStateUpdate = sceneStarted;
        InitializeBonusState();
    }

    private void InitializeCastleBatch(int promptOffset)
    {
        while (char.IsWhiteSpace(PromptCharacterAt(prompt, promptOffset)))
        {
            promptOffset++;
        }
        castlePromptPosition = promptOffset;
        castleBatchStart = promptOffset;
        castleBatchEndExclusive = OriginalCastleBatchEnd(prompt, promptOffset);
        castleBatchCorrect = 0;
        castleTargetX = OriginalCastleTargetX(
            castleBatchCorrect,
            castleBatchEndExclusive - castleBatchStart);
    }

    private void UpdateCastleState(long now)
    {
        if (castleLastStateUpdate <= 0)
        {
            castleLastStateUpdate = now;
            return;
        }

        var steps = 0;
        while (castleLastStateUpdate + OriginalFrameMilliseconds <= now && steps < 200)
        {
            castleLastStateUpdate += OriginalFrameMilliseconds;
            StepCastleState(castleLastStateUpdate);
            steps++;
        }
        if (steps == 200)
        {
            castleLastStateUpdate = now;
        }
    }

    private void StepCastleState(long stateTime)
    {
        StepCastleBonus(stateTime);
        if (castleFailurePhase != 0)
        {
            castleVelocity = 0;
            castleMarioY += 3;
            if (castleMarioY >= 200)
            {
                castleMarioY = 200;
                castleFailurePhase = 2;
            }
            return;
        }

        if (castleSequencePhase is 1 or 3)
        {
            castleVelocity = 0;
            castleSequenceFrame++;
            castleLastKeyAt = stateTime;
            StepCastleInterlude();
            return;
        }

        if (OriginalElapsedTicks(stateTime - castleLastKeyAt) > 20)
        {
            castleFailurePhase = 1;
            castleVelocity = 0;
            return;
        }

        var desiredVelocity = OriginalCastleDesiredVelocity(castleMarioX, castleTargetX);
        castleVelocity = OriginalCastleNextVelocity(castleVelocity, desiredVelocity);
        castleMarioX += castleVelocity;
        if (castleVelocity != 0)
        {
            castleRunCounter++;
        }

        if (castleSequencePhase == 0)
        {
            for (var rock = 0; rock < castleRockProgress.Length; rock++)
            {
                if (castleRockProgress[rock] == 0 && CastleRockXPositions[rock] <= castleMarioX)
                {
                    castleRockProgress[rock] = 1;
                    PlaySoundEffect(6);
                }
                if (castleRockProgress[rock] is >= 1 and <= 32)
                {
                    castleRockProgress[rock]++;
                    if (castleRockProgress[rock] > 32)
                    {
                        castleRockProgress[rock] = 100;
                    }
                }
            }
        }
    }

    private void BeginCastleConstruction()
    {
        castleSequencePhase = castleSequencePhase == 0 ? 1 : 3;
        castleSequenceFrame = 0;
        castleVelocity = 0;
        castleConstructionFrames = 20;
        for (var piece = 0; piece < castleBuildDwellFrames.Length; piece++)
        {
            // The DOS routine displays each piece for four calls to its
            // three-tick update, erases it, then waits rand(6) + 4 calls.
            castleBuildDwellFrames[piece] = bonusRandom.Next(6) + 4;
            castleConstructionFrames += castleBuildDwellFrames[piece];
        }
    }

    private int CastleConstructionPieceAtFrame(int frame)
    {
        if (frame < 62)
        {
            return -1;
        }

        var relativeFrame = frame - 62;
        var pieceStart = 0;
        for (var piece = 0; piece < castleBuildDwellFrames.Length; piece++)
        {
            if (relativeFrame >= pieceStart && relativeFrame < pieceStart + 4)
            {
                return piece;
            }
            pieceStart += 4 + castleBuildDwellFrames[piece];
        }
        return -1;
    }

    private bool IsCastleConstructionPieceStart(int frame)
    {
        if (frame < 62)
        {
            return false;
        }

        var relativeFrame = frame - 62;
        var pieceStart = 0;
        for (var piece = 0; piece < castleBuildDwellFrames.Length; piece++)
        {
            if (relativeFrame == pieceStart)
            {
                return true;
            }
            pieceStart += 4 + castleBuildDwellFrames[piece];
        }
        return false;
    }

    private void StepCastleInterlude()
    {
        if (castleSequencePhase == 1)
        {
            StepFirstCastleInterlude();
        }
        else
        {
            StepSecondCastleInterlude();
        }
    }

    private void StepFirstCastleInterlude()
    {
        var frame = castleSequenceFrame;
        var constructionEnd = 61 + castleConstructionFrames;
        if (frame <= 40)
        {
            castleMarioX -= frame % 2 == 0 ? 6 : 7;
        }
        else if (frame <= 61)
        {
            castleMarioX = Math.Min(48, castleMarioX + 4);
        }
        else if (frame <= constructionEnd)
        {
            castleMarioX = 48;
            if (IsCastleConstructionPieceStart(frame))
            {
                PlaySoundEffect(7);
            }
        }
        else if (frame <= constructionEnd + 15)
        {
            castleMarioX = 222 + (frame - constructionEnd) * 4;
            castleMarioY = 36;
        }
        else if (frame <= constructionEnd + 55)
        {
            castleMarioX -= frame % 2 == 0 ? 6 : 7;
            castleMarioY = 36;
        }
        else if (frame <= constructionEnd + 60)
        {
            castleMarioX = Math.Min(42, castleMarioX + 4);
            castleMarioY = 36;
        }
        else
        {
            var sinkFrame = Math.Clamp(frame - constructionEnd - 60, 1, 8);
            if (frame == constructionEnd + 61)
            {
                PlaySoundEffect(2);
            }
            castleMarioX += 3;
            castleMarioY += sinkFrame;
        }

        if (frame >= constructionEnd + 68)
        {
            PlaySoundEffect(9);
            FinishCastleInterlude(2, 2);
        }
    }

    private void StepSecondCastleInterlude()
    {
        var frame = castleSequenceFrame;
        var constructionEnd = 61 + castleConstructionFrames;
        if (frame <= 7)
        {
            if (frame == 1)
            {
                PlaySoundEffect(2);
            }
            var verticalVelocity = -9 + frame;
            castleMarioX += 4;
            castleMarioY += verticalVelocity;
        }
        else if (frame <= 13)
        {
            castleMarioX = Math.Min(279, castleMarioX + 4);
            castleMarioY = 36;
        }
        else if (frame <= 53)
        {
            castleMarioX -= frame % 2 == 0 ? 6 : 7;
            castleMarioY = 36;
        }
        else if (frame <= 61)
        {
            castleMarioX = Math.Min(48, castleMarioX + 4);
            castleMarioY = 36;
        }
        else if (frame <= constructionEnd)
        {
            castleMarioX = 48;
            if (IsCastleConstructionPieceStart(frame))
            {
                PlaySoundEffect(7);
            }
        }
        else if (frame <= constructionEnd + 2)
        {
            castleMarioX = Math.Min(281, 273 + (frame - constructionEnd) * 4);
            castleMarioY = 92;
        }
        else
        {
            castleMarioX -= frame % 2 == 0 ? 6 : 7;
            castleMarioY = 92;
        }

        if (frame >= constructionEnd + 42)
        {
            Array.Clear(castleRockProgress);
            FinishCastleInterlude(0, 0);
        }
    }

    private void FinishCastleInterlude(int nextPhase, int sceneIndex)
    {
        castleSequencePhase = nextPhase;
        castleSequenceFrame = 0;
        castleVelocity = 0;
        if (sceneIndex == 2)
        {
            castleMarioX = Math.Max(40, castleMarioX);
            castleMarioY = Math.Clamp(castleMarioY, 70, 80);
        }
        else
        {
            castleMarioX = Math.Max(0, castleMarioX);
            castleMarioY = 92;
        }
        InitializeCastleBatch(castlePromptPosition);
        castleLastKeyAt = castleLastStateUpdate;
    }

    internal static int OriginalCastleBatchEnd(string text, int start)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Castle lesson text cannot be empty.", nameof(text));
        }
        var terminators = 0;
        for (var offset = start; offset < start + text.Length * 2; offset++)
        {
            if (PromptCharacterAt(text, offset) is '.' or '!' or '?')
            {
                terminators++;
                if (terminators == 3)
                {
                    return offset + 1;
                }
            }
        }
        return start + text.Length;
    }

    internal static int OriginalCastleTargetX(int correctCharacters, int totalCharacters) =>
        67 + Math.Clamp(correctCharacters, 0, Math.Max(1, totalCharacters)) * 160 /
            Math.Max(1, totalCharacters);

    internal static int OriginalCastleDesiredVelocity(int marioX, int targetX)
    {
        if (marioX == targetX)
        {
            return 0;
        }
        return Math.Clamp((targetX - marioX) / 4 + 1, -5, 5);
    }

    internal static int OriginalCastleNextVelocity(int currentVelocity, int desiredVelocity) =>
        currentVelocity < desiredVelocity
            ? currentVelocity + 1
            : currentVelocity > desiredVelocity ? currentVelocity - 1 : currentVelocity;

    internal static Rectangle? OriginalCastleMarioClip(
        int sequencePhase,
        int sequenceFrame,
        int constructionFrames)
    {
        var constructionEnd = 61 + constructionFrames;
        return sequencePhase switch
        {
            // The original uses inclusive JaySetClipRect bounds. These five
            // windows hide the character behind the left/right pipe mouths and
            // below the upper pipe lip while preserving partially visible pixels.
            1 when sequenceFrame is >= 41 and <= 61 => new Rectangle(0, 0, 47, 200),
            1 when sequenceFrame > constructionEnd &&
                   sequenceFrame <= constructionEnd + 15 => new Rectangle(272, 0, 48, 200),
            1 when sequenceFrame > constructionEnd + 60 &&
                   sequenceFrame <= constructionEnd + 68 => new Rectangle(0, 0, 320, 92),
            3 when sequenceFrame is >= 54 and <= 61 => new Rectangle(0, 0, 45, 200),
            3 when sequenceFrame > constructionEnd &&
                   sequenceFrame <= constructionEnd + 2 => new Rectangle(269, 0, 51, 200),
            _ => null,
        };
    }

    internal void AdvanceCastleFramesForTest(int count)
    {
        if (screen != OriginalScreen.Castle)
        {
            throw new InvalidOperationException("Castle state is not active.");
        }
        for (var frame = 0; frame < Math.Max(0, count); frame++)
        {
            StepCastleState(castleLastStateUpdate += OriginalFrameMilliseconds);
        }
        Render();
    }

    private void InitializePracticeState()
    {
        practicePromptPosition = SkipPracticeWhitespace(prompt, 0);
        SetPracticeLine(practicePromptPosition);
        practiceLastCorrectAt = sceneStarted;
    }

    private void BeginNextPracticeLine()
    {
        practicePromptPosition = SkipPracticeWhitespace(prompt, practiceLineEndExclusive);
        SetPracticeLine(practicePromptPosition);
    }

    private void SetPracticeLine(int start)
    {
        var line = PracticeLine(prompt, start, PracticeTextColumns);
        practiceLine = line.Text;
        practiceLineEndExclusive = line.NextOffset;
        practiceLineTyped = 0;
        practicePromptPosition = start;
    }

    private static int SkipPracticeWhitespace(string text, int offset)
    {
        if (string.IsNullOrEmpty(text))
        {
            return offset;
        }
        var checkedCharacters = 0;
        while (checkedCharacters < text.Length &&
               char.IsWhiteSpace(PromptCharacterAt(text, offset)))
        {
            offset++;
            checkedCharacters++;
        }
        return offset;
    }

    private int PracticeTextColumns => Math.Max(
        32,
        (DisplayFrameWidth(progress.DisplayMode) - 60) / 8);

    internal static (string Text, int NextOffset) OriginalPracticeLine(string text, int start) =>
        PracticeLine(text, start, 32);

    internal static (string Text, int NextOffset) PracticeLine(
        string text,
        int start,
        int maximumCharacters)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Practice lesson text cannot be empty.", nameof(text));
        }

        maximumCharacters = Math.Max(1, maximumCharacters);
        var buffer = new StringBuilder(maximumCharacters);
        var offset = start;
        while (buffer.Length < maximumCharacters)
        {
            var character = PromptCharacterAt(text, offset);
            if (character is '\r' or '\n')
            {
                break;
            }
            buffer.Append(character);
            offset++;
        }

        if (buffer.Length == maximumCharacters &&
            buffer[^1] != ' ' &&
            PromptCharacterAt(text, offset) != ' ')
        {
            var lastSpace = buffer.ToString().LastIndexOf(' ');
            if (lastSpace > 0)
            {
                buffer.Length = lastSpace;
                offset = start + lastSpace + 1;
            }
        }

        while (buffer.Length > 0 && buffer[^1] == ' ')
        {
            buffer.Length--;
        }
        if (buffer.Length == 0)
        {
            buffer.Append(PromptCharacterAt(text, start));
            offset = start + 1;
        }
        return (buffer.ToString(), offset);
    }

    internal static int OriginalPracticeFace(
        int ticksSinceLastCorrect,
        int currentWordsPerMinute,
        int goalWordsPerMinute,
        bool hasStudent = true) =>
        ticksSinceLastCorrect > 15
            ? 1
            : hasStudent && currentWordsPerMinute > goalWordsPerMinute ? 3 : 2;

    private void RenderOutside()
    {
        var scenePalette = outsidePalettes[(int)progress.Character];
        var characterPrefix = CharacterPrefix(progress.Character);
        raster.CopyFrom(outside, repeatHorizontally: raster.Width > ClassicFrameWidth);
        raster.DrawIndexedHorizontalWrap(
            outside.Pixels,
            CrxImage.Width,
            sourceY: 140,
            height: 16,
            scenePalette,
            outsideGroundOffset);
        DrawBottomDashboard(scenePalette);
        // The DOS loop plots the bonus before Mario, targets, debris, and the
        // animated target cursor, which determines overlap when they cross.
        DrawBonusTarget(OriginalScreen.Outside, scenePalette);

        if (outsideDisplayedJumpFrame >= 0)
        {
            var jumpFrame = 8 + Math.Clamp(outsideDisplayedJumpFrame, 0, 11);
            raster.DrawSprite(
                walking[$"{characterPrefix}jmp{jumpFrame:0000}_1"],
                scenePalette,
                outsideMarioX,
                outsideMarioY);
        }
        else
        {
            var runFrame = 1 + outsideRunCounter / 6 % 12;
            raster.DrawSprite(
                walking[$"{characterPrefix}run{runFrame:0000}_1"],
                scenePalette,
                outsideMarioX,
                outsideMarioY);
        }

        DrawOutsideTargets();
    }

    private void RenderWater()
    {
        var scenePalette = waterPalettes[(int)progress.Character];
        var characterPrefix = CharacterPrefix(progress.Character);
        raster.CopyFrom(
            water,
            repeatHorizontally: raster.Width > ClassicFrameWidth,
            extensionSourceX: Math.Max(0, ClassicFrameWidth * 2 - raster.Width));
        DrawTopDashboard(scenePalette);
        DrawWaterPrompt(scenePalette);

        if (waterFailurePhase == 2)
        {
            raster.DrawSprite(
                waterObjects[$"sunk_{characterPrefix}swm"],
                scenePalette,
                waterMarioX,
                waterMarioY);
        }
        else
        {
            raster.DrawSprite(
                waterObjects[$"{characterPrefix}swm{waterSwimFrame + 1:0000}_1"],
                scenePalette,
                waterMarioX,
                waterMarioY);
        }

        if (waterCreatureDisplayKind == 1)
        {
            raster.DrawSprite(
                waterObjects[$"bite{waterCreatureDisplayFrame / 2 % 5 + 1:0000}_1"],
                scenePalette,
                waterCreatureDisplayX,
                waterCreatureDisplayY);
        }
        else
        {
            raster.DrawSprite(
                waterObjects[$"octo{waterCreatureDisplayFrame / 2 % 10 + 1:0000}_1"],
                scenePalette,
                waterCreatureDisplayX,
                waterCreatureDisplayY);
        }
        foreach (var bubble in waterBubbles)
        {
            raster.DrawSprite(
                bubble.Large ? WaterLargeBubbleSprite : WaterSmallBubbleSprite,
                scenePalette,
                bubble.X,
                bubble.Y);
        }
        DrawBonusTarget(OriginalScreen.Water, scenePalette);
    }

    private void DrawWaterPrompt(VgaPalette scenePalette)
    {
        const int currentColumn = 10;
        var columns = Math.Max(40, raster.Width / 8);
        Span<char> buffer = stackalloc char[columns];
        for (var column = 0; column < buffer.Length; column++)
        {
            var promptOffset = position + column - currentColumn;
            buffer[column] = promptOffset < 0 ? ' ' : PromptCharacterAt(prompt, promptOffset);
        }

        font9.Draw(
            raster,
            scenePalette,
            new string(buffer[..currentColumn]),
            0,
            27,
            currentColumn,
            foregroundIndex: 12,
            secondaryIndex: 86);
        font9.Draw(
            raster,
            scenePalette,
            buffer[currentColumn].ToString(),
            currentColumn * 8,
            27,
            foregroundIndex: 1,
            secondaryIndex: 15);
        font9.Draw(
            raster,
            scenePalette,
            new string(buffer[(currentColumn + 1)..]),
            (currentColumn + 1) * 8,
            27,
            buffer.Length - currentColumn - 1,
            foregroundIndex: 10,
            secondaryIndex: 86);
    }

    private byte[] CurrentCastleBackdropPixels()
    {
        if (castleSequencePhase == 1)
        {
            var constructionEnd = 61 + castleConstructionFrames;
            if (castleSequenceFrame <= 40)
            {
                return CastleBackgroundDecoder.ComposeScrollFrame(
                    castleBackgroundScenes[0],
                    castleBackgroundScenes[1],
                    castleSequenceFrame);
            }
            if (castleSequenceFrame <= constructionEnd + 15)
            {
                return castleBackgroundScenes[1];
            }
            if (castleSequenceFrame <= constructionEnd + 55)
            {
                return CastleBackgroundDecoder.ComposeScrollFrame(
                    castleBackgroundScenes[1],
                    castleBackgroundScenes[2],
                    castleSequenceFrame - constructionEnd - 15);
            }
            return castleBackgroundScenes[2];
        }

        if (castleSequencePhase == 3)
        {
            var constructionEnd = 61 + castleConstructionFrames;
            if (castleSequenceFrame <= 13)
            {
                return castleBackgroundScenes[2];
            }
            if (castleSequenceFrame <= 53)
            {
                return CastleBackgroundDecoder.ComposeScrollFrame(
                    castleBackgroundScenes[2],
                    castleBackgroundScenes[3],
                    castleSequenceFrame - 13);
            }
            if (castleSequenceFrame <= constructionEnd + 2)
            {
                return castleBackgroundScenes[3];
            }
            return CastleBackgroundDecoder.ComposeScrollFrame(
                castleBackgroundScenes[3],
                castleBackgroundScenes[0],
                castleSequenceFrame - constructionEnd - 2);
        }

        return castleSequencePhase == 2
            ? castleBackgroundScenes[2]
            : castleBackgroundScenes[0];
    }

    private void RenderCastle()
    {
        var scenePalette = castleScenePalettes[(int)progress.Character];
        var characterPrefix = CharacterPrefix(progress.Character);
        raster.DrawIndexedImageHorizontallyTiled(
            CurrentCastleBackdropPixels(),
            ClassicFrameWidth,
            FrameHeight,
            scenePalette);
        DrawBottomDashboard(scenePalette);
        DrawCastlePrompt(scenePalette);

        if (castleSequencePhase == 0)
        {
            for (var rock = 0; rock < castleRockProgress.Length; rock++)
            {
                var progressFrame = castleRockProgress[rock];
                var tableIndex = progressFrame == 0
                    ? 0
                    : progressFrame == 100
                        ? 32
                        : Math.Clamp(progressFrame - 1, 1, 32);
                var shape = tableIndex == 0 ? 1 : CastleRockShapeFrames[tableIndex];
                var y = 40 + (tableIndex == 0 ? 0 : CastleRockYOffsets[tableIndex]);
                raster.DrawSprite(
                    castleObjects[$"rockface_{shape}"],
                    scenePalette,
                    CastleRockXPositions[rock],
                    y);
            }
        }

        var buildFrame = castleSequencePhase is 1 or 3
            ? CastleConstructionPieceAtFrame(castleSequenceFrame)
            : -1;
        if (buildFrame >= 0)
        {
            var secondBuild = castleSequencePhase == 3;
            var xPositions = secondBuild
                ? CastleSecondBuildXPositions
                : CastleFirstBuildXPositions;
            var yPositions = secondBuild
                ? CastleSecondBuildYPositions
                : CastleFirstBuildYPositions;
            var pipePrefix = secondBuild ? "pip20002_bump" : "pip10002_bump";
            raster.DrawSprite(
                castleObjects[$"{pipePrefix}{buildFrame + 1}"],
                scenePalette,
                xPositions[buildFrame],
                yPositions[buildFrame]);
        }

        DrawBonusTarget(OriginalScreen.Castle, scenePalette);

        var sinking = castleSequencePhase == 2 ||
                      (castleSequencePhase == 1 && castleSequenceFrame >= 162) ||
                      (castleSequencePhase == 3 && castleSequenceFrame <= 7);
        var interludeMoving = castleSequencePhase is 1 or 3 &&
                              !(castleSequenceFrame is >= 62 and <= 101);
        var runFrame = 1 + (castleRunCounter + castleSequenceFrame) % 12;
        var marioClip = OriginalCastleMarioClip(
            castleSequencePhase,
            castleSequenceFrame,
            castleConstructionFrames);
        if (sinking)
        {
            var sinkFrame = 1 + castleRunCounter / 3 % 5;
            raster.DrawSprite(
                walking[$"{characterPrefix}sin{sinkFrame:0000}_1"],
                scenePalette,
                castleMarioX,
                castleMarioY,
                clipRectangle: marioClip);
        }
        else if (castleVelocity == 0 && !interludeMoving &&
            idle.TryGet($"{characterPrefix}std0001_1", out var standingFrame))
        {
            raster.DrawSprite(
                standingFrame,
                scenePalette,
                castleMarioX,
                castleMarioY,
                clipRectangle: marioClip);
        }
        else
        {
            raster.DrawSprite(
                walking[$"{characterPrefix}run{runFrame:0000}_1"],
                scenePalette,
                castleMarioX,
                castleMarioY,
                clipRectangle: marioClip);
        }
    }

    private void DrawCastlePrompt(VgaPalette scenePalette)
    {
        var columns = Math.Max(40, raster.Width / 8);
        Span<char> buffer = stackalloc char[columns];
        for (var column = 0; column < buffer.Length; column++)
        {
            buffer[column] = PromptCharacterAt(prompt, castlePromptPosition + column);
        }
        font9.Draw(
            raster,
            scenePalette,
            buffer[0].ToString(),
            0,
            0,
            1,
            foregroundIndex: 1,
            secondaryIndex: 15);
        font9.Draw(
            raster,
            scenePalette,
            new string(buffer[1..]),
            8,
            0,
            buffer.Length - 1,
            foregroundIndex: 10,
            secondaryIndex: 86);
    }

    private void RenderPractice()
    {
        if (CenteredContentOffsetX == 0)
        {
            raster.CopyFrom(practice);
        }
        else
        {
            DrawMarioWallpaper();
            var paper = practice.Palette.FindNearest(255, 255, 255);
            var border = practice.Palette.FindNearest(0, 0, 160);
            // Replace the complete original 272-pixel sheet, including its old
            // left/right borders, so those borders cannot become seams inside
            // the widened page.
            raster.FillRectangle(
                22,
                91,
                raster.Width - 44,
                109,
                practice.Palette.Argb[border]);
            raster.FillRectangle(
                24,
                92,
                raster.Width - 48,
                108,
                practice.Palette.Argb[paper]);
        }
        DrawPracticeDashboard(practice.Palette);
        var elapsedTicks = OriginalElapsedTicks(clock.ElapsedMilliseconds - typingStarted);
        var wordsPerMinute = CalculateOriginalWordsPerMinuteFromTicks(position, errors, elapsedTicks);
        var faceNumber = OriginalPracticeFace(
            OriginalElapsedTicks(clock.ElapsedMilliseconds - practiceLastCorrectAt),
            wordsPerMinute,
            progress.GoalWordsPerMinute);
        raster.DrawSprite(
            practiceObjects[$"prac000{faceNumber}_1"],
            practice.Palette,
            CenteredX(110),
            0);

        var typedLength = Math.Clamp(practiceLineTyped, 0, practiceLine.Length);
        var textColumns = PracticeTextColumns;
        font5.Draw(
            raster,
            practice.Palette,
            practiceLine[..typedLength],
            30,
            96,
            textColumns,
            foregroundIndex: 1);
        font5.Draw(
            raster,
            practice.Palette,
            practiceLineTyped >= practiceLine.Length ? string.Empty : "_",
            30 + typedLength * 8,
            96,
            1,
            foregroundIndex: 1);

        var previewOffset = practicePromptPosition - practiceLineTyped;
        for (var row = 0; row < 9; row++)
        {
            previewOffset = SkipPracticeWhitespace(prompt, previewOffset);
            var preview = PracticeLine(prompt, previewOffset, textColumns);
            font5.Draw(
                raster,
                practice.Palette,
                preview.Text,
                30,
                106 + row * 10,
                textColumns,
                foregroundIndex: 0);
            previewOffset = preview.NextOffset;
        }

        if (practiceLineTyped >= practiceLine.Length)
        {
            var paper = practice.Palette.FindNearest(255, 255, 255);
            var checkpointX = (raster.Width - 112) / 2;
            raster.FillRectangle(checkpointX, 143, 112, 15, practice.Palette.Argb[paper]);
            font5.Draw(
                raster,
                practice.Palette,
                "SPACE / ENTER",
                checkpointX + 5,
                147,
                13,
                foregroundIndex: 1);
        }
    }

    private void DrawBonusTarget(OriginalScreen world, VgaPalette scenePalette)
    {
        if (bonusFlag != 2 || screen != world)
        {
            return;
        }

        if (world == OriginalScreen.Water)
        {
            raster.DrawSprite(waterObjects["bonbub_1"], scenePalette, bonusX, bonusY);
            font9.Draw(
                raster,
                scenePalette,
                bonusCharacter.ToString(),
                bonusX + 8,
                bonusY + 7,
                1,
                foregroundIndex: 15,
                secondaryIndex: 9);
            return;
        }
        if (world == OriginalScreen.Castle)
        {
            raster.DrawSprite(castleObjects["brick1_cblock"], scenePalette, bonusX, bonusY);
            font9.Draw(
                raster,
                scenePalette,
                bonusCharacter.ToString(),
                bonusX + 8,
                bonusY + 7,
                1,
                foregroundIndex: 10,
                secondaryIndex: 1);
            return;
        }

        // The DOS outdoor overlay copies brick1_1 into the 40x40 bonus buffer,
        // then plots the FONT7 glyph at (6,3) through { 0, 15, 31 }.
        raster.DrawSprite(outsideObjects["brick1_1"], scenePalette, bonusX, bonusY);
        font7.Draw(
            raster,
            scenePalette,
            bonusCharacter.ToString(),
            bonusX + 6,
            bonusY + 3,
            1,
            foregroundIndex: 15,
            secondaryIndex: 31);
    }

    private void RenderReportCard()
    {
        DrawCenteredUiFrame(reportCard);
        var black = reportCard.Palette.FindNearest(0, 0, 0);
        var result = progress.Results.LastOrDefault();
        font5.Draw(
            raster, reportCard.Palette, $"Name: {progress.StudentName}",
            CenteredX(60), 66, foregroundIndex: black);
        font5.Draw(
            raster, reportCard.Palette, $"Teacher: {CharacterDisplayName(progress.Character)}",
            CenteredX(60), 80, foregroundIndex: black);
        font5.Draw(
            raster,
            reportCard.Palette,
            "Level:",
            CenteredX(60),
            96,
            foregroundIndex: black);
        font5.Draw(
            raster, reportCard.Palette, WorldDisplayName(progress.CurrentWorld),
            CenteredX(178), 96, foregroundIndex: black);
        font5.Draw(
            raster,
            reportCard.Palette,
            "Goal WPM:",
            CenteredX(60),
            108,
            foregroundIndex: black);
        font5.Draw(
            raster, reportCard.Palette, $"{progress.GoalWordsPerMinute,3}",
            CenteredX(178), 108, foregroundIndex: black);
        if (result is { WordsPerMinute: not 0 })
        {
            font5.Draw(
                raster,
                reportCard.Palette,
                "Last WPM:",
                CenteredX(60),
                120,
                foregroundIndex: black);
            font5.Draw(
                raster, reportCard.Palette, $"{result.WordsPerMinute,3:0}",
                CenteredX(178), 120, foregroundIndex: black);
            if (Math.Round(result.Accuracy * 100) != 0)
            {
                font5.Draw(
                    raster,
                    reportCard.Palette,
                    "Accuracy:",
                    CenteredX(60),
                    132,
                    foregroundIndex: black);
                font5.Draw(
                    raster, reportCard.Palette, $"{result.Accuracy * 100,3:0}%",
                    CenteredX(178), 132, foregroundIndex: black);
            }
        }
        font5.Draw(
            raster,
            reportCard.Palette,
            "Problem Keys:",
            CenteredX(60),
            144,
            foregroundIndex: black);
        font3.Draw(
            raster,
            reportCard.Palette,
            result is null ? "None!" : ProblemKeySummary(result),
            CenteredX(178),
            143,
            15,
            foregroundIndex: reportCard.Palette.FindNearest(255, 255, 0));
    }

    private void DrawOutsideTargets()
    {
        var scenePalette = outsidePalettes[(int)progress.Character];
        foreach (var target in outsideTargets)
        {
            var x = target.X + target.SpriteXOffset;
            if (x < -30 || x >= raster.Width || prompt.Length == 0)
            {
                continue;
            }
            if (target.Kind == 1)
            {
                raster.DrawSprite(outsideObjects["brick1_1"], scenePalette, x, target.Y);
                if (target.HasCharacter)
                {
                    DrawTargetCharacter(font7, scenePalette, x + 5, target.Y + 3, target.PromptIndex);
                }
            }
            else
            {
                var turtleFrame = target.HitFramesRemaining > 0 ? "turt1_2" : "turt1_1";
                raster.DrawSprite(outsideObjects[turtleFrame], scenePalette, x, target.Y);
                if (target.HasCharacter)
                {
                    DrawTargetCharacter(font5, scenePalette, x + 11, target.Y + 4, target.PromptIndex);
                }
            }
        }

        foreach (var particle in outsideParticles)
        {
            raster.DrawSprite(outsideObjects["expls1_1"], scenePalette, particle.X, particle.Y);
        }

        if (outsideSelectedTarget is { HasCharacter: true } selected)
        {
            var cursorFrame = OriginalOutsideCursorFrame(outsideAnimationCounter) + 1;
            raster.DrawSprite(
                outsideObjects[$"cursor_{cursorFrame}"],
                scenePalette,
                selected.X + selected.SpriteXOffset + 10,
                selected.Y - 12);
        }
    }

    private void DrawBottomDashboard(VgaPalette scenePalette)
    {
        DrawHands(90, 158, 90);
        DrawStatistics(scenePalette, bottom: true, drawTarget: true);
    }

    private void DrawTopDashboard(VgaPalette scenePalette)
    {
        DrawHands(100, 3, 70);
        DrawStatistics(scenePalette, bottom: false, drawTarget: false);
    }

    private void DrawPracticeDashboard(VgaPalette scenePalette)
    {
        DrawHands(CenteredX(30), 45, 230);
        DrawStatistics(
            scenePalette,
            bottom: false,
            drawTarget: false,
            xOffset: CenteredContentOffsetX);
    }

    private void DrawHands(int x, int y, int spacing)
    {
        raster.DrawSprite(fingers["finger_11"], fingerPalette, x, y);
        raster.DrawSprite(fingers["finger_12"], fingerPalette, x + spacing, y);

        if (CurrentExpectedCharacter() is not { } expected ||
            OriginalHandForCharacter(expected) is not { } element)
        {
            return;
        }

        raster.DrawSprite(
            fingers[$"finger_{element.SpriteNumber}"],
            fingerPalette,
            x + (element.RightHand ? spacing : 0) + element.XOffset,
            y + element.YOffset);
    }

    private void DrawStatistics(
        VgaPalette scenePalette,
        bool bottom,
        bool drawTarget,
        int xOffset = 0)
    {
        var elapsedTicks = typingStarted < 0
            ? 0
            : OriginalElapsedTicks(clock.ElapsedMilliseconds - typingStarted);
        var elapsed = OriginalElapsedSeconds(elapsedTicks);
        var wordsPerMinute = CalculateOriginalWordsPerMinuteFromTicks(position, errors, elapsedTicks);
        var green = scenePalette.FindNearest(0, 255, 0);
        var yellow = scenePalette.FindNearest(255, 255, 0);
        var red = scenePalette.FindNearest(255, 64, 64);
        var hud = BuildOriginalHudState(screen, position, errors, wordsPerMinute, elapsed);
        font5.Draw(
            raster, scenePalette, hud.TimeText, xOffset, hud.FirstRow,
            foregroundIndex: green);
        font5.Draw(
            raster, scenePalette, hud.TypedText, xOffset, hud.SecondRow,
            foregroundIndex: yellow);
        font5.Draw(
            raster, scenePalette, hud.WordsPerMinuteText,
            hud.WordsPerMinuteX + xOffset, hud.FirstRow,
            foregroundIndex: green);
        font5.Draw(
            raster, scenePalette, hud.ErrorsText, hud.ErrorsX + xOffset, hud.SecondRow,
            foregroundIndex: red);
        if (drawTarget)
        {
            var targetOffset = screen == OriginalScreen.Outside && outsideSelectedTarget is not null
                ? outsideSelectedTarget.PromptIndex
                : screen == OriginalScreen.Castle ? castlePromptPosition : position;
            DrawTargetCharacter(font7, scenePalette, 154 + xOffset, hud.SecondRow, targetOffset);
        }
    }

    internal static OriginalHudState BuildOriginalHudState(
        OriginalScreen world,
        int correctKeystrokes,
        int errors,
        int wordsPerMinute,
        long elapsedSeconds)
    {
        var bottom = world is OriginalScreen.Outside or OriginalScreen.Castle;
        var waterLayout = world == OriginalScreen.Water;
        elapsedSeconds = Math.Max(0, elapsedSeconds);
        return new OriginalHudState(
            $"{elapsedSeconds / 3600,3}:{elapsedSeconds / 60 % 60:00}:{elapsedSeconds % 60:00}",
            $"Typed:{Math.Max(0, correctKeystrokes) + Math.Max(0, errors),3}" +
                (waterLayout ? " " : ""),
            waterLayout
                ? $"   WPM: {Math.Max(0, wordsPerMinute),3}"
                : $"WPM: {Math.Max(0, wordsPerMinute),3}",
            waterLayout
                ? $" Errors:{Math.Max(0, errors),3}"
                : $"Errors:{Math.Max(0, errors),3}",
            waterLayout ? 232 : 256,
            waterLayout ? 232 : 240,
            bottom ? 168 : 8,
            bottom ? 184 : 24);
    }

    private void DrawTargetCharacter(OriginalFont font, VgaPalette palette, int x, int y, int promptOffset)
    {
        if (prompt.Length == 0)
        {
            return;
        }
        var character = PromptCharacterAt(prompt, promptOffset);
        font.Draw(
            raster,
            palette,
            character == ' ' ? "_" : character.ToString(),
            x,
            y,
            foregroundIndex: palette.FindNearest(255, 255, 255),
            secondaryIndex: palette.FindNearest(0, 0, 0));
    }

    private string VisiblePrompt(int maximum)
    {
        if (prompt.Length == 0 || maximum <= 0)
        {
            return "";
        }
        var visible = new StringBuilder(maximum);
        for (var offset = 0; offset < maximum; offset++)
        {
            visible.Append(prompt[(position + offset) % prompt.Length]);
        }
        return visible.ToString();
    }

    private string CreateLessonPrompt(ActivityMode mode)
    {
        activeLessonCategory = SelectedCategory();
        return LessonLibrary.CreatePrompt(activeLessonCategory, mode);
    }

    private int CurrentLessonTimeMinutes()
    {
        var index = screen switch
        {
            OriginalScreen.Outside => 0,
            OriginalScreen.Water => 1,
            OriginalScreen.Castle => 2,
            OriginalScreen.Practice => 3,
            _ => 0,
        };
        return progress.LessonTimesMinutes[index];
    }

    private bool HasAnyCertificate() =>
        CertificateWorlds.Any(progress.HasCertificate);

    private void MoveCertificateSelection(int direction)
    {
        auxiliarySelection = (auxiliarySelection + direction + CertificateFocusPoints.Length) %
                             CertificateFocusPoints.Length;
        CertificateCursorRequested?.Invoke(
            CenteredPoint(CertificateFocusPoints[auxiliarySelection]));
        RenderAndNotify();
    }

    private void PrintSelectedCertificate()
    {
        if ((uint)auxiliarySelection >= CertificateWorlds.Length ||
            !progress.HasCertificate(CertificateWorlds[auxiliarySelection]))
        {
            return;
        }
        CertificatePrintRequested?.Invoke(new CertificatePrintRequest(
            progress.StudentName,
            CertificateLevelNames[auxiliarySelection]));
    }

    private static string CharacterDisplayName(OriginalCharacter character) => character switch
    {
        OriginalCharacter.Luigi => "Luigi",
        OriginalCharacter.Princess => "Princess",
        _ => "Mario",
    };

    private LessonCategory SelectedCategory() => LessonCategoryForSelection(selectedLesson);

    internal static LessonCategory LessonCategoryForSelection(
        int zeroBasedSelection,
        int? allKeyboardChoice = null)
    {
        if (zeroBasedSelection is >= 0 and < 5)
        {
            return (LessonCategory)zeroBasedSelection;
        }
        return (LessonCategory)Math.Clamp(
            allKeyboardChoice ?? Random.Shared.Next(5),
            0,
            4);
    }

    private static string CharacterPrefix(OriginalCharacter character) => character switch
    {
        OriginalCharacter.Luigi => "l",
        OriginalCharacter.Princess => "p",
        _ => "m",
    };

    private static string WorldDisplayName(OriginalScreen world) => world switch
    {
        OriginalScreen.Water => "Underwater (Intermediate)",
        OriginalScreen.Castle => "Castle (Advanced)",
        OriginalScreen.Practice => "Practice",
        _ => "Outside (Beginner)",
    };

    private static string ProblemKeyLabel(char character) => character switch
    {
        ' ' => "Space",
        '\t' => "Tab",
        _ => character.ToString(),
    };

    private char? CurrentExpectedCharacter()
    {
        if (prompt.Length == 0 ||
            screen is not (OriginalScreen.Outside or OriginalScreen.Water or OriginalScreen.Castle or OriginalScreen.Practice))
        {
            return null;
        }
        if (screen == OriginalScreen.Outside)
        {
            return outsideSelectedTarget is { HasCharacter: true } target
                ? PromptCharacterAt(prompt, target.PromptIndex)
                : null;
        }
        if (screen == OriginalScreen.Practice)
        {
            return practiceLineTyped < practiceLine.Length
                ? practiceLine[practiceLineTyped]
                : ' ';
        }
        return PromptCharacterAt(
            prompt,
            screen == OriginalScreen.Castle ? castlePromptPosition : position);
    }

    internal static char PromptCharacterAt(string text, int offset)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Prompt cannot be empty.", nameof(text));
        }
        var normalized = offset % text.Length;
        if (normalized < 0)
        {
            normalized += text.Length;
        }
        return text[normalized];
    }

    internal static double CalculateOriginalWordsPerMinute(
        int correctKeystrokes,
        int errors,
        double seconds)
    {
        var netCharacters = Math.Max(0, correctKeystrokes - errors);
        return netCharacters * 10d / Math.Max(1d, seconds);
    }

    internal static int OriginalElapsedTicks(long elapsedMilliseconds) =>
        elapsedMilliseconds <= 0
            ? 0
            : (int)Math.Min(int.MaxValue, elapsedMilliseconds * 182L / 10_000L);

    internal static int OriginalElapsedSeconds(int elapsedTicks) =>
        Math.Max(0, elapsedTicks) * 10 / 182;

    internal static int CalculateOriginalWordsPerMinuteFromTicks(
        int correctKeystrokes,
        int errors,
        int elapsedTicks)
    {
        var netCharacters = Math.Max(0, correctKeystrokes - errors);
        return elapsedTicks <= 0
            ? 0
            : (int)Math.Min(int.MaxValue, (long)netCharacters * 182 / elapsedTicks);
    }

    internal static string ProblemKeySummary(SessionResult result, int maximum = 5)
    {
        if (result.ProblemKeys is null || result.ProblemKeys.Count == 0)
        {
            return "None!";
        }
        var keys = string.Join(" ", result.ProblemKeys
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maximum))
            .Select(pair => pair.Key.Equals("Space", StringComparison.OrdinalIgnoreCase)
                ? " "
                : pair.Key[..1]));
        return " " + keys;
    }

    private ActivityMode CurrentMode() => screen switch
    {
        OriginalScreen.Outside => ActivityMode.LetterDrill,
        OriginalScreen.Water => ActivityMode.WordChallenge,
        OriginalScreen.Castle => ActivityMode.SentenceChallenge,
        OriginalScreen.Practice => PracticeModeForGoal(progress.GoalWordsPerMinute),
        _ => ActivityMode.LetterDrill,
    };

    private void CompleteLesson()
    {
        if (lessonCompleteAt >= 0)
        {
            return;
        }
        lessonCompleteAt = clock.ElapsedMilliseconds;
        if (screen == OriginalScreen.Outside)
        {
            outsideMarioPhase = outsideMarioPhase == 1 ? 2 : 3;
        }
        var elapsedTicks = Math.Max(
            1,
            OriginalElapsedTicks(lessonCompleteAt - Math.Max(0, typingStarted)));
        var seconds = elapsedTicks * 10d / 182d;
        var attempted = position + errors;
        var accuracyPercent = attempted == 0 ? 100 : position * 100 / attempted;
        var accuracy = accuracyPercent / 100d;
        var completedCategory = activeLessonCategory;
        var completedMode = CurrentMode();
        lastCompletedWorld = screen;
        completedLessonLevel = selectedLesson + 1;
        rewardMovieQualified = false;
        earnedCertificateThisLesson = false;

        var measuredWordsPerMinute = CalculateOriginalWordsPerMinuteFromTicks(
            position,
            errors,
            elapsedTicks);
        var progression = AdvanceLessonProgress(
            screen,
            completedLessonLevel,
            progress.GoalWordsPerMinute,
            measuredWordsPerMinute);
        rewardMovieQualified = progression.RewardMovieQualified;
        if (!demoMode && progression.CertificateEarned)
        {
            earnedCertificateThisLesson = !progress.HasCertificate(screen);
            progress.AwardCertificate(screen);
        }
        if (!demoMode &&
            (progression.LessonLevel != completedLessonLevel ||
             progression.GoalWordsPerMinute != progress.GoalWordsPerMinute))
        {
            progress.SetLessonProgress(progression.LessonLevel, progression.GoalWordsPerMinute);
            selectedLesson = progression.LessonLevel - 1;
        }

        if (!demoMode)
        {
            progress.SetCurrentWorld(screen);
        }

        lastResult = new SessionResult(
            DateTimeOffset.UtcNow,
            progress.StudentName,
            completedCategory,
            completedMode,
            position,
            errors,
            accuracy,
            measuredWordsPerMinute,
            seconds,
            completedLessonLevel,
            screen.ToString(),
            problemKeys.ToDictionary(
                pair => ProblemKeyLabel(pair.Key),
                pair => pair.Value));
        if (!demoMode)
        {
            progress.Add(lastResult);
        }
    }

    private static string WorldName(ActivityMode mode) => mode switch
    {
        ActivityMode.LetterDrill => "Outside",
        ActivityMode.WordChallenge => "Underwater",
        ActivityMode.SentenceChallenge => "Castle/Practice",
        ActivityMode.TimedTest => "Timed Test",
        _ => "Lesson",
    };

    private void RenderAndNotify()
    {
        Render();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    private CrxImage LoadCrx(string name) => CrxImage.Decode(assets.ReadAllBytes(name));
    private VgaPalette LoadPalette(string name) => VgaPalette.Decode(assets.ReadAllBytes(name));
    private OriginalSpriteBank LoadGso(string dataName, string indexName) =>
        OriginalSpriteBank.DecodeGso(assets.ReadAllBytes(dataName), assets.ReadAllText(indexName));
    private OriginalSpriteBank LoadGco(string dataName, string indexName) =>
        OriginalSpriteBank.DecodeGco(assets.ReadAllBytes(dataName), assets.ReadAllText(indexName));

    private static HelpLine[] ParseHelp(string text)
    {
        var result = new List<HelpLine>();
        foreach (var sourceLine in text.Replace("\r", "").Split('\n'))
        {
            var line = sourceLine.Replace("\t", "  ").TrimEnd('\0', '\x1a');
            var heading = line.StartsWith("<12>", StringComparison.Ordinal);
            if (heading)
            {
                line = line[4..];
            }
            else if (line.StartsWith("<1>", StringComparison.Ordinal))
            {
                line = line[3..];
            }
            result.Add(new HelpLine(line, heading));
        }
        while (result.Count > 0 && string.IsNullOrEmpty(result[^1].Text))
        {
            result.RemoveAt(result.Count - 1);
        }
        return result.ToArray();
    }

    public void Dispose()
    {
        audio.Dispose();
        raster.Dispose();
    }
}
