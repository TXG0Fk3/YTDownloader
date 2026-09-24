using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace YTDownloader.Services;

public class CredentialService
{
    private readonly string _filePath;

    public event Action? CookiesUpdated;

    public CredentialService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YTDownloader",
            "youtube_cookies.dat"
        );
    }

    public void SaveCookies(IReadOnlyList<Cookie> cookies)
    {
        var data = cookies
            .Select(c => new CookieDto
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Secure = c.Secure,
                HttpOnly = c.HttpOnly,
                Expires = c.Expires == DateTime.MinValue ? null : c.Expires,
            })
            .ToList();

        byte[] plain = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
        byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        var tmp = _filePath + ".tmp";
        File.WriteAllBytes(tmp, encrypted);
        File.Move(tmp, _filePath, overwrite: true);

        CookiesUpdated?.Invoke();
    }

    public IReadOnlyList<Cookie>? LoadCookies()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            byte[] encrypted = File.ReadAllBytes(_filePath);
            byte[] plain = ProtectedData.Unprotect(
                encrypted,
                null,
                DataProtectionScope.CurrentUser
            );

            var items = JsonSerializer.Deserialize<List<CookieDto>>(Encoding.UTF8.GetString(plain));
            if (items is null || items.Count == 0)
                return null;

            return items
                .Select(i => new Cookie(i.Name, i.Value, i.Path, i.Domain)
                {
                    Secure = i.Secure,
                    HttpOnly = i.HttpOnly,
                    Expires = i.Expires ?? DateTime.MinValue,
                })
                .ToList();
        }
        catch (CryptographicException)
        {
            ClearCookies();
            return null;
        }
    }

    public void ClearCookies()
    {
        try
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);

            var tmp = _filePath + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);

            CookiesUpdated?.Invoke();
        }
        catch { }
    }

    public bool HasCookies() => File.Exists(_filePath);

    private sealed class CookieDto
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Path { get; set; } = "/";
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
        public DateTime? Expires { get; set; }
    }
}
