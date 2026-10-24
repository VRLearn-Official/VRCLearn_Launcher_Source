using Microsoft.Win32;
using VRCLearn.Core.Unity;

namespace VRCLearn.Infrastructure.Windows;

public enum SystemCheckKind
{
    UserName,
    DeviceName,
    FileExtensions,
    UnityHub,
    UnityEditor,
}

public enum UnityEdition
{
    Unknown,
    China,
    International,
}

/// <param name="Detail">通过时通常是被检查的值，不通过时是有问题的字符；文件扩展名通过时为空串。</param>
public sealed record SystemCheckResult(SystemCheckKind Kind, bool Passed, string Detail);

public sealed class SystemCheckService(IUnityHubLauncher hub, IUnityEditorLocator editors, HttpClient http)
{
    private const string AdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    private const string HideFileExt = "HideFileExt";

    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(8);

    public static string NonAscii(string value) =>
        string.Concat(value.Where(x => x > 0x7F).Distinct());

    /// <summary>注册表里没有这个值时按系统的默认来：扩展名是藏着的。</summary>
    private static bool ExtensionsShown()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AdvancedKey);

        return key?.GetValue(HideFileExt) is 0;
    }

    /// <summary>官网的下载地址会跳到就近的 CDN，中国版跳去 unitychina。</summary>
    public static UnityEdition EditionOf(Uri? final) =>
        final is null ? UnityEdition.Unknown
        : final.Host.Contains("china", StringComparison.OrdinalIgnoreCase)
            || final.Host.EndsWith(".cn", StringComparison.OrdinalIgnoreCase)
                ? UnityEdition.China
                : UnityEdition.International;

    /// <summary>官网的下载地址最终落到哪个 CDN，据此判断拿到的是哪个版本。</summary>
    public async Task<UnityEdition> EditionAsync(CancellationToken cancellationToken) =>
        EditionOf(await RedirectTargetAsync(UnityReleaseCatalog.Current.Editor.Url.AbsoluteUri, cancellationToken));

    public Task<IReadOnlyList<SystemCheckResult>> RunAsync()
    {
        var found = editors.FindEditors();

        IReadOnlyList<SystemCheckResult> results =
        [
            Name(SystemCheckKind.UserName, ProfileName()),
            Name(SystemCheckKind.DeviceName, Environment.MachineName),
            new SystemCheckResult(SystemCheckKind.FileExtensions, ExtensionsShown(), ""),
            hub.ExecutablePath is { } hubPath
                ? new SystemCheckResult(SystemCheckKind.UnityHub, true, hubPath)
                : new SystemCheckResult(SystemCheckKind.UnityHub, false, ""),
            found.FirstOrDefault(x => x.Version == UnityReleaseCatalog.SupportedVersion) is { } editor
                ? new SystemCheckResult(SystemCheckKind.UnityEditor, true, editor.Version.ToString())
                : new SystemCheckResult(SystemCheckKind.UnityEditor, false, ""),
        ];

        return Task.FromResult(results);
    }

    private static SystemCheckResult Name(SystemCheckKind kind, string value) =>
        NonAscii(value) is { Length: > 0 } bad
            ? new SystemCheckResult(kind, false, bad)
            : new SystemCheckResult(kind, true, value);

    private static string ProfileName()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return Path.GetFileName(profile) is { Length: > 0 } name ? name : Environment.UserName;
    }

    /// <summary>只读到响应头就够，别把整个安装包拉下来。</summary>
    private async Task<Uri?> RedirectTargetAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(NetworkTimeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            return response.RequestMessage?.RequestUri;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return null;
        }
    }
}
