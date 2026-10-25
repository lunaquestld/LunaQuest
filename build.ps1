# Compile LunaQuest.exe avec le compilateur C# fourni avec Windows (.NET Framework 4.x)
# puis prépare dist\LunaQuest.zip (exe + tutoriel) à partager.
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"
$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$res = "$PSScriptRoot\res"
$bin = "$PSScriptRoot\bin"
New-Item -ItemType Directory -Force $res, $bin | Out-Null

# ---- Icône : une lune sur fond nuit, enregistrée en .ico (PNG embarqués).
function New-MoonPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = "AntiAlias"
    $s = $size / 256.0
    $bg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = 56 * $s; $d = 2 * $r
    $bg.AddArc(0, 0, $d, $d, 180, 90); $bg.AddArc($size - $d, 0, $d, $d, 270, 90)
    $bg.AddArc($size - $d, $size - $d, $d, $d, 0, 90); $bg.AddArc(0, $size - $d, $d, $d, 90, 90); $bg.CloseFigure()
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Rectangle 0, 0, $size, $size), ([System.Drawing.Color]::FromArgb(8, 13, 36)), ([System.Drawing.Color]::FromArgb(28, 48, 110)), 70
    $g.FillPath($grad, $bg)
    $rnd = New-Object System.Random 3
    for ($i = 0; $i -lt 26; $i++) {
        $x = $rnd.NextDouble() * $size; $y = $rnd.NextDouble() * $size; $st = (1 + $rnd.NextDouble() * 3) * $s
        $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(110 + $rnd.Next(140), 255, 255, 255))), $x, $y, $st, $st)
    }
    $cx = 128 * $s; $cy = 128 * $s; $mr = 78 * $s
    $halo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $halo.AddEllipse($cx - $mr * 1.6, $cy - $mr * 1.6, $mr * 3.2, $mr * 3.2)
    $hb = New-Object System.Drawing.Drawing2D.PathGradientBrush $halo
    $hb.CenterColor = [System.Drawing.Color]::FromArgb(90, 150, 186, 255); $hb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 150, 186, 255))
    $g.FillPath($hb, $halo)
    $moon = New-Object System.Drawing.Drawing2D.GraphicsPath
    $moon.AddEllipse($cx - $mr, $cy - $mr, 2 * $mr, 2 * $mr)
    $mb = New-Object System.Drawing.Drawing2D.PathGradientBrush $moon
    $mb.CenterPoint = New-Object System.Drawing.PointF ($cx - $mr * 0.35), ($cy - $mr * 0.35)
    $mb.CenterColor = [System.Drawing.Color]::FromArgb(252, 253, 255); $mb.SurroundColors = @([System.Drawing.Color]::FromArgb(176, 194, 232))
    $g.FillPath($mb, $moon)
    $g.SetClip($moon)
    $crater = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45, 78, 98, 150))
    foreach ($c in @(@(-0.36, 0.12, 0.27), @(0.26, -0.40, 0.17), @(0.08, 0.46, 0.14), @(-0.06, -0.16, 0.09))) {
        $cr = $c[2] * $mr; $g.FillEllipse($crater, $cx + $c[0] * $mr - $cr, $cy + $c[1] * $mr - $cr, 2 * $cr, 2 * $cr)
    }
    $sh = New-Object System.Drawing.Drawing2D.GraphicsPath
    $sh.AddEllipse($cx - $mr * 0.35, $cy - $mr * 1.3, $mr * 2.6, $mr * 2.6)
    $sb = New-Object System.Drawing.Drawing2D.PathGradientBrush $sh
    $sb.CenterColor = [System.Drawing.Color]::FromArgb(165, 10, 18, 52); $sb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 10, 18, 52))
    $sb.FocusScales = New-Object System.Drawing.PointF 0.55, 0.55
    $g.FillPath($sb, $sh)
    $g.ResetClip()
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

$ico = "$res\LunaQuest.ico"
$sizes = 256, 64, 48, 32, 16
$pngs = foreach ($sz in $sizes) { , (New-MoonPng $sz) }
$fs = [System.IO.File]::Create($ico)
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32); $w.Write([UInt32]$pngs[$i].Length); $w.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $w.Write($p) }
$w.Close()

# ---- Version publique (affichée dans l'installateur et dans « Applications installées »).
$version = "0.4.0"
$versionCs = "$bin\Version.cs"
Set-Content -Encoding utf8 $versionCs "[assembly: System.Reflection.AssemblyVersion(""$version.0"")]`r`n[assembly: System.Reflection.AssemblyFileVersion(""$version.0"")]"

