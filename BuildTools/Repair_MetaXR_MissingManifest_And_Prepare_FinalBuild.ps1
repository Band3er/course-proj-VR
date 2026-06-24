$ErrorActionPreference = "Stop"

try {
    $ProjectPath = "C:\Universidad_de_Zaragoza\Virtual Reality\PROJECT\APP\course-project-VR-Ioan-test"
    $UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe"
    $BackupRoot = "C:\Universidad_de_Zaragoza\Virtual Reality\PROJECT\APP\course-project-VR-Ioan-test_LOCAL_BACKUPS"
    $BuildFolder = "C:\Universidad_de_Zaragoza\Virtual Reality\PROJECT\APP\APK_BUILDS\FINAL_CANDIDATE_STAGE09B_RESEARCH"
    $CandidateApk = Join-Path $BuildFolder "ForestArchery_FINAL_CANDIDATE_STAGE09B_RESEARCH.apk"
    $Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host " REPAIR META XR MISSING MANIFEST BUILD FAILURE" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""

    if (Get-Process -Name Unity -ErrorAction SilentlyContinue) {
        throw "Unity este deschis. Salveaza scena si inchide Unity complet."
    }

    if (-not (Test-Path -LiteralPath $ProjectPath)) {
        throw "Proiectul nu exista: $ProjectPath"
    }

    if (-not (Test-Path -LiteralPath $UnityExe)) {
        throw "Unity 6000.3.6f1 nu exista aici: $UnityExe"
    }

    $PackageCacheRoot = Join-Path $ProjectPath "Library\PackageCache"

    $MetaFiles = @(
        Get-ChildItem `
            -LiteralPath $PackageCacheRoot `
            -Directory `
            -Filter "com.meta.xr.sdk.core@*" `
            -ErrorAction Stop |
        ForEach-Object {
            Join-Path `
                $_.FullName `
                "Editor\Utils\SampleMetadata\UpdateManifestWithCodeSample.cs"
        } |
        Where-Object {
            Test-Path -LiteralPath $_
        }
    )

    if ($MetaFiles.Count -ne 1) {
        throw "Ma asteptam la exact un UpdateManifestWithCodeSample.cs, dar am gasit: $($MetaFiles.Count)"
    }

    $MetaFile = $MetaFiles[0]
    $PatchMarker = "FOREST_ARCHERY_SKIP_MISSING_META_SAMPLE_MANIFEST"

    $BackupFolder = Join-Path `
        $BackupRoot `
        ("Before_MetaXR_MissingManifestRepair_" + $Timestamp)

    New-Item `
        -ItemType Directory `
        -Path $BackupFolder `
        -Force |
        Out-Null

    Copy-Item `
        -LiteralPath $MetaFile `
        -Destination (Join-Path $BackupFolder "UpdateManifestWithCodeSample.cs") `
        -Force

    Write-Host "Backup punctual creat:" -ForegroundColor Green
    Write-Host $BackupFolder -ForegroundColor White
    Write-Host ""

    $Code = [System.IO.File]::ReadAllText($MetaFile)

    if (-not $Code.Contains($PatchMarker)) {
        $Pattern = '(?m)^(?<indent>[ \t]*)(?<statement>[A-Za-z_][A-Za-z0-9_]*\.Load\(manifestPath\);)[ \t]*$'
        $Matches = [regex]::Matches($Code, $Pattern)

        if ($Matches.Count -ne 1) {
            throw "Nu am putut identifica in mod unic linia .Load(manifestPath). Gasite: $($Matches.Count)"
        }

        $Match = $Matches[0]
        $Indent = $Match.Groups["indent"].Value
        $OriginalStatement = $Match.Groups["statement"].Value

        $Replacement = @"
${Indent}// $PatchMarker
${Indent}if (!System.IO.File.Exists(manifestPath))
${Indent}{
${Indent}    UnityEngine.Debug.LogWarning(
${Indent}        "[Forest Archery Build Fix] Meta XR sample metadata manifest is missing; skipping non-essential sample metadata update: " +
${Indent}        manifestPath);
${Indent}    return;
${Indent}}

${Indent}$OriginalStatement
"@

        $PatchedCode = $Code.Remove(
            $Match.Index,
            $Match.Length
        ).Insert(
            $Match.Index,
            $Replacement.TrimEnd([char[]]"`r`n")
        )

        $MetaItem = Get-Item -LiteralPath $MetaFile -Force
        if ($MetaItem.IsReadOnly) {
            $MetaItem.IsReadOnly = $false
        }

        $Utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)

        [System.IO.File]::WriteAllText(
            $MetaFile,
            $PatchedCode,
            $Utf8WithoutBom
        )

        $Verification = [System.IO.File]::ReadAllText($MetaFile)

        if (-not $Verification.Contains($PatchMarker)) {
            throw "Patch-ul Meta XR nu a fost scris corect."
        }

        Write-Host "Meta XR build callback reparat." -ForegroundColor Green
    }
    else {
        Write-Host "Patch-ul Meta XR exista deja; nu l-am duplicat." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Fisier reparat:" -ForegroundColor Cyan
    Write-Host $MetaFile -ForegroundColor White
    Write-Host ""

    $BeeAndroid = Join-Path $ProjectPath "Library\Bee\Android"

    if (Test-Path -LiteralPath $BeeAndroid) {
        Write-Host "Sterg numai buildul Android/Quest esuat:" -ForegroundColor Yellow
        Write-Host $BeeAndroid -ForegroundColor White

        Remove-Item `
            -LiteralPath $BeeAndroid `
            -Recurse `
            -Force `
            -ErrorAction Stop
    }

    New-Item `
        -ItemType Directory `
        -Path $BuildFolder `
        -Force |
        Out-Null

    if (Test-Path -LiteralPath $CandidateApk) {
        Remove-Item `
            -LiteralPath $CandidateApk `
            -Force
    }

    $ExistingV = @(& subst.exe) |
        Where-Object {
            $_ -match "^\s*V:\\"
        }

    if ($ExistingV) {
        & subst.exe V: /D | Out-Null
    }

    & subst.exe V: $ProjectPath

    if ($LASTEXITCODE -ne 0) {
        throw "Nu am putut crea maparea V:."
    }

    if (-not (Test-Path -LiteralPath "V:\Assets")) {
        throw "Maparea V: nu indica proiectul Unity."
    }

    Write-Host ""
    Write-Host "V: a fost mapat la proiect pentru a evita calea de 262 caractere." -ForegroundColor Green
    Write-Host "NU elimina V: pana dupa buildul final." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "APK candidat:" -ForegroundColor Cyan
    Write-Host $CandidateApk -ForegroundColor White
    Write-Host ""

    Start-Process `
        -FilePath $UnityExe `
        -ArgumentList @(
            "-projectPath",
            "V:\"
        )

    Write-Host "Unity a fost pornit prin V:\" -ForegroundColor Green
    Write-Host ""
    Write-Host "In Unity: asteapta compilarea, valideaza Stage 09b, apoi Build And Run." -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host " REPAIR FAILED" -ForegroundColor Red
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
