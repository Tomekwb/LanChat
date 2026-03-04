using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
        var logPath = GetLogPath(installDir);

        Log(logPath, $"=== CheckAndUpdateIfNeededAsync START ===");
        Log(logPath, $"installDir={installDir}");
        Log(logPath, $"infoUrl={infoUrl}");
        Log(logPath, $"CurrentVersion(local)={CurrentVersion}");

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            Log(logPath, "Downloading version.json...");
            var json = await http.GetStringAsync(infoUrl);
            Log(logPath, $"version.json bytes={Encoding.UTF8.GetByteCount(json)}");

            var info = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (info is null)
            {
                Log(logPath, "ERROR: version.json deserialized to null.");
                return false;
            }

            Log(logPath, $"RemoteVersion={info.version}");
            Log(logPath, $"zipUrl={info.zipUrl}");
            Log(logPath, $"exeName={info.exeName}");
            Log(logPath, $"sha256(remote)={info.sha256}");

            if (!IsNewer(info.version, CurrentVersion))
            {
                Log(logPath, "No update needed (remote is not newer).");
                return false;
            }

            Directory.CreateDirectory(installDir);
            Directory.CreateDirectory(Path.Combine(installDir, "log"));

            var tmpZip = Path.Combine(Path.GetTempPath(), "LanChatClient_update.zip");
            Log(logPath, $"Downloading ZIP to temp: {tmpZip}");

            var zipBytes = await http.GetByteArrayAsync(info.zipUrl);
            await File.WriteAllBytesAsync(tmpZip, zipBytes);
            Log(logPath, $"ZIP downloaded, bytes={zipBytes.Length}");

            var hash = Sha256File(tmpZip);
            Log(logPath, $"sha256(localZip)={hash}");

            if (!hash.Equals(info.sha256, StringComparison.OrdinalIgnoreCase))
            {
                Log(logPath, "ERROR: SHA256 mismatch.");
                throw new Exception("SHA256 mismatch (plik aktualizacji uszkodzony lub nie ten).");
            }

            // Updater.exe musi być obok klienta (w installDir)
            var updater = Path.Combine(installDir, "LanChatUpdater.exe");
            Log(logPath, $"Looking for updater: {updater}");

            if (!File.Exists(updater))
            {
                Log(logPath, "ERROR: Missing LanChatUpdater.exe.");
                throw new Exception("Brak LanChatUpdater.exe w " + installDir);
            }

            var args = $"\"{tmpZip}\" \"{installDir}\" \"{info.exeName}\"";
            Log(logPath, $"Starting updater: {updater} {args}");

            Process.Start(new ProcessStartInfo
            {
                FileName = updater,
                Arguments = args,
                UseShellExecute = true
            });

            Log(logPath, "Updater started OK. Returning true.");
            return true; // caller powinien zamknąć klienta
        }
        catch (Exception ex)
        {
            Log(logPath, "EXCEPTION: " + ex);
            throw;
        }
        finally
        {
            Log(logPath, $"=== CheckAndUpdateIfNeededAsync END ===");
        }
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

    private static string GetLogPath(string installDir)
        => Path.Combine(installDir, "log", "update.log");

    private static void Log(string logPath, string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
            File.AppendAllText(logPath, line, Encoding.UTF8);
        }
        catch
        {
            // logging nie może wywalić update
        }
    }
}