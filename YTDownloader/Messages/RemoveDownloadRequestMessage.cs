using YTDownloader.Models;
using YTDownloader.ViewModels.Components;

namespace YTDownloader.Messages;

public record RemoveDownloadRequestMessage(
    IDownloadableViewModel DownloadableViewModel,
    IDownloadable? Downloadable = null
);
