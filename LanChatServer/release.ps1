param(
    # Jeśli podasz -Version "1.0.8", skrypt nie pyta i robi release od razu.
    [string]$Version = "",

    [string]$ClientProjDir  = "C:\LanChatClient\LanChatClient",
    [string]$UpdaterProjDir = "C:\LanChatClient\LanChatUpdater",
    [string]$UpdatesDir     = "C:\LanChatServer\Updates",
    [string]$PublicBaseUrl  = "http://192.168.64.233:5001/updates",
    [string]$LogsDir        = "C:\LanChatServer\release_logs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Dir($p) { if (!(Test-Path $p)) { New-Item -ItemType Directory -Path $p | Out-Null } }

function Write-Step($msg) {
    $line = "`n=== $msg ==="
    Write-Host $line
    Add-Content -Path $script:LogFile -Value $line
}

function Log($msg) {
    Write-Host $msg
    Add-Content -Path $script:LogFile -Value $msg
}

function Fail($msg) {
    Write-Step "BLOCKED"
    Log $msg
    throw $msg
}

function Read-JsonFile($path) {
    if (!(Test-Path $path)) { return $null }
    try { return (Get-Content $path -Raw | ConvertFrom-Json) } catch { return $null }
}

function Get-CsprojVersion($csprojPath) {
    if (!(Test-Path $csprojPath)) { return $null }
    $raw = Get-Content $csprojPath -Raw
    $m = [regex]::Match($raw, '<Version>\s*([^<]+)\s*</Version>')
    if ($m.Success) { return $m.Groups[1].Value.Trim() }
    return $null
}

function Parse-Version3($v) {
    $v = ($v ?? "").Trim()
    if ($v -eq "") { return $null }

    $v = ($v -split '-')[0]

    $parts = $v.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Count -lt 2) { return $null }

    $maj = [int]$parts[0]
    $min = [int]$parts[1]
    $pat = 0
    if ($parts.Count -ge 3) { $pat = [int]$parts[2] }

    return [pscustomobject]@{ Major=$maj; Minor=$min; Patch=$pat; Text=("{0}.{1}.{2}" -f $maj,$min,$pat) }
}

function Next-Patch($v) {
    $pv = Parse-Version3 $v
    if ($null -eq $pv) { throw "Nie umiem sparsować wersji bazowej: '$v'" }
    return ("{0}.{1}.{2}" -f $pv.Major, $pv.Minor, ($pv.Patch + 1))
}

function Set-CsprojVersion($csprojPath, $ver) {
    if (!(Test-Path $csprojPath)) { throw "Brak csproj: $csprojPath" }

    $xml = Get-Content $csprojPath -Raw
    $asm = "$ver.0"

    $xml2 = $xml
    $xml2 = [regex]::Replace($xml2, '<Version>.*?</Version>', "<Version>$ver</Version>")
    $xml2 = [regex]::Replace($xml2, '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$asm</AssemblyVersion>")
    $xml2 = [regex]::Replace($xml2, '<FileVersion>.*?</FileVersion>', "<FileVersion>$asm</FileVersion>")

    if ($xml2 -eq $xml) {
        throw "Nie udało się podmienić wersji w csproj (brak tagów Version/AssemblyVersion/FileVersion?) : $csprojPath"
    }

    Set-Content -Path $csprojPath -Value $xml2 -Encoding UTF8
}

function Get-PublishDir($projDir) {
    return Join-Path $projDir "bin\Release\net8.0-windows\win-x64\publish"
}

function Run-Publish($projDir) {
    Write-Step "dotnet publish: $projDir"
    Push-Location $projDir
    try {
        & dotnet publish -c Release -r win-x64 --self-contained true
    }
    finally {
        Pop-Location
    }
}

function Get-ExeProductVersion($exePath) {
    if (!(Test-Path $exePath)) { throw "Brak EXE: $exePath" }
    return (Get-Item $exePath).VersionInfo.ProductVersion
}

function Http-GetText($url, $timeoutSec = 5) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec $timeoutSec
        return [pscustomobject]@{ Ok=$true; Status=$r.StatusCode; Text=($r.Content ?? ""); Headers=$r.Headers }
    } catch {
        return [pscustomobject]@{ Ok=$false; Status=$null; Text=$_.Exception.Message; Headers=$null }
    }
}

function Http-Head($url, $timeoutSec = 5) {
    try {
        $r = Invoke-WebRequest -Uri $url -Method Head -UseBasicParsing -TimeoutSec $timeoutSec
        return [pscustomobject]@{ Ok=$true; Status=$r.StatusCode; Headers=$r.Headers; Error=$null }
    } catch {
        # czasem Kestrel/StaticFiles nie lubi HEAD → bywa 405
        $msg = $_.Exception.Message
        return [pscustomobject]@{ Ok=$false; Status=$null; Headers=$null; Error=$msg }
    }
}

