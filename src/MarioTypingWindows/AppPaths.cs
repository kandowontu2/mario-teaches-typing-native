namespace MarioTypingWindows;

internal static class AppPaths
{
    public static string UserDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MarioTypingNative");

    public static string ProgressFile { get; } =
        Path.Combine(UserDataDirectory, "progress.json");
}
