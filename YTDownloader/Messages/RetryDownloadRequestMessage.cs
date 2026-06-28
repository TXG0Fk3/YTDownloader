using YTDownloader.Models;

namespace YTDownloader.Messages;

public record RetryDownloadRequestMessage(DownloadItem Item);
