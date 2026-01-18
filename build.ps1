# Steam P2P Info Modern - Build Script
# 需要以管理员身份运行

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Steam P2P Info Modern Builder" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$uiDir = "$rootDir\src\SteamP2PInfo.UI"
$appDir = "$rootDir\src\SteamP2PInfo.App"
$outputDir = "$rootDir\publish"

# Step 1: Build React UI
Write-Host "[1/3] Building React UI..." -ForegroundColor Yellow
Set-Location $uiDir

if (-not (Test-Path "node_modules")) {
    Write-Host "  Installing npm dependencies..." -ForegroundColor Gray
    npm install
}

Write-Host "  Building production bundle..." -ForegroundColor Gray
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "UI build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  UI build complete!" -ForegroundColor Green

# Step 2: Build .NET Application
Write-Host ""
Write-Host "[2/3] Building .NET Application..." -ForegroundColor Yellow
Set-Location $rootDir

dotnet publish src\SteamP2PInfo.App\SteamP2PInfo.App.csproj `
    -c Release `
    -r win-x64 `
    --no-self-contained `
    -p:PublishSingleFile=true `
    -o $outputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host ".NET build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  .NET build complete!" -ForegroundColor Green

# Step 3: Copy Steamworks binaries
Write-Host ""
Write-Host "[3/3] Copying dependencies..." -ForegroundColor Yellow

$steamworksCandidates = @(
    "$rootDir\steamworks_bin\steam_api64.dll",
    "$rootDir\src\SteamP2PInfo.App\steamworks_bin\steam_api64.dll",
    "$rootDir\src\SteamP2PInfo.Core\steamworks_bin\steam_api64.dll",
    "$rootDir\..\SteamP2PInfo-1.2.0\SteamP2PInfo\steamworks_bin\steam_api64.dll",
    "$outputDir\steam_api64.dll"
)

$steamworksDll = $steamworksCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($steamworksDll) {
    $destPath = Join-Path $outputDir "steam_api64.dll"
    if ($steamworksDll -ieq $destPath) {
        Write-Host "  steam_api64.dll already in output." -ForegroundColor Gray
    } else {
        Copy-Item $steamworksDll $outputDir -Force
        Write-Host "  Copied steam_api64.dll" -ForegroundColor Gray
    }
} else {
    Write-Host "  steam_api64.dll not found." -ForegroundColor Red
    Write-Host "  Place it in steamworks_bin\\steam_api64.dll (project root) or update build.ps1." -ForegroundColor Red
    exit 1
}

# Step 4: Clean up unnecessary files
Write-Host ""
Write-Host "[4/4] Cleaning up..." -ForegroundColor Yellow

# Remove unused architecture folders (only need amd64 for x64)
$foldersToRemove = @("arm64", "x86", "runtimes")
foreach ($folder in $foldersToRemove) {
    $folderPath = Join-Path $outputDir $folder
    if (Test-Path $folderPath) {
        Remove-Item $folderPath -Recurse -Force
        Write-Host "  Removed $folder/" -ForegroundColor Gray
    }
}

# Remove debug symbols
Get-ChildItem $outputDir -Filter "*.pdb" | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "  Removed $($_.Name)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Show final size
$totalSize = (Get-ChildItem $outputDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Output: $outputDir\SteamP2PInfo.exe" -ForegroundColor Cyan
Write-Host "Total size: $([math]::Round($totalSize, 2)) MB" -ForegroundColor Cyan
Write-Host ""

Set-Location $rootDir
