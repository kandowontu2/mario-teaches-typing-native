using System.Text.Json;

namespace MarioTypingWindows;

internal sealed class ProgressStore
{
    private readonly List<SessionResult> results = [];
    private readonly List<StudentProfileData> profiles = [];
    private readonly string filePath;
    private bool[] certificateFlags = [false, false, false];

    private ProgressStore(string filePath)
    {
        this.filePath = filePath;
    }

    public string StudentName { get; private set; } = "Student";

    public int GoalWordsPerMinute { get; private set; } = 5;

    public int LessonLevel { get; private set; } = 1;

    public OriginalCharacter Character { get; private set; } = OriginalCharacter.Mario;

    public OriginalScreen CurrentWorld { get; private set; } = OriginalScreen.Outside;

    public int[] LessonTimesMinutes { get; private set; } = [2, 5, 5, 10];

    public bool MusicEnabled { get; private set; } = true;

    public bool SoundEffectsEnabled { get; private set; } = true;

    public bool SpeechEnabled { get; private set; } = true;

    public NativeDisplayMode DisplayMode { get; private set; }

    public bool WidescreenEnabled => DisplayMode == NativeDisplayMode.Widescreen;

    public IReadOnlyList<SessionResult> Results => results;

    public IReadOnlyList<bool> CertificateFlags => certificateFlags;

