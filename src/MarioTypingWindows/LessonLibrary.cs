using System.Reflection;
using System.Text.RegularExpressions;

namespace MarioTypingWindows;

internal enum LessonCategory
{
    HomeRow,
    TopRow,
    BottomRow,
    Numbers,
    Symbols,
}

internal enum ActivityMode
{
    LetterDrill,
    WordChallenge,
    SentenceChallenge,
    TimedTest,
}

internal sealed record LessonCategoryInfo(
    LessonCategory Category,
    string Title,
    string Keys,
    string Prefix,
    Color Accent);

internal static partial class LessonLibrary
{
    public static IReadOnlyList<LessonCategoryInfo> Categories { get; } =
    [
        new(LessonCategory.HomeRow, "Home Row", "A S D F  J K L ;", "H", Color.FromArgb(255, 210, 55)),
        new(LessonCategory.TopRow, "Top Row", "Q W E R T  Y U I O P", "T", Color.FromArgb(80, 205, 255)),
        new(LessonCategory.BottomRow, "Bottom Row", "Z X C V  B N M , . /", "B", Color.FromArgb(255, 120, 185)),
        new(LessonCategory.Numbers, "Numbers", "1 2 3 4 5 6 7 8 9 0", "N", Color.FromArgb(105, 235, 150)),
        new(LessonCategory.Symbols, "Symbols", "! @ # $ % ^ & * ( )", "S", Color.FromArgb(205, 150, 255)),
    ];

    public static LessonCategoryInfo GetInfo(LessonCategory category) =>
        Categories.Single(info => info.Category == category);

    public static string GetModeTitle(ActivityMode mode) => mode switch
    {
        ActivityMode.LetterDrill => "Letter Drill",
        ActivityMode.WordChallenge => "Word Challenge",
        ActivityMode.SentenceChallenge => "Sentence Challenge",
        ActivityMode.TimedTest => "60-Second Test",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public static string CreatePrompt(LessonCategory category, ActivityMode mode, int variant = -1)
    {
        var info = GetInfo(category);
        var sourceMode = mode == ActivityMode.TimedTest ? ActivityMode.WordChallenge : mode;
        var fileNames = sourceMode switch
        {
            ActivityMode.LetterDrill => new[] { info.Prefix + "LETTERS.LEE" },
            ActivityMode.WordChallenge => new[] { info.Prefix + "WORDS.LEE" },
            ActivityMode.SentenceChallenge => SentenceFileNames(info.Prefix),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

        if (fileNames.Length == 0)
        {
            throw new InvalidDataException($"No embedded lesson files were mapped for {info.Title}.");
        }
        var fileIndex = variant >= 0
            ? variant % fileNames.Length
            : Random.Shared.Next(fileNames.Length);
        var segments = LoadSegments(fileNames[fileIndex]);

        if (segments.Count == 0)
        {
            throw new InvalidDataException($"No embedded lesson material was found for {info.Title}.");
        }

        var tokenIndex = variant >= 0 ? variant % segments.Count : Random.Shared.Next(segments.Count);
        var rotated = segments.Skip(tokenIndex).Concat(segments.Take(tokenIndex)).ToArray();
        return string.Join(" ", mode == ActivityMode.TimedTest
            ? rotated.Concat(rotated).Concat(rotated)
            : rotated);
    }

    public static IReadOnlyList<string> GetResourceNames() =>
        Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Where(name => name.StartsWith("MarioTypingWindows.Assets.Lessons.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static string[] SentenceFileNames(string prefix)
    {
        return prefix switch
        {
            "H" => new[] { "HSENTEN0.LEE", "HSENTEN1.LEE", "HSENTEN2.LEE" },
            "T" => new[] { "TSENTEN0.LEE", "TSENTEN1.LEE", "TSENTEN2.LEE" },
            "B" => new[] { "BSENTEN0.LEE", "BSENTEN1.LEE", "BSENTEN2.LEE" },
            "N" => new[] { "NSENTENC.LEE" },
            "S" => new[] { "SSENTENC.LEE" },
            _ => Array.Empty<string>(),
        };
    }

    private static List<string> LoadSegments(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(name =>
            name.EndsWith('.' + fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            return [];
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var text = System.Text.Encoding.GetEncoding(437).GetString(memory.ToArray());

        return text.Split('~', StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanSegment)
            .Where(segment => segment.Length > 0)
            .ToList();
    }

    private static string CleanSegment(string value)
    {
        var cleaned = value
            .Replace("\u001a", string.Empty, StringComparison.Ordinal)
            .Replace('`', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        return WhitespaceRegex().Replace(cleaned, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
