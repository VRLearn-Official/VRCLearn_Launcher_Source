using VRCLearn.Core.Versioning;

namespace VRCLearn.Core.Unity;

public static class UnityReleaseCatalog
{
    /// <summary>本程序针对的 Unity 版本，换版本时连同下面的哈希一起改。</summary>
    public static UnityVersion SupportedVersion { get; } = new(2022, 3, 22, ReleaseType.Normal, 1);

    internal const string EditorHash = "887be4894c44";

    public static UnityRelease Current { get; } = new(
        SupportedVersion.ToString(),
        new(new Uri($"https://download.unity3d.com/download_unity/{EditorHash}/Windows64EditorInstaller/UnitySetup64-{SupportedVersion}.exe")),
        new(new Uri($"https://download.unity3d.com/download_unity/{EditorHash}/TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-{SupportedVersion}.exe")),
        new(new Uri($"https://download.unity3d.com/download_unity/{EditorHash}/TargetSupportInstaller/UnitySetup-iOS-Support-for-Editor-{SupportedVersion}.exe")));

    public static UnityDownload UnityHub { get; } =
        new(new Uri("https://github.com/DanKE123abc/NoUnityCN/releases/latest/download/UnityHubSetup-x64.exe"));
}