function Preflight-Server() {
    Write-Step "PREFLIGHT: blokada bezpieczeństwa"

    # 0) Port lokalny
    $tnc = $null
    try { $tnc = Test-NetConnection -ComputerName "127.0.0.1" -Port 5001 -WarningAction SilentlyContinue } catch { }
    if ($null -eq $tnc -or $tnc.TcpTestSucceeded -ne $true) {
        Fail "Preflight: Port 5001 na localhost NIE odpowiada. Uruchom serwer: C:\LanChatServer> dotnet run"
    }
    Log "Preflight: Port 5001 OK (localhost)."

    # 1) GET /
    $root = Http-GetText "http://127.0.0.1:5001/" 5
    if (-not $root.Ok -or $root.Status -ne 200) {
        Fail ("Preflight: GET / nie powiodło się. Status={0} Msg={1}" -f $root.Status, $root.Text)
    }
    if ($root.Text -notmatch "LanChatServer OK") {
        Fail ("Preflight: GET / ma 200, ale treść nie zawiera 'LanChatServer OK'. Treść(krótko)='{0}'" -f ($root.Text.Substring(0, [Math]::Min(120, $root.Text.Length))))
    }
    Log "Preflight: GET / OK."

    # 2) GET /updates/version.json
    $vjUrl = "http://127.0.0.1:5001/updates/version.json"
    $vj = Http-GetText $vjUrl 5
    if (-not $vj.Ok -or $vj.Status -ne 200) {
        Fail ("Preflight: GET version.json nie powiodło się. Status={0} Msg={1}" -f $vj.Status, $vj.Text)
    }

    $parsed = $null
    try { $parsed = ($vj.Text | ConvertFrom-Json) } catch { }
    if ($null -eq $parsed -or -not $parsed.version -or -not $parsed.zipUrl -or -not $parsed.sha256 -or -not $parsed.exeName) {
        Fail ("Preflight: version.json nie jest poprawny JSON albo brakuje pól (version/zipUrl/sha256/exeName). Tekst='{0}'" -f ($vj.Text.Substring(0, [Math]::Min(200, $vj.Text.Length))))
    }
    Log ("Preflight: version.json OK (version={0})." -f $parsed.version)

    # 3) HEAD/GET zip (bez pobierania)
    $zipUrl = "http://127.0.0.1:5001/updates/LanChatClient.zip"

    $head = Http-Head $zipUrl 5
    if ($head.Ok -and $head.Status -eq 200) {
        Log "Preflight: HEAD LanChatClient.zip OK."
    }
    else {
        # fallback: spróbuj GET z Range 0-0 (minimalny transfer)
        try {
            $r = Invoke-WebRequest -Uri $zipUrl -Headers @{ Range = "bytes=0-0" } -UseBasicParsing -TimeoutSec 5
            # Range zwykle daje 206
            if ($r.StatusCode -ne 206 -and $r.StatusCode -ne 200) {
                Fail ("Preflight: ZIP nie odpowiada prawidłowo. Status={0}" -f $r.StatusCode)
            }
            Log ("Preflight: ZIP OK (fallback Range), Status={0}." -f $r.StatusCode)
        } catch {
            Fail ("Preflight: Nie mogę potwierdzić dostępności ZIP. Błąd: {0}" -f $_.Exception.Message)
        }
    }

    # 4) Ostrzeżenie jeśli PublicBaseUrl różni się hostem od 127.0.0.1 (to normalne, ale loguj)
    Log ("Preflight: PublicBaseUrl={0}" -f $PublicBaseUrl)
    Log "Preflight: OK. Można robić release."
}

# ==== START ====

Ensure-Dir $LogsDir
$ts = Get-Date -Format "yyyyMMdd_HHmmss"
$script:LogFile = Join-Path $LogsDir ("release_{0}.log" -f $ts)

try { Start-Transcript -Path (Join-Path $LogsDir ("release_{0}_transcript.log" -f $ts)) -Force | Out-Null } catch { }

