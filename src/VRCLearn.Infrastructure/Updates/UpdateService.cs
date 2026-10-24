using Microsoft.Extensions.Logging;
using VRCLearn.Infrastructure.Http;

namespace VRCLearn.Infrastructure.Updates;

public enum UpdateKind
{
    UpToDate,

    UpdateAvailable,

    Failed,
}

public sealed record UpdateStatus(UpdateKind Kind, string? Version = null, string? DownloadUrl = null);

public sealed class UpdateService(HttpClient http, ILogger<UpdateService> logger)
{
    private const string DefaultVersionUrl = "https://start.gamemode1.com/pages/vrclearnlauncher/version.txt";

    private const string DefaultDownloadUrl = "https://start.gamemode1.com/pages/vrclearnlauncher/";

    public string CurrentVersionText =>
        typeof(UpdateService).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public async Task<UpdateStatus> CheckAsync()
    {
        var remote = await RemoteTextFile.GetAsync(http, VersionUrl(), CancellationToken.None);

        if (remote.Outcome != RemoteTextOutcome.Fetched)
        {
            logger.LogWarning("获取最新版本号失败：{Detail}", remote.Detail);

            return new UpdateStatus(UpdateKind.Failed);
        }

        var text = remote.Text.Trim().TrimStart('﻿').Trim().TrimStart('v', 'V').Trim();

        if (ParseVersion(text) is not { } latest)
        {
            logger.LogWarning("版本号格式不对：{Text}", text);

            return new UpdateStatus(UpdateKind.Failed);
        }

        var current = Version.TryParse(CurrentVersionText, out var local) ? local : new Version(0, 0);

        if (latest > current)
        {
            logger.LogInformation("发现新版本：{Latest}，当前 {Current}", latest, current);

            return new UpdateStatus(UpdateKind.UpdateAvailable, latest.ToString(), DownloadUrl());
        }

        logger.LogInformation("已是最新版本：{Current}", current);

        return new UpdateStatus(UpdateKind.UpToDate);
    }

    private static Version? ParseVersion(string text) =>
        text.Split('.') is { Length: 3 } parts
        && parts.All(x => int.TryParse(x, out _))
        && Version.TryParse(text, out var version)
            ? version
            : null;

    private static string VersionUrl() =>
        Environment.GetEnvironmentVariable("VRCL_VERSION_URL") is { Length: > 0 } custom
            ? custom
            : DefaultVersionUrl;

    private static string DownloadUrl() =>
        Environment.GetEnvironmentVariable("VRCL_DOWNLOAD_URL") is { Length: > 0 } custom
            ? custom
            : DefaultDownloadUrl;
}