# ---- Compilation.
# x64 obligatoire : l'écran PC dans le casque charge l'openvr_api.dll 64 bits de SteamVR.
# Bluetooth (base stations) : API Windows (WinRT), décrites par les fichiers WinMetadata de Windows 10/11.
$winmd = "$env:WINDIR\System32\WinMetadata"
# Traductions (anglais, chinois, japonais) embarquées dans l'exe ; chemin relatif : csc refuse les espaces dans /resource.
Copy-Item "$res\lang.tsv" "$bin\lang.tsv" -Force
Push-Location $bin
& "$fw\csc.exe" /nologo /target:winexe /optimize+ /platform:x64 /win32icon:"$ico" /out:"$bin\LunaQuest.exe" /resource:lang.tsv `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll /r:System.Web.Extensions.dll `
    /r:"$fw\System.IO.Compression.dll" /r:"$fw\System.IO.Compression.FileSystem.dll" `
    /r:"$fw\System.Runtime.dll" /r:"$fw\System.Runtime.WindowsRuntime.dll" /r:"$fw\System.Runtime.InteropServices.WindowsRuntime.dll" `
    /r:"$winmd\Windows.Foundation.winmd" /r:"$winmd\Windows.Devices.winmd" /r:"$winmd\Windows.Storage.winmd" `
    "$PSScriptRoot\src\LunaQuest.cs" "$PSScriptRoot\src\DesktopViewer.cs" "$PSScriptRoot\src\BaseStations.cs" "$PSScriptRoot\src\AlvrLink.cs" `
    "$PSScriptRoot\src\Lang.cs" "$PSScriptRoot\src\Intro.cs" "$PSScriptRoot\src\openvr_api.cs" "$versionCs"
$appExit = $LASTEXITCODE
Pop-Location
if ($appExit -ne 0) { throw "La compilation a échoué." }
Copy-Item "$PSScriptRoot\Tutoriel.html" "$bin\Tutoriel.html" -Force -ErrorAction SilentlyContinue

# ---- Fichiers installés : l'exe, le tutoriel, l'appli du casque et les licences (pas d'ADB ni de réglages perso).
# Préparés dans un dossier temporaire à part, pour ne jamais effacer un dossier où l'appli est utilisée.
$dist = "$PSScriptRoot\dist"
$stage = "$dist\.zip-stage\LunaQuest"
if (Test-Path "$dist\.zip-stage") { Remove-Item "$dist\.zip-stage" -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item "$bin\LunaQuest.exe" $stage
if (Test-Path "$PSScriptRoot\Tutoriel.html") { Copy-Item "$PSScriptRoot\Tutoriel.html" $stage }
Copy-Item "$res\THIRD-PARTY-NOTICES.txt" $stage
if (Test-Path "$PSScriptRoot\LICENSE") { Copy-Item "$PSScriptRoot\LICENSE" "$stage\LICENSE.txt" }
# Client ALVR au thème LunaQuest, s'il a été compilé (voir Documents\LunaQuest-ALVR).
$lunaClient = "$env:USERPROFILE\Documents\LunaQuest-ALVR\ALVR\build\alvr_client_android\alvr_client_android.apk"
if (Test-Path $lunaClient) { Copy-Item $lunaClient "$stage\LunaQuest-Client.apk" }
# Partie PC d'ALVR compilée pour LunaQuest (modes FBE), avec une configuration de départ neutre.
$alvrRepo = "$env:USERPROFILE\Documents\LunaQuest-ALVR\ALVR"
$streamer = "$alvrRepo\build\alvr_streamer_windows"
if (Test-Path "$streamer\bin\win64\driver_alvr_server.dll") {
    $alvr = "$stage\ALVR"
    New-Item -ItemType Directory -Force "$alvr\bin\win64", "$alvr\licenses" | Out-Null
    Copy-Item "$streamer\ALVR Dashboard.exe", "$streamer\driver.vrdrivermanifest" $alvr
    Copy-Item "$streamer\bin\win64\driver_alvr_server.dll", "$streamer\bin\win64\openvr_api.dll", "$streamer\bin\win64\vcruntime140_1.dll" "$alvr\bin\win64"
    Copy-Item "$alvrRepo\LICENSE" "$alvr\licenses\ALVR.txt"
    Copy-Item "$alvrRepo\openvr\LICENSE" "$alvr\licenses\Valve.txt"
    Copy-Item "$res\alvr-session-default.json" "$alvr\session.json"
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payload = "$dist\.zip-stage\payload.zip"
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $payload, [System.IO.Compression.CompressionLevel]::Optimal, $false)

# ---- Installateur : les fichiers ci-dessus sont embarqués dans LunaQuest-Setup.exe.
# (chemin relatif : csc n'accepte pas d'espaces dans /resource, et le dossier du projet en contient)
Push-Location "$dist\.zip-stage"
& "$fw\csc.exe" /nologo /target:winexe /optimize+ /win32icon:"$ico" /out:"$bin\LunaQuest-Setup.exe" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Web.Extensions.dll `
    /r:"$fw\System.IO.Compression.dll" /r:"$fw\System.IO.Compression.FileSystem.dll" /resource:payload.zip `
    "$PSScriptRoot\src\Setup.cs" "$versionCs"
$setupExit = $LASTEXITCODE
Pop-Location
if ($setupExit -ne 0) { throw "La compilation de l'installateur a échoué." }

# ---- Archive à partager : seulement l'installateur.
$setupStage = "$dist\.zip-stage\setup"
New-Item -ItemType Directory -Force $setupStage | Out-Null
Copy-Item "$bin\LunaQuest-Setup.exe" $setupStage
$zip = "$dist\LunaQuest-$version-Setup.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($setupStage, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
if (Test-Path "$dist\LunaQuest.zip") { Remove-Item "$dist\LunaQuest.zip" -Force }
Write-Output "OK : $bin\LunaQuest.exe"
Write-Output "OK : $bin\LunaQuest-Setup.exe"
Write-Output "OK : $zip"
