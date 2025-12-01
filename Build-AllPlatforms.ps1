# Build Intune Wrapper Tool for All Platforms

Write-Host "🌍 Building Cross-Platform Intune Wrapper Tool..." -ForegroundColor Cyan

$projectPath = "C:\Users\gavishofir\source\IntuneWrapperTool"
Set-Location $projectPath

Write-Host ""
Write-Host "Building for Windows..." -ForegroundColor Yellow
dotnet publish -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained
Write-Host "✅ Windows build complete: bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\" -ForegroundColor Green

Write-Host ""
Write-Host "Building for macOS (Intel)..." -ForegroundColor Yellow
dotnet publish -c Release -f net8.0-maccatalyst -r maccatalyst-x64 --self-contained
Write-Host "✅ macOS Intel build complete: bin\Release\net8.0-maccatalyst\maccatalyst-x64\publish\" -ForegroundColor Green

Write-Host ""
Write-Host "Building for macOS (Apple Silicon)..." -ForegroundColor Yellow
dotnet publish -c Release -f net8.0-maccatalyst -r maccatalyst-arm64 --self-contained
Write-Host "✅ macOS ARM build complete: bin\Release\net8.0-maccatalyst\maccatalyst-arm64\publish\" -ForegroundColor Green

Write-Host ""
Write-Host "🎉 All builds complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Distribution packages:" -ForegroundColor Cyan
Write-Host "  Windows: Zip the win-x64\publish folder"
Write-Host "  macOS:   Create .app bundle from maccatalyst publish folders"
Write-Host ""
