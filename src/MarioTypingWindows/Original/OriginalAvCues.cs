namespace MarioTypingWindows.Original;

/// <summary>
/// Enhanced-CD spoken-movie cue tables recovered from MARCD.EXE's Borland
/// debug symbols and initialized data. Names and order intentionally match
/// the original executable, including its gaps in the Z-number sequence.
/// </summary>
internal static class OriginalAvCues
{
    public static readonly string[] MainMenuRandom =
    [
        "Z8", "Z9", "Z52",
    ];

    public static readonly string[] MainMenuIdle =
    [
        "Z10", "Z12", "Z13", "Z14", "Z16", "Z17", "Z19", "Z20",
        "Z21", "Z23", "Z24", "Z25", "Z26", "Z27", "Z39", "Z40",
        "Z41", "Z44", "Z45", "Z46", "Z47", "Z48", "Z49", "Z54",
    ];

    public static readonly string[] OutsideWorld =
    [
        "Z73", "C55", "C56", "C57", "C58", "C59", "C60",
        "C61", "C62", "C63", "C64", "C65", "C66",
    ];

    public static readonly string[] CastleWorld =
    [
        "Z74", "Z55", "Z57", "Z58", "Z59", "Z60", "Z61",
        "Z62", "Z63", "Z64", "Z65", "Z66", "Z72",
    ];

    public static readonly string[] WaterWorld =
    [
        "Z75", "M55", "M56", "M57", "M58", "M59", "M60", "M61",
        "M62", "M63", "M65", "M66", "Z68", "Z69", "Z70",
    ];

    public static readonly string[] ReportFail =
    [
        "Z76", "Z77", "Z78", "Z79",
    ];

    public static readonly string[] WaterDebrief =
    [
        "Z80", "Z81", "Z82", "Z83", "Z84", "Z85", "Z86", "Z87", "Z88", "Z89",
    ];
}
