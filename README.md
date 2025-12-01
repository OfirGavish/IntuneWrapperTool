# Intune Wrapper Tool for macOS

A native macOS application to wrap iOS apps with Microsoft Intune App Wrapping Tool.

## ✅ Why macOS Only?

The Microsoft Intune App Wrapping Tool **requires macOS with Xcode** to wrap iOS apps. This is a macOS-native limitation, not a choice.

## 🚀 Quick Start

### Download
Download the latest release from GitHub Actions artifacts or releases.

### Install
1. Extract `IntuneWrapperTool-macOS.zip`
2. Copy `IntuneWrapperTool.app` to Applications folder
3. Right-click → Open (first time only)

### Use
1. Download Intune wrapper: https://github.com/msintuneappsdk/intune-app-wrapping-tool-ios
2. Extract to `/Applications/IntuneMAMPackager/`
3. Launch IntuneWrapperTool
4. Select IPA → Select provisioning profile → Wrap!

## 🛠️ Building from Source

```bash
# Clone the repository
git clone https://github.com/OfirGavish/IntuneWrapperTool.git
cd IntuneWrapperTool

# Build
dotnet build -f net8.0-maccatalyst -c Release

# Run
dotnet run -f net8.0-maccatalyst
```

## 📦 GitHub Actions

Automatically builds macOS app on every push. Download from Actions artifacts.

## ❓ What About Windows Users?

Windows users should:
1. Use GitHub Actions to wrap apps (see AzureDeploymentsApp example)
2. Use PowerShell scripts in the Intune-iOS-App repository
3. Transfer files to a Mac for wrapping

## 📅 January 19, 2026 Deadline

This tool uses Intune Wrapper v20.8.0+ to meet Microsoft''s deadline.
