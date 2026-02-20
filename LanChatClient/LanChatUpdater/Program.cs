using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

static bool IsRuntimeFile(string fileName)
{
    // twarda czarna lista najbardziej problematycznych/zbędnych runtime plików
    var n = fileName.ToLowerInvariant();

    if (n is "clrjit.dll" or "coreclr.dll" or "hostfxr.dll" or "hostpolicy.dll" or "createdump.exe" or "clrgc.dll")
        return true;

    if (n.StartsWith("mscordaccore")) return true;
    if (n is "mscordbi.dll" or "mscorrc.dll" or "clretwrc.dll") return true;

    // większość runtime'owych bibliotek systemowych – nie aktualizujemy ich updaterem
    if (n.StartsWith("system.") || n.StartsWith("microsoft.netcore.") || n.StartsWith("windowsbase.dll"))
        return true;

    // sam .NET runtime corelib i hosty
    if (n is "system.private.corelib.dll" or "netstandard.dll" or "mscorlib.dll")
        return true;

    return false;
}

static bool ShouldCopy(string relPath)
{
    // relPath w formie np. "LanChatClient.exe" albo "pl\\something.dll"
    var fileName = Path.GetFileName(relPath);

    // nie kopiujemy katalogów językowych (opcjonalnie: możesz zostawić true jeśli chcesz je aktualizować)
    // Jeśli chcesz je aktualizować, usuń ten blok.
    var dir = Path.GetDirectoryName(relPath);
    if (!string.IsNullOrEmpty(dir))
    {
        var top = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault() ?? "";
        // typowe foldery lokalizacji w publish
        if (top.Length == 2 || top.Contains('-')) // np. "pl", "de", "pt-BR"
        {
            // foldery językowe zwykle nie są krytyczne — pomijamy, żeby zmniejszyć ryzyko locków
            return false;
        }
    }

    // pomijamy runtime
    if (IsRuntimeFile(fileName))
        return false;

    // whitelist: nasze pliki + znane zależności aplikacji
    var n = fileName.ToLowerInvariant();
    if (n.StartsWith("lanchatclient.")) return true;
    if (n == "lanchatclient.exe") return true;
    if (n.StartsWith("microsoft.aspnetcore.")) return true;
    if (n.StartsWith("microsoft.extensions.")) return true;
    if (n.StartsWith("hardcodet.")) return true;
    if (n.StartsWith("system.text.json")) return true;
    if (n.StartsWith("system.io.pipelines")) return true;
    if (n.StartsWith("system.threading.channels")) return true;
    if (n.StartsWith("system.text.encodings.web")) return true;

    // reszta – nie ruszamy
    return false;
}

static void CopyFiltered(string srcRoot, string dstRoot)
{
    Directory.CreateDirectory(dstRoot);

    foreach (var file in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
    {
        var rel = Path.GetRelativePath(srcRoot, file);

        // normalizacja separatorów
        rel = rel.Replace('/', Path.DirectorySeparatorChar);

        if (!ShouldCopy(rel))
            continue;

        var target = Path.Combine(dstRoot, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        File.Copy(file, target, true);
    }
}

static void TryDelete(string path)
{
    try { if (File.Exists(path)) File.Delete(path); } catch { }
}

static void TryDeleteDir(string path)
{
    try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
}

// args: <zipPath> <installDir> <exeName>
if (args.Length < 3)
{
    Console.WriteLine("Usage: LanChatUpdater <zipPath> <installDir> <exeName>");
    Environment.Exit(2);
}

var zipPath = args[0];
var installDir = args[1];
var exeName = args[2];

if (!File.Exists(zipPath))
{
    Console.WriteLine("ZIP not found: " + zipPath);
    Environment.Exit(3);
}

Directory.CreateDirectory(installDir);

var staging = Path.Combine(Path.GetTempPath(), "LanChatUpdate_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(staging);

try
{
    ZipFile.ExtractToDirectory(zipPath, staging, true);

    // zabij instancje klienta (jeśli żyją)
    var procName = Path.GetFileNameWithoutExtension(exeName);
    foreach (var p in Process.GetProcessesByName(procName))
    {
        try { p.Kill(true); } catch { }
    }

    // krótka pauza na zwolnienie uchwytów
    System.Threading.Thread.Sleep(500);

    // Podmień tylko wybrane pliki (bez runtime .NET)
    CopyFiltered(staging, installDir);

    // uruchom klienta
    var exePath = Path.Combine(installDir, exeName);
    if (File.Exists(exePath))
    {
        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
    }
    else
    {
        Console.WriteLine("EXE not found after update: " + exePath);
        Environment.Exit(4);
    }
}
catch (Exception ex)
{
    Console.WriteLine("ERROR: " + ex);
    Environment.Exit(1);
}
finally
{
    // zip usuwamy (to jest OK)
    TryDelete(zipPath);
    TryDeleteDir(staging);
}