    public IReadOnlyList<string> StudentNames
    {
        get
        {
            var names = profiles.Select(profile => profile.StudentName).ToList();
            if (!names.Contains(StudentName, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(StudentName);
            }
            return names;
        }
    }

    public static ProgressStore Load(string? filePath = null)
    {
        filePath ??= AppPaths.ProgressFile;
        try
        {
            if (!File.Exists(filePath))
            {
                return CreateDefault(filePath);
            }

            var jsonText = File.ReadAllText(filePath);
            using var jsonDocument = JsonDocument.Parse(jsonText);
            var hasDisplayMode = jsonDocument.RootElement.ValueKind == JsonValueKind.Object &&
                jsonDocument.RootElement.TryGetProperty(nameof(ProgressData.DisplayMode), out _);
            var data = JsonSerializer.Deserialize<ProgressData>(jsonText);
            var store = new ProgressStore(filePath);
            store.MusicEnabled = data?.MusicEnabled ?? true;
            store.SoundEffectsEnabled = data?.SoundEffectsEnabled ?? true;
            store.SpeechEnabled = data?.SpeechEnabled ?? true;
            store.DisplayMode = NormalizeDisplayMode(
                hasDisplayMode ? data?.DisplayMode : null,
                data?.WidescreenEnabled ?? false);
            if (data?.Profiles is { Count: > 0 })
            {
                foreach (var profile in data.Profiles)
                {
                    var normalized = NormalizeProfile(profile);
                    var existingIndex = store.profiles.FindIndex(item =>
                        item.StudentName.Equals(normalized.StudentName, StringComparison.OrdinalIgnoreCase));
                    if (existingIndex >= 0)
                    {
                        store.profiles[existingIndex] = normalized;
                    }
                    else
                    {
                        store.profiles.Add(normalized);
                    }
                }

                var activeName = NormalizeStudentName(data.StudentName);
                var active = store.profiles.FirstOrDefault(profile =>
                    profile.StudentName.Equals(activeName, StringComparison.OrdinalIgnoreCase))
                    ?? store.profiles[0];
                store.ApplyProfile(active);
            }
            else
            {
                store.StudentName = NormalizeStudentName(data?.StudentName);
                store.GoalWordsPerMinute = Math.Clamp(data?.GoalWordsPerMinute ?? 5, 5, 150);
                store.LessonLevel = Math.Clamp(data?.LessonLevel ?? 1, 1, 6);
                store.Character = NormalizeCharacter(data?.Character ?? OriginalCharacter.Mario);
                store.CurrentWorld = NormalizeWorld(
                    data?.CurrentWorld ?? OriginalScreen.Outside,
                    store.GoalWordsPerMinute);
                store.LessonTimesMinutes = NormalizeLessonTimes(data?.LessonTimesMinutes);
                if (data?.Results is not null)
                {
                    store.results.AddRange(data.Results.OrderBy(result => result.CompletedAt).TakeLast(500));
                }
                store.certificateFlags = NormalizeCertificateFlags(data?.CertificateFlags, store.results);
                store.UpsertCurrentProfile();
            }
            return store;
        }
        catch
        {
            return CreateDefault(filePath);
        }
    }

    public void SetStudentName(string? value) =>
        SetStudentInformation(value, GoalWordsPerMinute, LessonLevel);

    public void SetStudentInformation(
        string? name,
        int goal,
        int lessonLevel,
        OriginalCharacter? character = null)
    {
        var oldName = StudentName;
        StudentName = NormalizeStudentName(name);
        GoalWordsPerMinute = Math.Clamp(goal, 5, 150);
        LessonLevel = Math.Clamp(lessonLevel, 1, 6);
        Character = NormalizeCharacter(character ?? Character);
        if (!oldName.Equals(StudentName, StringComparison.OrdinalIgnoreCase))
        {
            profiles.RemoveAll(profile =>
                profile.StudentName.Equals(oldName, StringComparison.OrdinalIgnoreCase));
            for (var index = 0; index < results.Count; index++)
            {
                results[index] = results[index] with { Student = StudentName };
            }
        }
        Save();
    }

    public void Add(SessionResult result)
    {
        results.Add(result);
        if (results.Count > 500)
        {
            results.RemoveRange(0, results.Count - 500);
        }
        Save();
    }

    public void StartNewStudent(string? value)
    {
        UpsertCurrentProfile();
        StudentName = NormalizeStudentName(value);
        profiles.RemoveAll(profile =>
            profile.StudentName.Equals(StudentName, StringComparison.OrdinalIgnoreCase));
        GoalWordsPerMinute = 5;
        LessonLevel = 1;
        Character = OriginalCharacter.Mario;
        CurrentWorld = OriginalScreen.Outside;
        LessonTimesMinutes = [2, 5, 5, 10];
        certificateFlags = [false, false, false];
        results.Clear();
        Save();
    }

    public bool LoadStudent(string name)
    {
        UpsertCurrentProfile();
        var profile = profiles.FirstOrDefault(item =>
            item.StudentName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            return false;
        }
        ApplyProfile(profile);
        Save();
        return true;
    }

    public void SetLessonLevel(int level)
    {
        LessonLevel = Math.Clamp(level, 1, 6);
        Save();
    }

    public void SetGoalWordsPerMinute(int goal)
    {
        GoalWordsPerMinute = Math.Clamp(goal, 5, 150);
        Save();
    }

    public void SetLessonProgress(int level, int goal)
    {
        LessonLevel = Math.Clamp(level, 1, 6);
        GoalWordsPerMinute = Math.Clamp(goal, 5, 150);
        Save();
    }

    public void SetCurrentWorld(OriginalScreen world)
    {
        CurrentWorld = NormalizeWorld(world, GoalWordsPerMinute);
        Save();
    }

    public bool HasCertificate(OriginalScreen world)
    {
        var index = CertificateIndex(world);
        return index >= 0 && certificateFlags[index];
    }

    public void AwardCertificate(OriginalScreen world)
    {
        var index = CertificateIndex(world);
        if (index < 0 || certificateFlags[index])
        {
            return;
        }
        certificateFlags[index] = true;
        Save();
    }

    public void SetLessonTime(int modeIndex, int minutes)
    {
        if ((uint)modeIndex >= LessonTimesMinutes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(modeIndex));
        }
        LessonTimesMinutes[modeIndex] = Math.Clamp(minutes, 1, 60);
        Save();
    }

    public void SetLessonTimes(IReadOnlyList<int> minutes)
    {
        LessonTimesMinutes = NormalizeLessonTimes(minutes.ToArray());
        Save();
    }

    public void SetAudioSettings(bool music, bool soundEffects, bool speech)
    {
        MusicEnabled = music;
        SoundEffectsEnabled = soundEffects;
        SpeechEnabled = speech;
        Save();
    }

