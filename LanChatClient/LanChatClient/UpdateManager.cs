using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace LanChatClient;

public static class UpdateManager
{
    public record UpdateInfo(string version, string zipUrl, string sha256, string exeName);

    public static string CurrentVersion =>
        (typeof(UpdateManager).Assembly.GetName().Version?.ToString() ?? "0.0.0");

    public static async Task<bool> CheckAndUpdateIfNeededAsync(string baseUrl, string installDir)
    {
        // baseUrl: http://192.168.64.233:5001/updates
        var infoUrl = baseUrl.TrimEnd('/') + "/version.json";

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var json = await http.GetStringAsync(infoUrl);

        var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (info is null) return false;

        if (!IsNewer(info.version, CurrentVersion))
            return false;

        Directory.CreateDirectory(installDir);

        var tmpZip = Path.Combine(Path.GetTempPath(), "LanChatClient_update.zip");

        var zipBytes = await http.GetByteArrayAsync(info.zipUrl);
        await File.WriteAllBytesAsync(tmpZip, zipBytes);

        var hash = Sha256File(tmpZip);
        if (!hash.Equals(info.sha256, StringComparison.OrdinalIgnoreCase))
            throw new Exception("SHA256 mismatch (plik aktualizacji uszkodzony lub nie ten).");

        // Updater.exe musi być obok klienta (w installDir)
        var updater = Path.Combine(installDir, "LanChatUpdater.exe");
        if (!File.Exists(updater))
            throw new Exception("Brak LanChatUpdater.exe w " + installDir);

        Process.Start(new ProcessStartInfo
        {
            FileName = updater,
            Arguments = $"\"{tmpZip}\" \"{installDir}\" \"{info.exeName}\"",
            UseShellExecute = true
        });

        return true; // klient powinien się zamknąć po uruchomieniu updatera
    }

    private static bool IsNewer(string remote, string local)
    {
        if (Version.TryParse(remote, out var r) && Version.TryParse(local, out var l))
            return r > l;

        // fallback
        return !string.Equals(remote, local, StringComparison.OrdinalIgnoreCase);
    }

    private static string Sha256File(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = sha.ComputeHash(fs);
        return Convert.ToHexString(hash);
    }
}
