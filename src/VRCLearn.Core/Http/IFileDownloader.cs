namespace VRCLearn.Core.Http;

public sealed record DownloadProgress(long Received, long? Total);

public interface IFileDownloader
{
    /// <summary>失败或取消时不留下写了一半的文件。</summary>
    Task DownloadAsync(
        Uri url,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
}
