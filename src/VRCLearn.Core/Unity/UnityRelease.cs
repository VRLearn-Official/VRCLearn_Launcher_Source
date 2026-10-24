namespace VRCLearn.Core.Unity;

public sealed record UnityDownload(Uri Url)
{
    /// <summary>URL 末段，用作保存对话框里的默认文件名。</summary>
    public string FileName { get; } = Path.GetFileName(Url.LocalPath);
}

public sealed record UnityRelease(string Version, UnityDownload Editor, UnityDownload Android, UnityDownload Ios);
