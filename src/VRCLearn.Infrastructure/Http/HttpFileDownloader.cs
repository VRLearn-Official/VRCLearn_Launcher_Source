using VRCLearn.Core.Http;
using VRCLearn.Infrastructure.IO;

namespace VRCLearn.Infrastructure.Http;

public sealed class HttpFileDownloader(HttpClient client) : IFileDownloader
{
    private const long ReportInterval = 1 << 20;

    public async Task DownloadAsync(
        Uri url,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await HttpRequests
            .SendAsync(client, HttpRequests.Get(url), cancellationToken)
            .ConfigureAwait(false);

        var total = response.Content.Headers.ContentLength;

        progress?.Report(new DownloadProgress(0, total));

        await using var source = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        var reported = 0L;

        var received = await StreamToFile.CopyAsync(
            source,
            destinationPath,
            value =>
            {
                if (value - reported < ReportInterval)
                {
                    return;
                }

                reported = value;
                progress?.Report(new DownloadProgress(value, total));
            },
            cancellationToken).ConfigureAwait(false);

        progress?.Report(new DownloadProgress(received, total));
    }
}
