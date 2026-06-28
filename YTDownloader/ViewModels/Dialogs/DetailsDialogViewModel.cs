using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Helpers.Builders;
using YTDownloader.Messages;
using YTDownloader.Models;
using YTDownloader.Models.Info;
using YTDownloader.Services;

namespace YTDownloader.ViewModels.Dialogs;

public partial class DetailsDialogViewModel : ObservableObject
{
    private readonly YoutubeService _youtubeService;
    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly IMessenger _messenger;

    private VideoInfo? _video;
    private PlaylistInfo? _playlist;
    private StreamOption? _videoStreamOption;
    private StreamOption? _audioStreamOption;
    private CancellationTokenSource? _cts;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(LoadContentInfoCommand))]
    public partial string UrlBoxText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContentUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ThumbnailUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DefaultFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SizeMB { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<MediaFormat> AvailableFormats { get; set; } =
    [MediaFormat.Mp4, MediaFormat.Mp3];

    [ObservableProperty]
    public partial IReadOnlySet<string> AvailableQualities { get; set; } = new HashSet<string>();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsQualitySelectionEnabled))]
    public partial MediaFormat? SelectedFormat { get; set; }

    [ObservableProperty]
    public partial string? SelectedQuality { get; set; }

    [ObservableProperty]
    public partial bool IsPlaylist { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsQualitySelectionEnabled),
        nameof(IsContentVisible),
        nameof(IsDownloadEnabled)
    )]
    public partial bool IsContentLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsFileNameBoxEnabled),
        nameof(IsContentVisible),
        nameof(IsPlaylistIconVisible),
        nameof(IsDownloadEnabled)
    )]
    public partial bool IsContentLoaded { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsErrorVisible { get; set; }

    public bool IsQualitySelectionEnabled => SelectedFormat == MediaFormat.Mp4 && IsContentLoaded;
    public bool IsFileNameBoxEnabled => !IsPlaylist && IsContentLoaded;
    public bool IsContentVisible => IsContentLoading || IsContentLoaded;
    public bool IsPlaylistIconVisible => IsContentLoaded && IsPlaylist;
    public bool IsDownloadEnabled => IsContentLoaded;

    public DetailsDialogViewModel(
        YoutubeService youtubeService,
        SettingsService settingsService,
        DialogService dialogService,
        IMessenger messenger
    )
    {
        _youtubeService = youtubeService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _messenger = messenger;
    }

    [RelayCommand]
    private async Task OnDownloadAsync()
    {
        IDownloadable? downloadable = null;

        var outputDirectory = await GetFolderPathAsync();
        if (outputDirectory == null)
            return;

        var fileName = !string.IsNullOrEmpty(UserFileName)
            ? FileHelper.SanitizeFileName(UserFileName)
            : DefaultFileName;

        if (!IsPlaylist && _video != null)
        {
            var builder = new DownloadItemBuilder()
                .FromVideoInfo(_video)
                .WithOutputPath(Path.Combine(outputDirectory, fileName));

            downloadable =
                SelectedFormat == MediaFormat.Mp4
                    ? builder
                        .AsVideo(SelectedQuality, _videoStreamOption, _audioStreamOption)
                        .Build()
                    : builder.AsAudio(_audioStreamOption).Build();
        }
        else if (IsPlaylist && _playlist != null)
        {
            downloadable = new DownloadGroupBuilder()
                .FromPlaylistInfo(_playlist)
                .WithOutputPath(Path.Combine(outputDirectory, FileHelper.SanitizeFileName(Title)))
                .WithFormat(
                    SelectedFormat == MediaFormat.Mp4 ? DownloadType.Video : DownloadType.Audio,
                    SelectedQuality
                )
                .Build();
        }

        if (downloadable != null)
            _messenger.Send(new DownloadRequestMessage(downloadable));
    }

    private bool CanLoadContentInfo() =>
        Uri.IsWellFormedUriString(UrlBoxText, UriKind.RelativeOrAbsolute)
        && (
            UrlBoxText.Contains("youtube.com/watch?v=")
            || UrlBoxText.Contains("youtube.com/playlist?list=")
        )
        && (UrlBoxText != ContentUrl);

    [RelayCommand(CanExecute = nameof(CanLoadContentInfo))]
    private async Task LoadContentInfo()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        ResetState();
        IsContentLoading = true;

        try
        {
            if (UrlBoxText.Contains("playlist?list="))
                await LoadPlaylistInfoAsync(token);
            else
                await LoadVideoInfoAsync(token);

            token.ThrowIfCancellationRequested();

            UpdateAvailableQualities();
            IsContentLoaded = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsErrorVisible = true;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsContentLoading = false;
        }
    }

    private void ResetState()
    {
        IsContentLoaded = false;
        IsErrorVisible = false;
        SelectedFormat = MediaFormat.Mp4;
        Title = "Loading...";
        ThumbnailUrl = string.Empty;
        DefaultFileName = "Loading...";
        UserFileName = string.Empty;
        SizeMB = "Loading...";
        ContentUrl = UrlBoxText;
    }

    private async Task LoadVideoInfoAsync(CancellationToken token)
    {
        IsPlaylist = false;
        _video = await _youtubeService.GetVideoAsync(UrlBoxText, token);

        Title = _video.Title;
        ThumbnailUrl = _video.ThumbnailUrl;
        DefaultFileName = FileHelper.SanitizeFileName(_video.Title);
    }

    private async Task LoadPlaylistInfoAsync(CancellationToken token)
    {
        IsPlaylist = true;
        _playlist = await _youtubeService.GetPlaylistAsync(UrlBoxText, token);

        Title = _playlist.Title;
        DefaultFileName = "Custom File Name not available for Playlists.";
    }

    private void UpdateAvailableQualities()
    {
        if (SelectedFormat == MediaFormat.Mp4)
        {
            if (IsPlaylist && _playlist != null)
            {
                AvailableQualities = _playlist.Qualities;
            }
            else
            {
                if (_video != null)
                {
                    AvailableQualities = _video
                        .Streams.Where(s => s.Format == MediaFormat.Mp4)
                        .Select(s => s.Quality)
                        .ToHashSet();
                }
            }
        }
        else
            AvailableQualities = new HashSet<string>() { "Best" };

        SelectedQuality = AvailableQualities.FirstOrDefault();
    }

    private void UpdateSize()
    {
        if (IsPlaylist)
            SizeMB = "Undetermined";
        else if (_videoStreamOption != null && _audioStreamOption != null)
        {
            SizeMB =
                SelectedFormat == MediaFormat.Mp4
                    ? $"{(_videoStreamOption.SizeMB + _audioStreamOption.SizeMB):F1} MB"
                    : $"{_audioStreamOption.SizeMB:F1} MB";
        }
    }

    private async Task<string?> GetFolderPathAsync()
    {
        if (!_settingsService.Current.AlwaysAskWhereSave)
            return _settingsService.Current.DefaultDownloadsPath;

        var path = await _dialogService.OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(path))
            return path;

        return null;
    }

    partial void OnSelectedFormatChanged(MediaFormat? value)
    {
        if (value != null)
            UpdateAvailableQualities();
    }

    partial void OnSelectedQualityChanged(string? value)
    {
        if (value == null)
            return;

        if (!IsPlaylist && _video != null)
        {
            if (SelectedFormat == MediaFormat.Mp4)
                _videoStreamOption = _video.Streams.FirstOrDefault(s =>
                    s.Quality == value && s.Format == MediaFormat.Mp4
                );

            _audioStreamOption = _audioStreamOption = _video.Streams.FirstOrDefault(s =>
                s.Format == MediaFormat.Mp3
            );
        }

        UpdateSize();
    }
}