    public void SetDisplaySettings(NativeDisplayMode mode)
    {
        DisplayMode = NormalizeDisplayMode(mode, legacyWidescreen: false);
        Save();
    }

    public double BestWordsPerMinute => results.Count == 0 ? 0 : results.Max(result => result.WordsPerMinute);

    public double AverageAccuracy => results.Count == 0 ? 0 : results.Average(result => result.Accuracy);

    private static ProgressStore CreateDefault(string filePath)
    {
        var store = new ProgressStore(filePath);
        store.UpsertCurrentProfile();
        return store;
    }

    private void ApplyProfile(StudentProfileData profile)
    {
        StudentName = NormalizeStudentName(profile.StudentName);
        GoalWordsPerMinute = Math.Clamp(profile.GoalWordsPerMinute, 5, 150);
        LessonLevel = Math.Clamp(profile.LessonLevel, 1, 6);
        Character = NormalizeCharacter(profile.Character);
        CurrentWorld = NormalizeWorld(profile.CurrentWorld, GoalWordsPerMinute);
        LessonTimesMinutes = NormalizeLessonTimes(profile.LessonTimesMinutes);
        results.Clear();
        results.AddRange((profile.Results ?? []).OrderBy(result => result.CompletedAt).TakeLast(500));
        certificateFlags = NormalizeCertificateFlags(profile.CertificateFlags, results);
    }

    private void UpsertCurrentProfile()
    {
        var snapshot = new StudentProfileData(
            StudentName,
            results.ToList(),
            GoalWordsPerMinute,
            LessonLevel,
            Character,
            CurrentWorld,
            (int[])LessonTimesMinutes.Clone(),
            (bool[])certificateFlags.Clone());
        var index = profiles.FindIndex(profile =>
            profile.StudentName.Equals(StudentName, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            profiles[index] = snapshot;
        }
        else
        {
            profiles.Add(snapshot);
        }
    }

    private void Save()
    {
        UpsertCurrentProfile();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var data = new ProgressData(
            StudentName,
            results.ToList(),
            GoalWordsPerMinute,
            LessonLevel,
            Character,
            CurrentWorld,
            (int[])LessonTimesMinutes.Clone(),
            (bool[])certificateFlags.Clone(),
            profiles.Select(CloneProfile).ToList(),
            MusicEnabled,
            SoundEffectsEnabled,
            SpeechEnabled,
            DisplayMode != NativeDisplayMode.Classic,
            DisplayMode);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var temporary = filePath + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, filePath, overwrite: true);
    }

    private static StudentProfileData NormalizeProfile(StudentProfileData profile) => new(
        NormalizeStudentName(profile.StudentName),
        (profile.Results ?? []).OrderBy(result => result.CompletedAt).TakeLast(500).ToList(),
        Math.Clamp(profile.GoalWordsPerMinute, 5, 150),
        Math.Clamp(profile.LessonLevel, 1, 6),
        NormalizeCharacter(profile.Character),
        NormalizeWorld(profile.CurrentWorld, profile.GoalWordsPerMinute),
        NormalizeLessonTimes(profile.LessonTimesMinutes),
        NormalizeCertificateFlags(profile.CertificateFlags, profile.Results));

    private static StudentProfileData CloneProfile(StudentProfileData profile) => new(
        profile.StudentName,
        profile.Results.ToList(),
        profile.GoalWordsPerMinute,
        profile.LessonLevel,
        profile.Character,
        profile.CurrentWorld,
        (int[])profile.LessonTimesMinutes.Clone(),
        (bool[])NormalizeCertificateFlags(profile.CertificateFlags, profile.Results).Clone());

    private static OriginalCharacter NormalizeCharacter(OriginalCharacter value) =>
        value is OriginalCharacter.Mario or OriginalCharacter.Luigi or OriginalCharacter.Princess
            ? value
            : OriginalCharacter.Mario;

    private static NativeDisplayMode NormalizeDisplayMode(
        NativeDisplayMode? value,
        bool legacyWidescreen) => value switch
    {
        NativeDisplayMode.Widescreen => NativeDisplayMode.Widescreen,
        NativeDisplayMode.UltraWidescreen => NativeDisplayMode.UltraWidescreen,
        NativeDisplayMode.Classic => NativeDisplayMode.Classic,
        _ => legacyWidescreen ? NativeDisplayMode.Widescreen : NativeDisplayMode.Classic,
    };

