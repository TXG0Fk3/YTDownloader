using YTDownloader.Enums;

namespace YTDownloader.Models.Info;

public class StreamOption
{
    public required string Quality { get; set; }
    public MediaFormat Format { get; set; }
    public double SizeMB { get; set; }
}
