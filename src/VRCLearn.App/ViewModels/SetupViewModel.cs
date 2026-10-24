using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VRCLearn.App.Services;
using VRCLearn.Core.Projects;
using VRCLearn.Core.Unity;
using VRCLearn.Core.Vpm;
using VRCLearn.Infrastructure.Http;
using VRCLearn.Infrastructure.Shell;
using VRCLearn.Infrastructure.Storage;
using VRCLearn.Infrastructure.Windows;

namespace VRCLearn.App.ViewModels;

public sealed partial class SetupViewModel : ObservableObject
{
    public const int StepCount = 6;

    private readonly GuiConfigStore _guiConfigStore;
    private readonly ILauncherSettingsStore _settings;
    private readonly IBackupStore _backupStore;
    private readonly LocalizationService _localization;
    private readonly ThemeService _theme;
    private readonly WindowsProtocolRegistration _protocols;
    private readonly IUnityHubLauncher _hub;
    private readonly IUnityEditorLocator _editors;
    private readonly UnityEditorRegistration _editorRegistration;
    private readonly NavigationService _navigation;
    private readonly MutableProxy _proxy;
    private readonly RepositoriesViewModel _repositories;
    private readonly SystemCheckService _systemCheck;
    private readonly ILogger<SetupViewModel> _logger;
    private readonly IReadOnlyList<Option> _languages;
    private readonly IReadOnlyList<Option> _themes;

    public SetupViewModel(
        GuiConfigStore guiConfigStore,
        ILauncherSettingsStore settings,
        IBackupStore backupStore,
        LocalizationService localization,
        ThemeService theme,
        WindowsProtocolRegistration protocols,
        IUnityHubLauncher hub,
        IUnityEditorLocator editors,
        UnityEditorRegistration editorRegistration,
        NavigationService navigation,
        MutableProxy proxy,
        RepositoriesViewModel repositories,
        PackageCacheViewModel cache,
        SystemCheckService systemCheck,
        ILogger<SetupViewModel> logger)
    {
        _logger = logger;
        _guiConfigStore = guiConfigStore;
        _settings = settings;
        _backupStore = backupStore;
        _localization = localization;
        _theme = theme;
        _protocols = protocols;
        _hub = hub;
        _editors = editors;
        _editorRegistration = editorRegistration;
        _navigation = navigation;
        _proxy = proxy;
        _repositories = repositories;
        _systemCheck = systemCheck;
        Cache = cache;

        _languages = LocalizedOptions.Languages(localization);
        _themes = LocalizedOptions.Themes(localization);

        _localization.PropertyChanged += (_, _) =>
        {
            LocalizedOptions.Refresh(_languages);
            LocalizedOptions.Refresh(_themes);
            ReloadRepositoryGroups();
        };

        var config = guiConfigStore.Current;

        _selectedLanguage = config.Language;
        _selectedTheme = config.Theme;
        _useForVccProtocol = config.UseForVccProtocol;

        _unityHubPath = hub.ExecutablePath ?? settings.Current.UnityHubPath;
        _projectDirectory = settings.Current.EffectiveProjectPath;
        _backupDirectory = backupStore.Directory;

        ReloadRepositoryGroups();
    }

    public bool IsRerun => _guiConfigStore.Current.SetupCompleted;

    public void Restart()
    {
        Step = 0;

        var config = _guiConfigStore.Current;

        SelectedLanguage = config.Language;
        SelectedTheme = config.Theme;
        UseForVccProtocol = config.UseForVccProtocol;

        _applyingStores = true;

        try
        {
            UnityHubPath = _hub.ExecutablePath ?? _settings.Current.UnityHubPath;
            ProjectDirectory = _settings.Current.EffectiveProjectPath;
            BackupDirectory = _backupStore.Directory;
        }
        finally
        {
            _applyingStores = false;
        }

        OnPropertyChanged(nameof(IsRerun));
    }

    private bool _applyingStores;

    [ObservableProperty]
    private int _step;

