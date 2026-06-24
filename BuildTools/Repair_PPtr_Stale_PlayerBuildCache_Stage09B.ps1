$ErrorActionPreference = "Stop"

try {
    $ProjectPath = "C:\Universidad_de_Zaragoza\Virtual Reality\PROJECT\APP\course-project-VR-Ioan-test"
    $UnityExe = "C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Unity.exe"
    $BuildFolder = "C:\Universidad_de_Zaragoza\Virtual Reality\PROJECT\APP\APK_BUILDS\FINAL_CANDIDATE_STAGE09B_RESEARCH"
    $CandidateApk = Join-Path $BuildFolder "ForestArchery_FINAL_CANDIDATE_STAGE09B_RESEARCH.apk"
    $PatchMarker = "FOREST_ARCHERY_SKIP_MISSING_META_SAMPLE_MANIFEST"

    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host " REBUILD ONLY UNITY PLAYER CACHES - STAGE 09B" -ForegroundColor Cyan
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

    $MetaPatchFiles = @(
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

    if ($MetaPatchFiles.Count -ne 1) {
        throw "Nu am gasit in mod unic fisierul Meta XR patch-uit. Gasite: $($MetaPatchFiles.Count)"
    }

    $MetaPatchCode = [System.IO.File]::ReadAllText($MetaPatchFiles[0])

    if (-not $MetaPatchCode.Contains($PatchMarker)) {
        throw "Patch-ul Meta XR pentru manifestul lipsa nu mai este prezent. Nu continua buildul."
    }

    Write-Host "Patch-ul Meta XR este prezent." -ForegroundColor Green
    Write-Host ""

    function Remove-GeneratedFolder {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Path,

            [Parameter(Mandatory = $true)]
            [string]$Label
        )

        if (-not (Test-Path -LiteralPath $Path)) {
            Write-Host ("Nu exista: " + $Label) -ForegroundColor DarkGray
            return
        }

        Write-Host ("Sterg numai cache generat: " + $Label) -ForegroundColor Yellow
        Write-Host $Path -ForegroundColor DarkGray

        Remove-Item `
            -LiteralPath $Path `
            -Recurse `
            -Force `
            -ErrorAction Stop
    }

    Remove-GeneratedFolder `
        -Path (Join-Path $ProjectPath "Library\Bee") `
        -Label "Library\Bee"

    Remove-GeneratedFolder `
        -Path (Join-Path $ProjectPath "Library\BuildPlayerData") `
        -Label "Library\BuildPlayerData"

    Remove-GeneratedFolder `
        -Path (Join-Path $ProjectPath "Library\PlayerDataCache") `
        -Label "Library\PlayerDataCache"

    Remove-GeneratedFolder `
        -Path (Join-Path $ProjectPath "Library\ScriptAssemblies") `
        -Label "Library\ScriptAssemblies"

    Remove-GeneratedFolder `
        -Path (Join-Path $ProjectPath "Library\Il2cppBuildCache") `
        -Label "Library\Il2cppBuildCache"

    New-Item `
        -ItemType Directory `
        -Path $BuildFolder `
        -Force |
        Out-Null

    if (Test-Path -LiteralPath $CandidateApk) {
        Remove-Item `
            -LiteralPath $CandidateApk `
            -Force `
            -ErrorAction Stop
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
        throw "V: nu indica proiectul Unity."
    }

    $Drive = Get-PSDrive -Name C -ErrorAction Stop
    $FreeGB = [Math]::Round($Drive.Free / 1GB, 2)

    Write-Host ""
    Write-Host "Spatiu liber dupa curatare:" -ForegroundColor Cyan
    Write-Host ($FreeGB.ToString() + " GB") -ForegroundColor White
    Write-Host ""

    Write-Host "NU au fost atinse:" -ForegroundColor Green
    Write-Host " - Assets" -ForegroundColor White
    Write-Host " - Packages" -ForegroundColor White
    Write-Host " - ProjectSettings" -ForegroundColor White
    Write-Host " - UserSettings" -ForegroundColor White
    Write-Host " - Library\Artifacts" -ForegroundColor White
    Write-Host " - Library\ArtifactDB" -ForegroundColor White
    Write-Host " - Library\PackageCache" -ForegroundColor White
    Write-Host " - baseline-urile si APK-urile pastrate" -ForegroundColor White
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
    Write-Host "Asteapta recompilarea completa inainte de build." -ForegroundColor Yellow
}
catch {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host " CACHE REBUILD PREPARATION FAILED" -ForegroundColor Red
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
