using YTDownloader.Models;

namespace YTDownloader.Messages;

public record DownloadRequestMessage(IDownloadable DownloadInfo);
