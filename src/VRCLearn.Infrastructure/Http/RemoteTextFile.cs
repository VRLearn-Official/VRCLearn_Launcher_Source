using System.Net;

namespace VRCLearn.Infrastructure.Http;

public enum RemoteTextOutcome
{
    Fetched,
    NotFound,
    Failed,
}

/// <param name="Detail">没取到时的原因，只用来写日志。</param>
public sealed record RemoteText(RemoteTextOutcome Outcome, string Text, string Detail)
{
    public static RemoteText NotFound { get; } = new(RemoteTextOutcome.NotFound, "", "");

    internal static RemoteText Failure(string detail) => new(RemoteTextOutcome.Failed, "", detail);
}

/// <summary>拉一段纯文本。找不到单独标出来，算不算错由调用方定。</summary>
public static class RemoteTextFile
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

    /// <param name="url">地址无效时同样按未取到处理。</param>
    public static async Task<RemoteText> GetAsync(
        HttpClient http,
        string url,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(Timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Absolute));
            using var response = await http
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return RemoteText.NotFound;
            }

            response.EnsureSuccessStatusCode();

            return new RemoteText(
                RemoteTextOutcome.Fetched,
                await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false),
                "");
        }
        catch (Exception exception) when (exception is HttpRequestException
            or TaskCanceledException
            or IOException
            or UriFormatException
            or InvalidOperationException)
        {
            return RemoteText.Failure(exception.Message);
        }
    }
}
