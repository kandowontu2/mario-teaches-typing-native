namespace MarioTypingWindows;

internal sealed class TypingSession
{
    private DateTimeOffset? startedAt;
    private DateTimeOffset? completedAt;

    public TypingSession(string prompt, TimeSpan? timeLimit = null)
    {
        Prompt = prompt;
        TimeLimit = timeLimit;
    }

    public string Prompt { get; }

    public TimeSpan? TimeLimit { get; }

    public int Position { get; private set; }

    public int Errors { get; private set; }

    public int CorrectKeystrokes => Position;

    public bool HasStarted => startedAt.HasValue;

    public bool IsComplete => completedAt.HasValue || Position >= Prompt.Length || IsTimeExpired;

    public char? ExpectedCharacter => Position < Prompt.Length ? Prompt[Position] : null;

    public TimeSpan Elapsed
    {
        get
        {
            if (!startedAt.HasValue)
            {
                return TimeSpan.Zero;
            }

            return (completedAt ?? DateTimeOffset.UtcNow) - startedAt.Value;
        }
    }

    public TimeSpan? Remaining => TimeLimit is null
        ? null
        : TimeSpan.FromTicks(Math.Max(0, (TimeLimit.Value - Elapsed).Ticks));

    public bool IsTimeExpired => TimeLimit.HasValue && HasStarted && Elapsed >= TimeLimit.Value;

    public double Accuracy => CorrectKeystrokes + Errors == 0
        ? 1
        : (double)CorrectKeystrokes / (CorrectKeystrokes + Errors);

    public double WordsPerMinute
    {
        get
        {
            return NativeGameEngine.CalculateOriginalWordsPerMinute(
                CorrectKeystrokes,
                Errors,
                Elapsed.TotalSeconds);
        }
    }

    public bool Accept(char input)
    {
        if (IsComplete || char.IsControl(input))
        {
            return false;
        }

        startedAt ??= DateTimeOffset.UtcNow;
        if (input == ExpectedCharacter)
        {
            Position++;
            if (Position >= Prompt.Length)
            {
                completedAt = DateTimeOffset.UtcNow;
            }
            return true;
        }

        Errors++;
        return false;
    }

    public void Finish()
    {
        startedAt ??= DateTimeOffset.UtcNow;
        completedAt ??= DateTimeOffset.UtcNow;
    }
}

internal sealed record SessionResult(
    DateTimeOffset CompletedAt,
    string Student,
    LessonCategory Category,
    ActivityMode Mode,
    int CorrectKeystrokes,
    int Errors,
    double Accuracy,
    double WordsPerMinute,
    double Seconds,
    int LessonLevel = 1,
    string World = "",
    Dictionary<string, int>? ProblemKeys = null)
{
    public int AttemptedKeystrokes => CorrectKeystrokes + Errors;
}
