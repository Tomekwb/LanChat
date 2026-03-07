using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace LanChatUpdater;

internal static class Program
{
    private static readonly string GlobalLogDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LanChat");

    private static readonly string GlobalLogPath =
        Path.Combine(GlobalLogDir, "updater.log");

    private const string ChildModeFlag = "--do-swap";

    private static int Main(string[] args)
    {
        Log("============================================================");
        Log("LanChatUpdater START");

        try
        {
            if (args.Length >= 1 && string.Equals(args[0], ChildModeFlag, StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 4)
                {
                    Log("ERROR: invalid child args count. Expected: --do-swap <zipPath> <installDir> <exeName>");
                    return 2;
                }

                var zipPath = args[1];
                var installDir = args[2];
                var exeName = args[3];

                Log($"CHILD MODE zipPath={zipPath}");
                Log($"CHILD MODE installDir={installDir}");
                Log($"CHILD MODE exeName={exeName}");

                RunSwap(zipPath, installDir, exeName);
                Log("LanChatUpdater END OK (child)");
                return 0;
            }

            if (args.Length < 3)
            {
                Log("ERROR: invalid args count. Expected: <zipPath> <installDir> <exeName>");
                Console.WriteLine("Usage: LanChatUpdater <zipPath> <installDir> <exeName>");
                return 2;
            }

            var parentZipPath = args[0];
            var parentInstallDir = args[1];
            var parentExeName = args[2];

            Log($"PARENT MODE zipPath={parentZipPath}");
            Log($"PARENT MODE installDir={parentInstallDir}");
            Log($"PARENT MODE exeName={parentExeName}");

            StartTempChild(parentZipPath, parentInstallDir, parentExeName);

            Log("LanChatUpdater parent end OK");
            return 0;
        }
        catch (Exception ex)
        {
            Log("FATAL ERROR: " + ex);
            Console.WriteLine("ERROR: " + ex);
            return 1;
        }
    }

    private static void StartTempChild(string zipPath, string installDir, string exeName)
    {
        if (string.IsNullOrWhiteSpace(zipPath))
            throw new ArgumentException("zipPath is empty");

        if (string.IsNullOrWhiteSpace(installDir))
            throw new ArgumentException("installDir is empty");

        if (string.IsNullOrWhiteSpace(exeName))
            throw new ArgumentException("exeName is empty");

        if (!File.Exists(zipPath))
            throw new FileNotFoundException("ZIP not found", zipPath);

        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
            throw new FileNotFoundException("Cannot determine current updater EXE path", currentExe);

        var currentDir = Path.GetDirectoryName(currentExe)!;
        var tempUpdaterDir = Path.Combine(Path.GetTempPath(), "LanChatUpdaterRun");

        SafeDeleteDir(tempUpdaterDir);
        Directory.CreateDirectory(tempUpdaterDir);

        Log("Copy current install dir -> temp child dir");
        CopyDirectoryRecursive(currentDir, tempUpdaterDir);

        var tempExePath = Path.Combine(tempUpdaterDir, "LanChatUpdater.exe");
        if (!File.Exists(tempExePath))
            throw new FileNotFoundException("Temp child EXE missing", tempExePath);

        Log("Temp child prepared: " + tempExePath);

        var psi = new ProcessStartInfo
        {
            FileName = tempExePath,
            Arguments = $"{ChildModeFlag} \"{zipPath}\" \"{installDir}\" \"{exeName}\"",
            UseShellExecute = true,
            WorkingDirectory = tempUpdaterDir
        };

        Log("Starting temp child: " + psi.FileName + " " + psi.Arguments);
        Process.Start(psi);
    }

    private static void RunSwap(string zipPath, string installDir, string exeName)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException("ZIP not found", zipPath);

        Directory.CreateDirectory(installDir);

        var stageRoot = Path.Combine(Path.GetTempPath(), "LanChatUpdate_" + Guid.NewGuid().ToString("N"));
        var newDir = Path.Combine(stageRoot, "new");
        var backupDir = installDir.TrimEnd('\\') + "__OLD_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        Log($"StageRoot={stageRoot}");
        Log($"NewDir={newDir}");
        Log($"BackupDir={backupDir}");

        Directory.CreateDirectory(stageRoot);
        Directory.CreateDirectory(newDir);

