using System;
using YTDownloader.Enums;

namespace YTDownloader.ViewModels.Components;

public interface IDownloadableViewModel : IDisposable
{
    string Title { get; }
    double Progress { get; }
    DownloadStatus Status { get; }
}
