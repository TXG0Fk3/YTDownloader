using YTDownloader.Enums;

namespace YTDownloader.Models.Info;

public class StreamOption
{
    public string Quality { get; init; }
    public MediaFormat Format { get; init; }
    public double SizeMB { get; init; }
}