    private static OriginalScreen NormalizeWorld(OriginalScreen world, int goalWordsPerMinute) => world switch
    {
        OriginalScreen.Practice => OriginalScreen.Practice,
        OriginalScreen.Castle when goalWordsPerMinute >= 30 => OriginalScreen.Castle,
        OriginalScreen.Water when goalWordsPerMinute >= 10 => OriginalScreen.Water,
        OriginalScreen.Outside => OriginalScreen.Outside,
        _ => OriginalScreen.Outside,
    };

    private static string NormalizeStudentName(string? value)
    {
        var trimmed = new string((value ?? "")
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? "Student"
            : trimmed[..Math.Min(trimmed.Length, 8)];
    }

    private static int[] NormalizeLessonTimes(int[]? values)
    {
        var normalized = new[] { 2, 5, 5, 10 };
        if (values is null)
        {
            return normalized;
        }
        for (var index = 0; index < normalized.Length && index < values.Length; index++)
        {
            normalized[index] = Math.Clamp(values[index], 1, 60);
        }
        return normalized;
    }

    private static bool[] NormalizeCertificateFlags(
        bool[]? values,
        IReadOnlyList<SessionResult>? legacyResults)
    {
        if (values is { Length: >= 3 })
        {
            return [values[0], values[1], values[2]];
        }

        // Old native profiles did not persist the original three certificate
        // bits. Reconstruct only outcomes that could have awarded them under
        // the DOS progression rules, then persist explicit bits from now on.
        var inferred = new[] { false, false, false };
        foreach (var result in legacyResults ?? [])
        {
            if (result.LessonLevel != 6)
            {
                continue;
            }
            if (string.Equals(result.World, OriginalScreen.Outside.ToString(), StringComparison.OrdinalIgnoreCase) &&
                result.WordsPerMinute >= 9)
            {
                inferred[0] = true;
            }
            else if (string.Equals(result.World, OriginalScreen.Water.ToString(), StringComparison.OrdinalIgnoreCase) &&
                     result.WordsPerMinute >= 26)
            {
                inferred[1] = true;
            }
            else if (string.Equals(result.World, OriginalScreen.Castle.ToString(), StringComparison.OrdinalIgnoreCase) &&
                     result.WordsPerMinute >= 40)
            {
                inferred[2] = true;
            }
        }
        return inferred;
    }

    private static int CertificateIndex(OriginalScreen world) => world switch
    {
        OriginalScreen.Outside => 0,
        OriginalScreen.Water => 1,
        OriginalScreen.Castle => 2,
        _ => -1,
    };

    private sealed record StudentProfileData(
        string StudentName,
        List<SessionResult> Results,
        int GoalWordsPerMinute,
        int LessonLevel,
        OriginalCharacter Character,
        OriginalScreen CurrentWorld,
        int[] LessonTimesMinutes,
        bool[]? CertificateFlags)
    {
        public StudentProfileData() : this(
            "Student", [], 5, 1, OriginalCharacter.Mario, OriginalScreen.Outside,
            [2, 5, 5, 10], [false, false, false]) { }
    }

    private sealed record ProgressData(
        string StudentName,
        List<SessionResult> Results,
        int GoalWordsPerMinute,
        int LessonLevel,
        OriginalCharacter Character,
        OriginalScreen CurrentWorld,
        int[] LessonTimesMinutes,
        bool[]? CertificateFlags,
        List<StudentProfileData>? Profiles,
        bool? MusicEnabled,
        bool? SoundEffectsEnabled,
        bool? SpeechEnabled,
        bool? WidescreenEnabled,
        NativeDisplayMode? DisplayMode)
    {
        public ProgressData() : this(
            "Student", [], 5, 1, OriginalCharacter.Mario, OriginalScreen.Outside,
            [2, 5, 5, 10], [false, false, false], null, true, true, true, false,
            NativeDisplayMode.Classic) { }
    }
}
