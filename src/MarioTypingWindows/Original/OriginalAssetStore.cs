using System.Reflection;

namespace MarioTypingWindows.Original;

internal sealed class OriginalAssetStore
{
    private const string GamePrefix = "MarioTypingWindows.Assets.Game.";
    private readonly Assembly assembly;
    private readonly Dictionary<string, string> resources;

    public OriginalAssetStore()
    {
        assembly = Assembly.GetExecutingAssembly();
        resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(GamePrefix, StringComparison.Ordinal))
            .ToDictionary(
                name => Normalize(name[GamePrefix.Length..]),
                name => name,
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> Paths => resources.Keys;

    public byte[] ReadAllBytes(string path)
    {
        using var stream = Open(path);
        if (stream.Length > int.MaxValue)
        {
            throw new InvalidDataException($"Embedded asset is too large to buffer: {path}");
        }

        var data = new byte[stream.Length];
        stream.ReadExactly(data);
        return data;
    }

    public string ReadAllText(string path)
    {
        using var stream = Open(path);
        using var reader = new StreamReader(stream, System.Text.Encoding.ASCII, false);
        return reader.ReadToEnd();
    }

    public Stream Open(string path)
    {
        var normalized = Normalize(path);
        if (!resources.TryGetValue(normalized, out var resourceName))
        {
            var matches = resources
                .Where(pair => pair.Key.EndsWith('/' + normalized, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Value)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new FileNotFoundException($"Embedded original game asset was not found: {path}");
            }
            resourceName = matches[0];
        }

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded resource cannot be opened: {resourceName}");
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
