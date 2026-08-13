using System.Reflection;
using System.Security.Cryptography;

namespace MarioTypingWindows;

internal static class EmbeddedAssets
{
    public const string OriginalCdResource = "MarioTypingWindows.Assets.OriginalCd.chd";
    public const string OriginalInstallResource = "MarioTypingWindows.Assets.OriginalInstall.vhd";
    public const string CdAudioTrackResource = "MarioTypingWindows.Assets.CdAudio.Track02.wav";
    public const long ExpectedCdLength = 143_617_768;
    public const long ExpectedInstallLength = 7_340_544;
    public const long ExpectedCdAudioTrackLength = 42_129_068;
    public const string ExpectedCdSha256 = "3481BE69AA99F89A68CCAB4C1C79419A7D85C6A5CD9B696FD266C7A4E5235E08";
    public const string ExpectedInstallSha256 = "38B00F9533EBAEE12A54B0B01C6DF81004F5B9E436D62F754178A74EB3889967";

    public static Stream OpenOriginalCd() => OpenRequired(OriginalCdResource);

    public static Stream OpenOriginalInstall() => OpenRequired(OriginalInstallResource);

    public static Stream OpenCdAudioTrack() => OpenRequired(CdAudioTrackResource);

    public static (long CdBytes, long InstallBytes) GetEmbeddedMediaLengths()
    {
        using var cd = OpenOriginalCd();
        using var install = OpenOriginalInstall();
        return (cd.Length, install.Length);
    }

    public static (string CdSha256, string InstallSha256) GetEmbeddedMediaHashes()
    {
        using var cd = OpenOriginalCd();
        using var install = OpenOriginalInstall();
        return (Convert.ToHexString(SHA256.HashData(cd)), Convert.ToHexString(SHA256.HashData(install)));
    }

    private static Stream OpenRequired(string name)
    {
        return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidDataException($"Required embedded asset is missing: {name}");
    }
}
