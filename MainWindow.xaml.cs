using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FiveMCleaner.Models;
using FiveMCleaner.Services;

namespace FiveMCleaner;

public partial class MainWindow : Window
{
    private const string TwitchUrl = "https://www.twitch.tv/vip3r_76";
    private const string DiscordUrl = "https://discord.gg/9AxuZkyU7P";
    private const string KofiUrl = "https://ko-fi.com/vip3r76";
    private const string GitHubUrl = "https://github.com/ViP3R76/FiveM-Cleaner";
    private readonly FiveMService _fiveM;
    private readonly DispatcherTimer _statusTimer;
    private readonly DispatcherTimer _sizeTimer;
    private int _sizeUpdateRunning;
    private bool _busy;
    private bool _aboutVisible;
    private AppLanguage _language = DetectWindowsLanguage();
    private CleanupRequest? _pendingRequest;

    private sealed record CleanupRequest(string Title, IReadOnlyList<string> Targets, bool IsAll);

    public MainWindow()
    {
        InitializeComponent();
        ApplyLanguage();
        // Environment.ProcessPath liefert bei Single-File-Publish den tatsächlichen Speicherort der EXE.
        // Das ist zuverlässiger als das aktuelle Arbeitsverzeichnis.
        var executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        var executableDirectory = !string.IsNullOrWhiteSpace(executablePath)
            ? Path.GetDirectoryName(Path.GetFullPath(executablePath))
            : null;
        var workingDirectory = Environment.CurrentDirectory;
        // Zuerst wird neben der EXE gesucht; Arbeitsverzeichnis und AppContext sind nur Fallbacks.
        var resolvedRoot = FiveMService.ResolveInstallationRoot(executableDirectory, workingDirectory, AppContext.BaseDirectory);
        _fiveM = new FiveMService(resolvedRoot ?? executableDirectory ?? workingDirectory ?? AppContext.BaseDirectory);
        RootText.Text = _fiveM.Root;
        UpdateStatus(); // Initialen lokalisierten Status setzen.

        var version = GetApplicationVersion();
        FooterVersionText.Text = version;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += (_, _) => UpdateStatus();
        _sizeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _sizeTimer.Tick += async (_, _) => await UpdateSizesAsync();

        Loaded += (_, _) =>
        {
            // Aufwendige Startarbeiten werden aus dem ersten Render-Schritt herausgehalten.
            // Die rekursive Größenberechnung wird bewusst erst danach gestartet.
            UpdateStatus();
            CacheSizeText.Text = L("SizeChecking");
            LogsSizeText.Text = L("SizeChecking");
            CrashesSizeText.Text = L("SizeChecking");
            _statusTimer.Start();
            _sizeTimer.Start();
        };

        ContentRendered += async (_, _) =>
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await UpdateSizesAsync();
        };
        Closing += (_, _) => { _statusTimer.Stop(); _sizeTimer.Stop(); };
    }

    private static AppLanguage DetectWindowsLanguage()
    {
        // InstalledUICulture entspricht der in Windows eingestellten Anzeige- bzw. UI-Sprache.
        // Deutsch wird als eigene Sprache unterstützt; alle anderen
        // Windows-Sprachen verwenden automatisch Englisch.
        var windowsLanguage = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        return string.Equals(windowsLanguage, "de", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.German
            : AppLanguage.English;
    }

    private string L(string key)
    {
        var dictionary = _language == AppLanguage.German ? Localization.German : Localization.English;
        return dictionary.TryGetValue(key, out var value) ? value : key;
    }
    private void ApplyLanguage()
    {
        LblStatusHeader.Text = L("StatusHeader");
        LblInstallationHeader.Text = L("InstallationHeader");

        LblCache.Text = L("Cache");
        LblCacheDescription.Text = L("CacheDescription");
        LblLogs.Text = L("Logs");
        LblLogsDescription.Text = L("LogsDescription");
        LblCrashes.Text = L("Crashes");
        LblCrashesDescription.Text = L("CrashesDescription");

        LblOperationStatus.Text = L("Status");

        CacheButton.Content = L("CleanCache");
        LogsButton.Content = L("CleanLogs");
        CrashesButton.Content = L("CleanCrashes");
        AllButton.Content = L("CleanAll");

        ConfirmYesButton.Content = L("Yes");
        ConfirmNoButton.Content = L("No");

        LblDisclaimer.Text = L("DisclaimerHeader");
        AboutDisclaimer1.Text = L("Disclaimer1");
        AboutDisclaimer2.Text = L("Disclaimer2");
        AboutDisclaimer3.Text = L("Disclaimer3");

        LblTwitch.Text = L("Twitch");
        LblDiscord.Text = L("Discord");
        LblGitHub.Text = L("GitHub");
        LblClose.Content = L("Close");
        LanguageButtonText.Text = L("Language");

        RefreshButton.ToolTip = L("StatusRefresh");
        LogsDetailText.Text = L("LogsDetail");
        CrashesDetailText.Text = L("CrashesDetail");

        // Die erste Statusmeldung darf nach einem Sprachwechsel nicht in Deutsch stehen bleiben.
        if (!_busy && _pendingRequest is null)
            OperationMessageRun.Text = L("Ready");
    }

    private void Language_Click(object sender, RoutedEventArgs e)
    {
        _language = _language == AppLanguage.German ? AppLanguage.English : AppLanguage.German;
        ApplyLanguage();
    }

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "v1.0.0" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private void UpdateStatus()
    {
        if (_aboutVisible) return;
        bool valid = _fiveM.IsValidInstallation(out var reason);
        UpdateInstallationPanel(valid, reason);
        if (!valid)
        {
            SetStatus(false, L("InvalidInstallation"), L(reason), false, true);
            if (!_busy && _pendingRequest is null)
            {
                SetOperationMessage(L(reason), error: true);
                OperationDetailsText.Text = string.Empty;
                OperationDetailsText.Visibility = Visibility.Collapsed;
            }
            return;
        }

        bool running = _fiveM.IsFiveMRunning();
        SetStatus(running, running ? L("StatusRunning") : L("StatusNotRunning"),
            running ? L("CleanupLocked") : L("CleanupReady"),
            !running && !_busy && _pendingRequest is null, false);
    }

    private void UpdateInstallationPanel(bool valid, string reason)
    {
        var brush = FindResource(valid ? "GreenBrush" : "RedBrush") as Brush;
        InstallationDot.Fill = brush;
        InstallationText.Foreground = brush;
        InstallationText.Text = valid ? L("InstallationDetected") : reason;
        RootText.Foreground = valid ? FindResource("TextBrush") as Brush : brush;
    }

    private void SetStatus(bool running, string text, string subtext, bool enabled, bool invalidInstallation)
    {
        var brush = FindResource(running || invalidInstallation ? "RedBrush" : "GreenBrush") as Brush;
        StatusDot.Fill = brush;
        StatusText.Text = text;
        StatusSubtext.Text = subtext;
        StatusSubtext.Foreground = brush;
        SetCleanupEnabled(enabled);
    }

    private void SetCleanupEnabled(bool enabled)
    {
        if (_aboutVisible) enabled = false;
        CacheButton.IsEnabled = enabled;
        LogsButton.IsEnabled = enabled;
        CrashesButton.IsEnabled = enabled;
        AllButton.IsEnabled = enabled;
    }

    private async Task UpdateSizesAsync()
    {
        if (_busy || !_fiveM.IsValidInstallation(out _)) return;
        if (Interlocked.Exchange(ref _sizeUpdateRunning, 1) != 0) return;

        try
        {
            var cache = _fiveM.CacheTargets;
            var logs = new[] { _fiveM.LogsTarget };
            var crashes = new[] { _fiveM.CrashesTarget };
            var results = await Task.Run(() => new
            {
                Cache = _fiveM.GetSize(cache),
                Logs = _fiveM.GetSize(logs),
                Crashes = _fiveM.GetSize(crashes)
            });

            if (!_busy && !_aboutVisible)
            {
                CacheSizeText.Text = FormatMegabytes(results.Cache);
                LogsSizeText.Text = FormatMegabytes(results.Logs);
                CrashesSizeText.Text = FormatMegabytes(results.Crashes);
            }
        }
        finally
        {
            Volatile.Write(ref _sizeUpdateRunning, 0);
        }
    }

    private void SetOperationMessage(string message, bool success = false, bool error = false, bool warning = false)
    {
        OperationIconRun.Text = success ? "✓ " : error ? "✕ " : warning ? "⚠ " : "";
        OperationIconRun.Foreground = FindResource(
            success ? "GreenBrush" : error ? "RedBrush" : warning ? "YellowBrush" : "TextBrush") as Brush;
        OperationMessageRun.Text = message;
    }
    private string LocalizeServiceMessage(string key)
    {
        return key switch
        {
            "InstallationRootUnavailable" => L("InstallationRootUnavailable"),
            "FiveMExeMissing" => L("FiveMExeMissing"),
            "AppDirMissing" => L("AppDirMissing"),
            "DataDirMissing" => L("DataDirMissing"),
            "ProtectedTarget" => L("ProtectedTarget"),
            "UnauthorizedTarget" => L("UnauthorizedTarget"),
            "ReparseTarget" => L("ReparseTarget"),
            "FiveMStartedDuringCleanup" => L("FiveMStartedDuringCleanup"),
            "CleanupBlockedByRunning" => L("CleanupBlockedByRunning"),
            _ => key
        };
    }

    private static string FormatMegabytes(long bytes) => $"{bytes / 1024d / 1024d:0.0} MB";

    private static string FormatDuration(TimeSpan duration) => $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";


    private void RequestCleanup(string title, IEnumerable<string> targets)
    {
        if (_busy || _aboutVisible) return;
        if (!_fiveM.IsValidInstallation(out var reason)) { SetOperationMessage(L(reason), error: true); UpdateStatus(); return; }
        if (_fiveM.IsFiveMRunning()) { SetOperationMessage(L("RunningLocked"), warning: true); UpdateStatus(); return; }

        _pendingRequest = new CleanupRequest(title, targets.ToArray(), title.Equals("ALLES BEREINIGEN", StringComparison.OrdinalIgnoreCase));
        ConfirmText.Text = title.Equals("ALLES BEREINIGEN", StringComparison.OrdinalIgnoreCase)
                ? L("ConfirmAll")
                : $"{title}: {L("Confirm")}";
        SetOperationMessage(L("ConfirmPrompt"));
        OperationDetailsText.Text = string.Empty;
        OperationDetailsText.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Collapsed;
        ConfirmPanel.Visibility = Visibility.Visible;
        SetCleanupEnabled(false);
    }

    private async void ConfirmYes_Click(object sender, RoutedEventArgs e)
    {
        var request = _pendingRequest;
        _pendingRequest = null;
        ConfirmPanel.Visibility = Visibility.Collapsed;
        if (request is not null) await RunCleanupAsync(request);
    }

    private void ConfirmNo_Click(object sender, RoutedEventArgs e)
    {
        _pendingRequest = null;
        ConfirmPanel.Visibility = Visibility.Collapsed;
        SetOperationMessage(L("Cancelled"));
        OperationDetailsText.Text = string.Empty;
        OperationDetailsText.Visibility = Visibility.Collapsed;
        UpdateStatus();
    }

    private void ResetCacheChecks()
    {
        foreach (var check in new[] { CacheCheck1, CacheCheck2, CacheCheck3, CacheCheck4 })
        {
            check.Text = "○";
            check.Foreground = FindResource("MutedBrush") as Brush;
        }
    }

    private void SetCacheCheck(int index, bool done, bool active = false)
    {
        var checks = new[] { CacheCheck1, CacheCheck2, CacheCheck3, CacheCheck4 };
        if (index < 0 || index >= checks.Length) return;
        checks[index].Text = done ? "✓" : active ? "•" : "○";
        checks[index].Foreground = FindResource(done ? "GreenBrush" : active ? "PurpleBrush" : "MutedBrush") as Brush;
    }

    private async Task RunCleanupAsync(CleanupRequest request)
    {
        if (_busy) return;
        if (!_fiveM.IsValidInstallation(out var reason)) { SetOperationMessage(L(reason), error: true); UpdateStatus(); return; }
        if (_fiveM.IsFiveMRunning()) { SetOperationMessage(L("RunningNoDelete"), warning: true); UpdateStatus(); return; }

        _busy = true;
        var stopwatch = Stopwatch.StartNew();
        SetCleanupEnabled(false);
        ResetCacheChecks();
        CleanupProgressBar.Value = 0;
        CleanupPercentText.Text = "0 %";
        CleanupCurrentText.Text = L("Prepare");
        ProgressPanel.Visibility = Visibility.Visible;
        SetOperationMessage($"{request.Title}{L("CleanupRunning")}");
        OperationDetailsText.Text = string.Empty;
        OperationDetailsText.Visibility = Visibility.Collapsed;

        try
        {
            var targets = request.Targets.ToArray();
            CleanupResult result = await Task.Run(() => _fiveM.CleanAsync(targets, progress: (completed, total, target) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var percent = total == 0 ? 100 : (int)Math.Round(completed * 100d / total);
                    CleanupProgressBar.Value = percent;
                    CleanupPercentText.Text = $"{percent} %";
                    CleanupCurrentText.Text = completed < total ? $"{L("Processing")}{Path.GetFileName(target)}" : L("AllProcessed");
                    if (targets.Length == 4 && target.StartsWith(_fiveM.DataDir, StringComparison.OrdinalIgnoreCase))
                        SetCacheCheck(completed - 1, true);
                });
            }));
            stopwatch.Stop();
            if (result.Success)
            {
                var successMessage = request.IsAll ? L("CleanupSuccess") : request.Title switch
                {
                    "CACHE" => L("CacheSuccess"),
                    "LOGS" => L("LogsSuccess"),
                    "CRASH DUMPS" => L("CrashesSuccess"),
                    _ => L("CleanupSuccess")
                };
                SetOperationMessage(successMessage, success: true);
                OperationDetailsText.Text = $"{FormatMegabytes(result.BytesFreed)} {L("Removed")} • {result.FilesProcessed:N0} {L("Files")} • {result.TargetsProcessed} {L("Areas")} • {L("Duration")}: {FormatDuration(stopwatch.Elapsed)}";
                OperationDetailsText.Visibility = Visibility.Visible;
            }
            else
            {
                SetOperationMessage(LocalizeServiceMessage(result.Message), error: true);
                OperationDetailsText.Text = string.Empty;
                OperationDetailsText.Visibility = Visibility.Collapsed;
            }
            if (result.Success && targets.Length == 4)
                for (var i = 0; i < 4; i++) SetCacheCheck(i, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            SetOperationMessage($"{L("ErrorPrefix")}{ex.Message}", error: true);
            OperationDetailsText.Text = string.Empty;
            OperationDetailsText.Visibility = Visibility.Collapsed;
        }
        finally
        {
            ProgressPanel.Visibility = Visibility.Collapsed;
            _busy = false;
            UpdateStatus();
            await UpdateSizesAsync();
        }
    }

    private void Cache_Click(object sender, RoutedEventArgs e) => RequestCleanup("CACHE", _fiveM.CacheTargets);
    private void Logs_Click(object sender, RoutedEventArgs e) => RequestCleanup("LOGS", [_fiveM.LogsTarget]);
    private void Crashes_Click(object sender, RoutedEventArgs e) => RequestCleanup("CRASH DUMPS", [_fiveM.CrashesTarget]);
    private void All_Click(object sender, RoutedEventArgs e) => RequestCleanup("ALLES BEREINIGEN", _fiveM.CacheTargets.Concat([_fiveM.LogsTarget, _fiveM.CrashesTarget]));
    private void Refresh_Click(object sender, RoutedEventArgs e) => UpdateStatus();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void AllButton_MouseEnter(object sender, MouseEventArgs e)
    {
        AllButton.BorderBrush = FindResource("YellowBrush") as Brush;
        AllButton.BorderThickness = new Thickness(1);
    }

    private void AllButton_MouseLeave(object sender, MouseEventArgs e)
    {
        AllButton.BorderBrush = FindResource("PurpleBrush") as Brush;
        AllButton.BorderThickness = new Thickness(1);
    }

    private void Kofi_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(KofiUrl);
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(GitHubUrl);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        _aboutVisible = true;
        MainView.Visibility = Visibility.Collapsed;
        AboutView.Visibility = Visibility.Visible;
        SetCleanupEnabled(false);
    }

    private void AboutClose_Click(object sender, RoutedEventArgs e)
    {
        _aboutVisible = false;
        AboutView.Visibility = Visibility.Collapsed;
        MainView.Visibility = Visibility.Visible;
        UpdateStatus();
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetOperationMessage($"{L("ErrorPrefix")}{ex.Message}", error: true);
        }
    }
    private void Twitch_Click(object sender, RoutedEventArgs e) => OpenUrl(TwitchUrl);
    private void Discord_Click(object sender, RoutedEventArgs e) => OpenUrl(DiscordUrl);
}