    partial void OnStepChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsNotLastStep));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepCaption));

        if (value == EditorStep)
        {
            DetectEditors();
        }
    }

    private const int EditorStep = 2;

    public bool CanGoBack => Step > 0;

    public bool IsNotLastStep => Step < StepCount - 1;

    public bool IsLastStep => !IsNotLastStep;

    public string StepCaption =>
        _localization.Format("Setup.StepCaption", Step + 1, StepCount);

    [ObservableProperty]
    private bool _isBusy;

    // ---- 外观 ----

    public IReadOnlyList<Option> Languages => _languages;

    [ObservableProperty]
    private string _selectedLanguage;

    partial void OnSelectedLanguageChanged(string value)
    {
        var config = _guiConfigStore.Current;
        config.Language = value;
        _guiConfigStore.Save();

        _localization.Apply(value);
        _logger.LogInformation("界面语言：{Language}", value);
    }

    public IReadOnlyList<Option> Themes => _themes;

    [ObservableProperty]
    private string _selectedTheme;

    partial void OnSelectedThemeChanged(string value)
    {
        var config = _guiConfigStore.Current;
        config.Theme = value;
        _guiConfigStore.Save();

        _theme.Apply(value);
        _logger.LogInformation("界面主题：{Theme}", value);
    }

    // ---- Unity Hub ----

    [ObservableProperty]
    private string _unityHubPath;

    partial void OnUnityHubPathChanged(string value)
    {
        if (_applyingStores)
        {
            return;
        }

        _settings.Current.UnityHubPath = value;
        _settings.Save();
    }

    // ---- 已装的 Unity ----

    public ObservableCollection<SetupEditorViewModel> InstalledEditors { get; } = [];

    public bool HasNoEditors => InstalledEditors.Count == 0;

    [RelayCommand]
    private void DetectEditors()
    {
        InstalledEditors.Clear();

        var manual = _settings.Current.UnityEditors;

        foreach (var editor in _editors
            .FindEditors()
            .Where(x => x.Version == UnityReleaseCatalog.SupportedVersion
                || manual.Contains(x.ExecutablePath, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Version))
        {
            InstalledEditors.Add(new SetupEditorViewModel(editor));
        }

        OnPropertyChanged(nameof(HasNoEditors));
    }

    public void AddUnityEditor(string executablePath)
    {
        if (_editorRegistration.Add(executablePath))
        {
            DetectEditors();
        }
    }

    // ---- 仓库 ----

    public ObservableCollection<RecommendedGroupViewModel> RepositoryGroups { get; } = [];

    public bool CanFinish => HasChecked;

    private void ReloadRepositoryGroups()
    {
        var showMirrors = _localization.CurrentLanguage == "zh-Hans";

        RepositoryGroups.Clear();

        foreach (var group in _repositories.BuildGroups())
        {
            if (showMirrors || !group.IsMirror)
            {
                RepositoryGroups.Add(group);
            }
        }
    }

    [RelayCommand]
    private async Task AddRepositoryGroup(RecommendedGroupViewModel group)
    {
        IsBusy = true;

        try
        {
            await _repositories.AddGroupAsync(group);
        }
        finally
        {
            IsBusy = false;
        }

        ReloadRepositoryGroups();
    }

    // ---- 新项目默认位置 ----

    [ObservableProperty]
    private string _projectDirectory;

    partial void OnProjectDirectoryChanged(string value)
    {
        if (_applyingStores)
        {
            return;
        }

        _settings.Current.DefaultProjectPath = value;
        _settings.Save();
    }

    public string ActualProjectDirectory => _settings.Current.EffectiveProjectPath;

    // ---- 备份与链接 ----

    [ObservableProperty]
    private string _backupDirectory;

    partial void OnBackupDirectoryChanged(string value)
    {
        if (_applyingStores)
        {
            return;
        }

        _settings.Current.ProjectBackupPath = value;
        _settings.Save();
    }

    // ---- 包缓存 ----

    public PackageCacheViewModel Cache { get; }

    [ObservableProperty]
    private bool _useForVccProtocol;

    partial void OnUseForVccProtocolChanged(bool value)
    {
        var config = _guiConfigStore.Current;
        config.UseForVccProtocol = value;
        _guiConfigStore.Save();

        _protocols.Apply(value, config.UseForAlcomTemplateFiles);
        _logger.LogInformation("vcc:// 关联：{Value}", value);
    }

    // ---- 环境检测 ----

    public ObservableCollection<CheckReportItem> CheckResults { get; } = [];

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _hasChecked;

    partial void OnHasCheckedChanged(bool value) => OnPropertyChanged(nameof(CanFinish));

    [RelayCommand]
    private async Task RunCheck()
    {
        if (IsChecking || HasChecked)
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

        CheckResults.Clear();

        foreach (var result in results)
        {
            CheckResults.Add(CheckReportItem.From(result, _localization));
        }

        HasChecked = true;

        _logger.LogInformation("向导环境检测：{Total} 项里 {Failed} 项没过", results.Count, results.Count(x => !x.Passed));
    }

    // ---- 步骤 ----

    [RelayCommand]
    private void Next()
    {
        if (Step < StepCount - 1)
        {
            Step++;
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (Step > 0)
        {
            Step--;
        }
    }

    [RelayCommand]
    private void Cancel() => _navigation.GoTo("settings");

    [RelayCommand]
    private void Finish()
    {
        var config = _guiConfigStore.Current;
        config.SetupCompleted = true;
        _guiConfigStore.Save();

        _proxy.Configure(config.ProxyEnabled, config.ProxyUrl);

        _navigation.GoTo("projects");
    }
}

public sealed class SetupEditorViewModel(UnityEditor editor)
{
    public string Version { get; } = editor.Version.ToString();

    public string Path { get; } = editor.ExecutablePath;
}
