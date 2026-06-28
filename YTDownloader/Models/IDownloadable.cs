using System;
using System.Threading;
using YTDownloader.Enums;

namespace YTDownloader.Models;

public interface IDownloadable
{
    string Id { get; }
    DownloadType Type { get; }
    double Progress { get; }
    DownloadStatus Status { get; }
    Exception? Error { get; }
    CancellationTokenSource CTS { get; }
}
