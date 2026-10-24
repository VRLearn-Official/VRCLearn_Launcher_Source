using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRCLearn.App.Services;
using VRCLearn.Core.Http;
using VRCLearn.Core.Unity;
using VRCLearn.Infrastructure.Shell;
using VRCLearn.Infrastructure.Windows;

namespace VRCLearn.App.ViewModels;

public sealed partial class DownloadViewModel : PageViewModelBase
{
    private readonly IFileDownloader _downloader;
    private readonly DialogService _dialogs;
    private readonly LocalizationService _localization;
    private readonly NavigationService _navigation;
    private readonly SettingsViewModel _settings;
    private readonly SystemCheckService _systemCheck;
    private readonly ILogger<DownloadViewModel> _logger;

    private CancellationTokenSource? _cancellation;

    public DownloadViewModel(
        IFileDownloader downloader,
        DialogService dialogs,
        LocalizationService localization,
        NavigationService navigation,
        SettingsViewModel settings,
        SystemCheckService systemCheck,
        ToastService toasts,
        ILogger<DownloadViewModel> logger)
        : base(toasts)
    {
        _downloader = downloader;
        _dialogs = dialogs;
        _localization = localization;
        _navigation = navigation;
        _settings = settings;
        _systemCheck = systemCheck;
        _logger = logger;

        _localization.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SubtitleText));
            OnPropertyChanged(nameof(CheckButtonText));
        };
    }

    protected override ILogger Logger => _logger;

    protected override LocalizationService Localization => _localization;

    public string SubtitleText =>
        _localization.Format("Download.Subtitle", UnityReleaseCatalog.Current.Version);

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private string _currentFile = "";

    [ObservableProperty]
    private bool _isChecking;

    public bool IsNotChecking => !IsChecking;

    public string CheckButtonText =>
        _localization[IsChecking ? "Download.CheckRunning" : "Download.CheckButton"];

    partial void OnIsCheckingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotChecking));
        OnPropertyChanged(nameof(CheckButtonText));
    }

    [RelayCommand]
    private async Task CheckSystem()
    {
        if (IsChecking)
        {
            return;
        }

        IsChecking = true;

        IReadOnlyList<SystemCheckResult> results;

        try
        {
            results = await _systemCheck.RunAsync();
        }
        finally
        {
            IsChecking = false;
        }

        _dialogs.ShowCheckReport(
            _localization["Check.Title"],
            [.. results.Select(x => CheckReportItem.From(x, _localization))],
            _localization["Dialog.Confirm"]);
    }

    [RelayCommand]
    private Task DownloadUnity() => DownloadAsync(UnityReleaseCatalog.Current.Editor, "Download.Name.Editor");

    [RelayCommand]
    private Task DownloadAndroid() => DownloadAsync(UnityReleaseCatalog.Current.Android, "Download.Name.Android");

    [RelayCommand]
    private Task DownloadIos() => DownloadAsync(UnityReleaseCatalog.Current.Ios, "Download.Name.Ios");

    [RelayCommand]
    private async Task DownloadHub()
    {
        if (IsDownloading)
        {
            return;
        }

        var choice = _dialogs.ShowUnityHubDownload(
            _localization["Download.Prompt.Title"],
            _localization["Download.Hub.Notice"],
            _localization["Download.Hub.Official"],
            _localization["Download.Prompt.Continue"],
            _localization["Download.Proxy.OpenSettings"]);

        switch (choice)
        {
            case UnityDownloadChoice.OpenOfficial:
                ShellLauncher.OpenUrl(Links.UnityHubOfficial);
                break;
            case UnityDownloadChoice.OpenProxy:
                OpenProxy();
                break;
            case UnityDownloadChoice.Download:
                await SaveAsync(UnityReleaseCatalog.UnityHub);
                break;
        }
    }

    [RelayCommand]
    private void Cancel() => _cancellation?.Cancel();

    private async Task DownloadAsync(UnityDownload target, string nameKey)
    {
        if (IsDownloading || !ConfirmDownload(nameKey))
        {
            return;
        }

        await SaveAsync(target);
    }

    private bool ConfirmDownload(string nameKey)
    {
        var name = _localization[nameKey];

        var choice = _dialogs.ShowUnityDownload(
            _systemCheck.EditionAsync,
            _localization["Download.Prompt.Title"],
            _localization["Download.Prompt.Checking"],
            _localization.Format("Download.Prompt.Edition", _localization["Check.Edition.China"], name),
            _localization.Format("Download.Prompt.Edition", _localization["Check.Edition.International"], name),
            _localization.Format("Download.Prompt.Unknown", name),
            _localization["Download.Prompt.Continue"],
            _localization["Download.Proxy.OpenSettings"]);

        if (choice == UnityDownloadChoice.OpenProxy)
        {
            OpenProxy();
        }

        return choice == UnityDownloadChoice.Download;
    }

    private async Task SaveAsync(UnityDownload target)
    {
        var path = _dialogs.PickSaveFile(
            _localization["Download.Save.Title"],
            target.FileName,
            _localization["Download.FileFilter"]);

        if (path is null)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();

        _cancellation = cancellation;
        IsDownloading = true;
        Progress = 0;
        ProgressText = "";
        CurrentFile = target.FileName;
        Status = _localization.Format("Download.Status.Downloading", target.FileName);

        var progress = new Progress<DownloadProgress>(OnProgress);

        try
        {
            await _downloader.DownloadAsync(target.Url, path, progress, cancellation.Token);

            Status = _localization.Format("Download.Status.Done", path);
        }
        catch (OperationCanceledException)
        {
            Status = _localization["Download.Status.Canceled"];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "下载失败：{Url}", target.Url);

            Status = _localization.Format("Download.Status.Failed", exception.Message);
        }
        finally
        {
            IsDownloading = false;
            _cancellation = null;
            ProgressText = "";
            CurrentFile = "";
        }
    }

    private void OpenProxy()
    {
        _settings.FocusProxy();
        _navigation.GoTo("settings");
    }

    private void OnProgress(DownloadProgress value)
    {
        if (value.Total is not { } total || total <= 0)
        {
            ProgressText = _localization.Format("Download.Progress.Received", SizeText.Format(value.Received));
            return;
        }

        Progress = value.Received * 100d / total;
        ProgressText = _localization.Format(
            "Download.Progress.Bytes",
            SizeText.Format(value.Received),
            SizeText.Format(total),
            Progress.ToString("0"));
    }
}