try {
    Write-Step "START release"
    Log ("ClientProjDir  = {0}" -f $ClientProjDir)
    Log ("UpdaterProjDir = {0}" -f $UpdaterProjDir)
    Log ("UpdatesDir     = {0}" -f $UpdatesDir)
    Log ("PublicBaseUrl  = {0}" -f $PublicBaseUrl)
    Log ("LogFile        = {0}" -f $script:LogFile)

    Ensure-Dir $UpdatesDir

    # ===== BLOKADA =====
    Preflight-Server
    # ===================

    $clientCsproj  = Join-Path $ClientProjDir "LanChatClient.csproj"
    $updaterCsproj = Join-Path $UpdaterProjDir "LanChatUpdater.csproj"
    $versionJsonPath = Join-Path $UpdatesDir "version.json"

    $base = $null
    $vjLocal = Read-JsonFile $versionJsonPath
    if ($vjLocal -and $vjLocal.version) {
        $base = $vjLocal.version
        Log ("Base version from version.json: {0}" -f $base)
    } else {
        $cs = Get-CsprojVersion $clientCsproj
        if ($cs) {
            $base = $cs
            Log ("Base version from LanChatClient.csproj: {0}" -f $base)
        }
    }

    if ($Version.Trim() -eq "") {
        Write-Step "Wybór wersji"
        if ($base) {
            $suggest = Next-Patch $base
            Log ("Suggested (auto +1 patch): {0}" -f $suggest)
            Write-Host ""
            Write-Host ("Aktualna wersja bazowa: {0}" -f $base)
            Write-Host ("Proponowana automatycznie: {0}" -f $suggest)
            Write-Host ""
            $ans = Read-Host "Wpisz wersję ręcznie (np. 1.0.8) albo ENTER żeby użyć automatycznej"
            if (($ans ?? "").Trim() -ne "") { $Version = $ans.Trim() } else { $Version = $suggest }
        } else {
            $ans = Read-Host "Nie znalazłem wersji bazowej. Wpisz wersję ręcznie (np. 1.0.8)"
            if (($ans ?? "").Trim() -eq "") { throw "Nie podano wersji." }
            $Version = $ans.Trim()
        }
    }

    $pv = Parse-Version3 $Version
    if ($null -eq $pv) { throw "Nieprawidłowa wersja: '$Version' (oczekuję np. 1.0.8)" }
    $Version = $pv.Text

    Write-Step ("Wybrana wersja: {0}" -f $Version)

    Write-Step "1) Ustawiam wersję w csproj (klient + updater)"
    Set-CsprojVersion $clientCsproj  $Version
    Set-CsprojVersion $updaterCsproj $Version

    Write-Step "2) Publikuję klienta i updatera"
    Run-Publish $ClientProjDir
    Run-Publish $UpdaterProjDir

    $clientPub  = Get-PublishDir $ClientProjDir
    $updaterPub = Get-PublishDir $UpdaterProjDir

    $clientExe  = Join-Path $clientPub  "LanChatClient.exe"
    $updaterExe = Join-Path $updaterPub "LanChatUpdater.exe"

    Write-Step "3) Kontrola wersji EXE w publish"
    $clientVer  = Get-ExeProductVersion $clientExe
    $updaterVer = Get-ExeProductVersion $updaterExe
    Log ("Client publish ProductVersion : {0}" -f $clientVer)
    Log ("Updater publish ProductVersion: {0}" -f $updaterVer)

    if ($clientVer -ne $Version) {
        throw "Client EXE ma ProductVersion=$clientVer, a oczekiwano $Version."
    }

    Write-Step "4) Składam ZIP (client + LanChatUpdater.*)"
    $stage = Join-Path $env:TEMP ("LanChatPkg_" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $stage | Out-Null

    try {
        Copy-Item "$clientPub\*" $stage -Recurse -Force
        Copy-Item "$updaterPub\LanChatUpdater.*" $stage -Force

        $zip = Join-Path $UpdatesDir "LanChatClient.zip"
        if (Test-Path $zip) { Remove-Item $zip -Force }
        Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -Force

        Write-Step "5) Liczę SHA256 ZIP"
        $sha = (Get-FileHash $zip -Algorithm SHA256).Hash
        Log ("SHA256={0}" -f $sha)
        $zi = Get-Item $zip | Select-Object FullName, Length, LastWriteTime
        Log ("ZIP: {0} | {1} bytes | {2}" -f $zi.FullName, $zi.Length, $zi.LastWriteTime)

        Write-Step "6) Aktualizuję version.json"
        $jsonObj = [ordered]@{
            version = $Version
            zipUrl  = "$PublicBaseUrl/LanChatClient.zip"
            sha256  = $sha
            exeName = "LanChatClient.exe"
        }
        ($jsonObj | ConvertTo-Json -Depth 3) | Set-Content -Path $versionJsonPath -Encoding UTF8
        Log ("Wrote: {0}" -f $versionJsonPath)

        Write-Step "7) Kontrola: wersja klienta w ZIP = version.json"
        $checkDir = Join-Path $env:TEMP ("ZipCheck_" + [guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $checkDir | Out-Null
        try {
            Expand-Archive -Path $zip -DestinationPath $checkDir -Force
            $zipClientExe = Join-Path $checkDir "LanChatClient.exe"
            $zipVer = Get-ExeProductVersion $zipClientExe
            Log ("ZIP Client ProductVersion: {0}" -f $zipVer)

            if ($zipVer -ne $Version) {
                throw "ZIP zawiera klienta $zipVer, ale version.json mówi $Version. Przerywam."
            }
        }
        finally {
            Remove-Item $checkDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    finally {
        Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Step "GOTOWE"
    Log ("version.json: {0}/version.json" -f $PublicBaseUrl)
    Log ("zip        : {0}/LanChatClient.zip" -f $PublicBaseUrl)
    Log ("Log saved  : {0}" -f $script:LogFile)
}
catch {
    Write-Step "ERROR"
    Log $_.Exception.ToString()
    throw
}
finally {
    try { Stop-Transcript | Out-Null } catch { }
}
