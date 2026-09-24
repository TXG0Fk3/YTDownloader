using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using YTDownloader.Models;

namespace YTDownloader.Services;

public class BrowserService
{
    private const string YouTubeLoginUrl =
        "https://accounts.google.com/ServiceLogin?continue=https%3A%2F%2Fwww.youtube.com";

    private readonly string WebView2CacheFolder = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YTDownloader",
        "WebView2Cache"
    );

    private WebView2? _webView;
    private CoreWebView2? _coreWebView;
    private IBrowserHost? _host;

    private CancellationTokenSource? _interactionCts;

    public void SetHost(IBrowserHost host) => _host = host;

    public async Task<IReadOnlyList<Cookie>> ShowYTLoginAndGetCookies()
    {
        await InitializeWebViewAsync();

        var tcs = new TaskCompletionSource<IReadOnlyList<Cookie>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _ = HandleNavigationCompletedAsync(e, tcs);
        }

        try
        {
            _interactionCts = new();

            _coreWebView!.NavigationCompleted += OnNavigationCompleted;
            _host!.UserCancelledInteraction += OnUserCancelledInteraction;

            _coreWebView!.Navigate(YouTubeLoginUrl);
            _host!.ShowBrowser();

            await using (_interactionCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            _coreWebView!.NavigationCompleted -= OnNavigationCompleted;
            _host!.UserCancelledInteraction -= OnUserCancelledInteraction;

            _host!.HideBrowser();
            CloseWebView();

            _interactionCts?.Dispose();
            _interactionCts = null;
        }
    }

    private async Task InitializeWebViewAsync()
    {
        if (_webView != null)
            return;

        if (_host == null)
            throw new InvalidOperationException(
                "BrowserService requires a host. Call SetHost() before using."
            );

        try
        {
            _webView = new WebView2();
            _host.AttachBrowser(_webView);

            var env = await CoreWebView2Environment.CreateWithOptionsAsync(
                null,
                WebView2CacheFolder,
                null
            );

            await _webView.EnsureCoreWebView2Async(env);
            _coreWebView = _webView.CoreWebView2;

            _coreWebView.Settings.IsScriptEnabled = true;
            _coreWebView.Settings.AreDefaultScriptDialogsEnabled = false;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize WebView2. Ensure WebView2 Runtime is installed.",
                ex
            );
        }
    }

    private void CloseWebView()
    {
        if (_webView == null)
            return;

        _host!.DetachBrowser(_webView);

        _webView.Close();
        _webView = null;
        _coreWebView = null;

        TryDeleteCacheFolder();
    }

    private void OnUserCancelledInteraction() => _interactionCts?.Cancel();

    private void TryDeleteCacheFolder(int attempts = 5)
    {
        for (int i = 0; i < attempts; i++)
        {
            try
            {
                if (Directory.Exists(WebView2CacheFolder))
                    Directory.Delete(WebView2CacheFolder, recursive: true);
                return;
            }
            catch (IOException) when (i < attempts - 1)
            {
                Thread.Sleep(100 * (i + 1));
            }
            catch (UnauthorizedAccessException) when (i < attempts - 1)
            {
                Thread.Sleep(100 * (i + 1));
            }
        }
    }

    private async Task HandleNavigationCompletedAsync(
        CoreWebView2NavigationCompletedEventArgs e,
        TaskCompletionSource<IReadOnlyList<System.Net.Cookie>> tcs
    )
    {
        if (!e.IsSuccess || tcs.Task.IsCompleted)
            return;

        string url = _coreWebView!.Source;
        bool onYouTube =
            url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("accounts.google.com", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("ServiceLogin", StringComparison.OrdinalIgnoreCase);

        if (!onYouTube)
            return;

        await Task.Delay(400);
        var cookies = await _coreWebView.CookieManager.GetCookiesAsync("https://www.youtube.com");
        bool hasSession = cookies.Any(c =>
            c.Name is "SID" or "HSID" or "SSID" or "LOGIN_INFO" or "SAPISID" or "APISID"
        );

        if (!hasSession)
            return;

        var netCookies = cookies.Select(c => WebViewCookieToSystemNetCookie(c)).ToList();
        tcs.TrySetResult(netCookies);
    }

    private Cookie WebViewCookieToSystemNetCookie(CoreWebView2Cookie c) =>
        new(c.Name, c.Value, c.Path, c.Domain)
        {
            Secure = c.IsSecure,
            HttpOnly = c.IsHttpOnly,
            Expires =
                c.Expires <= 0
                    ? DateTime.MinValue
                    : DateTimeOffset.FromUnixTimeSeconds((long)c.Expires).UtcDateTime,
        };
}
