using System.Diagnostics;
using System.Text;

namespace IntuneWrapperTool;

public partial class MainPage : ContentPage
{
    private StringBuilder logBuilder = new StringBuilder();
    
    // Platform-specific wrapper tool paths
    private string GetWrapperToolPath()
    {
        if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
        {
            // macOS: Check common installation locations
            var possiblePaths = new[]
            {
                "/Applications/IntuneMAMPackager/IntuneMAMPackager",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "IntuneWrapper/IntuneMAMPackager"),
                "/usr/local/bin/IntuneMAMPackager"
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            return "/Applications/IntuneMAMPackager/IntuneMAMPackager";
        }
        else if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return @"C:\IntuneWrapper\IntuneMAMPackager\IntuneMAMPackager.exe";
        }
        
        return string.Empty;
    }

    public MainPage()
    {
        InitializeComponent();
        UpdatePlatformInfo();
    }

    private void UpdatePlatformInfo()
    {
        var platform = DeviceInfo.Platform.ToString();
        var canWrap = DeviceInfo.Platform == DevicePlatform.macOS || 
                      DeviceInfo.Platform == DevicePlatform.MacCatalyst;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!canWrap)
            {
                LogMessage($"⚠️ Platform: {platform}");
                LogMessage("⚠️ Note: Actual iOS app wrapping requires macOS with Xcode");
                LogMessage("⚠️ This tool can prepare files and test on Windows/other platforms");
                LogMessage("");
            }
            else
            {
                LogMessage($"✅ Platform: macOS - Ready for iOS app wrapping!");
                LogMessage("");
            }
        });
    }

    private async void OnBrowseInputIpa(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".ipa" } },
                { DevicePlatform.macOS, new[] { "ipa" } },
                { DevicePlatform.MacCatalyst, new[] { "ipa" } },
                { DevicePlatform.iOS, new[] { "public.archive" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select iOS IPA File",
                FileTypes = customFileType
            });

            if (result != null)
            {
                InputIpaPath.Text = result.FullPath;
                
                // Auto-suggest output path
                if (string.IsNullOrEmpty(OutputIpaPath.Text))
                {
                    var directory = Path.GetDirectoryName(result.FullPath);
                    var filename = Path.GetFileNameWithoutExtension(result.FullPath);
                    OutputIpaPath.Text = Path.Combine(directory ?? "", $"{filename}-wrapped.ipa");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select file: {ex.Message}", "OK");
        }
    }

    private async void OnBrowseProvisioningProfile(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".mobileprovision" } },
                { DevicePlatform.macOS, new[] { "mobileprovision" } },
                { DevicePlatform.MacCatalyst, new[] { "mobileprovision" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select Provisioning Profile",
                FileTypes = customFileType
            });

            if (result != null)
            {
                ProvisioningProfilePath.Text = result.FullPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select file: {ex.Message}", "OK");
        }
    }

    private async void OnBrowseOutputIpa(object sender, EventArgs e)
    {
        try
        {
            var directory = string.Empty;
            if (!string.IsNullOrEmpty(InputIpaPath.Text))
            {
                directory = Path.GetDirectoryName(InputIpaPath.Text);
            }
            else
            {
                directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var filename = "wrapped-app.ipa";
            if (!string.IsNullOrEmpty(InputIpaPath.Text))
            {
                filename = Path.GetFileNameWithoutExtension(InputIpaPath.Text) + "-wrapped.ipa";
            }

            OutputIpaPath.Text = Path.Combine(directory ?? "", filename);
            
            await DisplayAlert("Output Path", $"Output will be saved to:\n{OutputIpaPath.Text}\n\nYou can edit this path manually.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to set output location: {ex.Message}", "OK");
        }
    }

    private async void OnWrapApp(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(InputIpaPath.Text))
        {
            await DisplayAlert("Validation Error", "Please select an input IPA file", "OK");
            return;
        }

        if (!File.Exists(InputIpaPath.Text))
        {
            await DisplayAlert("Validation Error", "Input IPA file does not exist", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ProvisioningProfilePath.Text))
        {
            await DisplayAlert("Validation Error", "Please select a provisioning profile", "OK");
            return;
        }

        if (!File.Exists(ProvisioningProfilePath.Text))
        {
            await DisplayAlert("Validation Error", "Provisioning profile does not exist", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputIpaPath.Text))
        {
            await DisplayAlert("Validation Error", "Please specify an output location", "OK");
            return;
        }

        // Check platform compatibility
        if (DeviceInfo.Platform != DevicePlatform.macOS && DeviceInfo.Platform != DevicePlatform.MacCatalyst)
        {
            var proceed = await DisplayAlert(
                "Platform Limitation",
                $"You are running on {DeviceInfo.Platform}.\n\n" +
                "iOS app wrapping requires macOS with Xcode installed.\n\n" +
                "This tool can validate your files and prepare the command, but cannot perform actual wrapping.\n\n" +
                "Continue anyway?",
                "Yes", "No");
            
            if (!proceed)
                return;
        }

        // Check if wrapper tool exists
        var wrapperPath = GetWrapperToolPath();
        if (!File.Exists(wrapperPath))
        {
            var download = await DisplayAlert(
                "Wrapper Tool Not Found",
                $"The Intune App Wrapping Tool is not found at:\n{wrapperPath}\n\n" +
                "Would you like instructions to download it?",
                "Yes", "No");

            if (download)
            {
                var instructions = DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst
                    ? "macOS Instructions:\n" +
                      "1. Visit: https://github.com/msintuneappsdk/intune-app-wrapping-tool-ios\n" +
                      "2. Download the latest release (v20.8.0 or higher)\n" +
                      "3. Extract to /Applications/IntuneMAMPackager/\n" +
                      "4. Make executable: chmod +x /Applications/IntuneMAMPackager/IntuneMAMPackager"
                    : "Windows Instructions:\n" +
                      "1. Visit: https://github.com/msintuneappsdk/intune-app-wrapping-tool-ios\n" +
                      "2. Note: Wrapping requires macOS - use this tool to prepare files\n" +
                      "3. Transfer files to a Mac or use CI/CD (GitHub Actions)";

                await DisplayAlert("Download Instructions", instructions, "OK");
            }
            
            if (DeviceInfo.Platform != DevicePlatform.macOS && DeviceInfo.Platform != DevicePlatform.MacCatalyst)
            {
                await ShowPreparedCommand();
                return;
            }
            
            return;
        }

        // Start wrapping process
        await WrapAppAsync();
    }

    private async Task ShowPreparedCommand()
    {
        var command = $"IntuneMAMPackager -i \"{InputIpaPath.Text}\" -o \"{OutputIpaPath.Text}\" -p \"{ProvisioningProfilePath.Text}\"";
        
        if (!string.IsNullOrWhiteSpace(SigningIdentity.Text))
        {
            command += $" -c \"{SigningIdentity.Text}\"";
        }
        
        if (VerboseLogging.IsChecked)
        {
            command += " -v";
        }

        LogMessage("📋 Prepared Command for macOS:");
        LogMessage("");
        LogMessage(command);
        LogMessage("");
        LogMessage("✅ Copy this command and run it on a Mac with Xcode installed");

        await DisplayAlert("Command Prepared", 
            "The wrapping command has been prepared and shown in the log.\n\n" +
            "Copy this command and run it on a macOS machine with Xcode.", 
            "OK");
    }

    private async Task WrapAppAsync()
    {
        try
        {
            ProgressSection.IsVisible = true;
            LogSection.IsVisible = true;
            logBuilder.Clear();

            UpdateStatus("Starting wrapping process...", 0.1);

            var wrapperPath = GetWrapperToolPath();
            
            // Build command arguments
            var arguments = new StringBuilder();
            arguments.Append($"-i \"{InputIpaPath.Text}\" ");
            arguments.Append($"-o \"{OutputIpaPath.Text}\" ");
            arguments.Append($"-p \"{ProvisioningProfilePath.Text}\" ");

            if (!string.IsNullOrWhiteSpace(SigningIdentity.Text))
            {
                arguments.Append($"-c \"{SigningIdentity.Text}\" ");
            }

            if (VerboseLogging.IsChecked)
            {
                arguments.Append("-v ");
            }

            LogMessage($"Executing: {wrapperPath}");
            LogMessage($"Arguments: {arguments}");
            LogMessage("");

            UpdateStatus("Wrapping app with Intune wrapper...", 0.3);

            // Execute wrapper tool
            var processInfo = new ProcessStartInfo
            {
                FileName = wrapperPath,
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            process.OutputDataReceived += (s, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    MainThread.BeginInvokeOnMainThread(() => LogMessage(e.Data));
                }
            };
            
            process.ErrorDataReceived += (s, e) => 
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    MainThread.BeginInvokeOnMainThread(() => LogMessage($"ERROR: {e.Data}"));
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            UpdateStatus("Processing... Please wait", 0.5);

            await process.WaitForExitAsync();

            UpdateStatus("Wrapping completed", 0.8);

            if (process.ExitCode == 0)
            {
                LogMessage("");
                LogMessage("✅ SUCCESS! App wrapped successfully!");
                LogMessage($"Wrapped IPA: {OutputIpaPath.Text}");

                if (VerifyAfterWrap.IsChecked)
                {
                    UpdateStatus("Verifying wrapped IPA...", 0.9);
                    await VerifyWrappedIpaAsync();
                }

                UpdateStatus("✅ Complete!", 1.0);

                await DisplayAlert("Success", 
                    $"App wrapped successfully!\n\nOutput: {OutputIpaPath.Text}", 
                    "OK");
            }
            else
            {
                LogMessage("");
                LogMessage($"❌ FAILED! Exit code: {process.ExitCode}");
                UpdateStatus("❌ Wrapping failed", 0);

                await DisplayAlert("Error", 
                    $"Wrapping failed with exit code {process.ExitCode}. Check the log for details.", 
                    "OK");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"EXCEPTION: {ex.Message}");
            UpdateStatus("❌ Error occurred", 0);

            await DisplayAlert("Error", 
                $"An error occurred: {ex.Message}", 
                "OK");
        }
    }

    private async Task VerifyWrappedIpaAsync()
    {
        try
        {
            LogMessage("");
            LogMessage("Verifying wrapped IPA...");

            if (File.Exists(OutputIpaPath.Text))
            {
                var fileInfo = new FileInfo(OutputIpaPath.Text);
                LogMessage($"✅ File exists: {fileInfo.Length:N0} bytes");
                
                var originalSize = new FileInfo(InputIpaPath.Text).Length;
                LogMessage($"Original size: {originalSize:N0} bytes");
                LogMessage($"Wrapped size: {fileInfo.Length:N0} bytes");
                
                if (fileInfo.Length > originalSize)
                {
                    LogMessage("✅ Size increased - wrapper likely applied successfully");
                }
                
                LogMessage("✅ Verification passed!");
            }
            else
            {
                LogMessage("❌ Output file not found!");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Verification error: {ex.Message}");
        }
    }

    private void UpdateStatus(string message, double progress)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
            ProgressBar.Progress = progress;
        });
    }

    private void LogMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            logBuilder.AppendLine(message);
            LogOutput.Text = logBuilder.ToString();
        });
    }

    private void OnReset(object sender, EventArgs e)
    {
        InputIpaPath.Text = string.Empty;
        ProvisioningProfilePath.Text = string.Empty;
        SigningIdentity.Text = string.Empty;
        OutputIpaPath.Text = string.Empty;
        VerboseLogging.IsChecked = true;
        VerifyAfterWrap.IsChecked = true;
        ProgressSection.IsVisible = false;
        LogSection.IsVisible = false;
        logBuilder.Clear();
        LogOutput.Text = string.Empty;
        StatusLabel.Text = "Ready to wrap...";
        ProgressBar.Progress = 0;
    }
}
