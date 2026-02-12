# Build portable single-file executable for TopMonitoring v1.2.0
# Usage: .\build-portable.ps1

$projectPath = ".\src\TopMonitoring.App"
$configuration = "Release"
$runtimeId = "win-x64"

Write-Host "Building TopMonitoring v1.2.0 portable executable..." -ForegroundColor Green

dotnet publish `
  -p:Configuration=$configuration `
  -p:RuntimeIdentifier=$runtimeId `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishTrimmed=false `
  $projectPath

if ($LASTEXITCODE -eq 0) {
    $publishPath = ".\src\TopMonitoring.App\bin\$configuration\net8.0-windows\$runtimeId\publish\TopMonitoring.exe"
    Write-Host "✓ Build successful!" -ForegroundColor Green
    Write-Host "✓ Portable EXE created at: $publishPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "File size: $((Get-Item $publishPath).Length / 1MB)MB" -ForegroundColor Cyan
} else {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