        try
        {
            Log("Extract ZIP -> newDir");
            ZipFile.ExtractToDirectory(zipPath, newDir, overwriteFiles: true);
            Log("Extract OK");

            var newExe = Path.Combine(newDir, exeName);
            Log($"Check extracted exe: {newExe}");
            if (!File.Exists(newExe))
                throw new FileNotFoundException("EXE not found inside extracted ZIP", newExe);

            Log("Kill running client processes");
            KillByName(Path.GetFileNameWithoutExtension(exeName));
            Thread.Sleep(1500);

            if (Directory.Exists(backupDir))
            {
                Log("BackupDir already exists, deleting old leftover backup");
                SafeDeleteDir(backupDir);
            }

            if (Directory.Exists(installDir))
            {
                if (!Directory.EnumerateFileSystemEntries(installDir).Any())
                {
                    Log("installDir exists but is empty -> delete empty dir");
                    SafeDeleteDir(installDir);
                }
                else
                {
                    Log($"MOVE installDir -> backupDir: {installDir} -> {backupDir}");
                    RetryIO(
                        () => Directory.Move(installDir, backupDir),
                        attempts: 20,
                        delayMs: 750,
                        opName: "Directory.Move installDir -> backupDir");
                    Log("MOVE installDir -> backupDir OK");
                }
            }
            else
            {
                Log("installDir does not exist -> fresh install path");
            }

            Log($"MOVE newDir -> installDir: {newDir} -> {installDir}");
            RetryIO(
                () => Directory.Move(newDir, installDir),
                attempts: 20,
                delayMs: 750,
                opName: "Directory.Move newDir -> installDir");
            Log("MOVE newDir -> installDir OK");

            var exePath = Path.Combine(installDir, exeName);
            Log($"Check final exe: {exePath}");
            if (!File.Exists(exePath))
                throw new FileNotFoundException("EXE not found after update", exePath);

            Log("Start updated client");
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
            Log("Client start requested");

            SafeDeleteFile(zipPath);
            Log("ZIP deleted");
        }
        catch (Exception ex)
        {
            Log("ERROR during update: " + ex);

            try
            {
                if (Directory.Exists(installDir))
                {
                    Log("Rollback: delete broken installDir");
                    SafeDeleteDir(installDir);
                }

                if (Directory.Exists(backupDir))
                {
                    Log($"Rollback: MOVE backupDir -> installDir: {backupDir} -> {installDir}");
                    RetryIO(
                        () => Directory.Move(backupDir, installDir),
                        attempts: 10,
                        delayMs: 500,
                        opName: "Directory.Move backupDir -> installDir");
                    Log("Rollback OK");
                }
                else
                {
                    Log("Rollback skipped: backupDir does not exist");
                }
            }
            catch (Exception rollbackEx)
            {
                Log("ROLLBACK ERROR: " + rollbackEx);
            }

            throw;
        }
        finally
        {
            Log("Cleanup temp stageRoot");
            SafeDeleteDir(stageRoot);
        }
    }

    private static void CopyDirectoryRecursive(string srcDir, string dstDir)
    {
        Directory.CreateDirectory(dstDir);

        foreach (var dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(srcDir, dir);
            Directory.CreateDirectory(Path.Combine(dstDir, rel));
        }

        foreach (var file in Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(srcDir, file);
            var name = Path.GetFileName(file);

            if (string.Equals(name, "updater.log", StringComparison.OrdinalIgnoreCase))
                continue;

            var dst = Path.Combine(dstDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(file, dst, overwrite: true);
        }
    }

    private static void KillByName(string procName)
    {
        if (string.IsNullOrWhiteSpace(procName))
            return;

        foreach (var p in Process.GetProcessesByName(procName))
        {
            try
            {
                Log($"Kill pid={p.Id} name={p.ProcessName}");
                p.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Log($"WARN kill failed pid={p.Id}: {ex.Message}");
            }
        }
    }

    private static void RetryIO(Action action, int attempts, int delayMs, string opName)
    {
        Exception? last = null;

        for (int i = 1; i <= attempts; i++)
        {
            try
            {
                Log($"{opName} attempt {i}/{attempts}");
                action();
                return;
            }
            catch (IOException ex)
            {
                last = ex;
                Log($"{opName} IOException attempt {i}/{attempts}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                last = ex;
                Log($"{opName} UnauthorizedAccessException attempt {i}/{attempts}: {ex.Message}");
            }

            Thread.Sleep(delayMs);
        }

        throw new Exception($"{opName} failed after {attempts} attempts", last);
    }

    private static void SafeDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Log($"WARN SafeDeleteFile failed: {path} :: {ex.Message}");
        }
    }

    private static void SafeDeleteDir(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            Log($"WARN SafeDeleteDir failed: {path} :: {ex.Message}");
        }
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(GlobalLogDir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(GlobalLogPath, line);
        }
        catch
        {
        }
    }
}