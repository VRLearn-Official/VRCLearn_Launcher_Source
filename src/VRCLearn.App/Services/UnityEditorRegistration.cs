using Microsoft.Extensions.Logging;
using VRCLearn.Core.Unity;
using VRCLearn.Core.Vpm;

namespace VRCLearn.App.Services;

public sealed class UnityEditorRegistration(
    IUnityEditorLocator editors,
    ILauncherSettingsStore settings,
    DialogService dialogs,
    LocalizationService localization,
    ToastService toasts,
    ILogger<UnityEditorRegistration> logger)
{
    public bool Add(string executablePath)
    {
        if (editors.Probe(executablePath) is not { } editor)
        {
            logger.LogWarning("指定的 Unity 编辑器认不出来：{Path}", executablePath);

            toasts.Show(localization.Format("Settings.UnityEditor.Unrecognized", executablePath));

            return false;
        }

        if (editor.Version != UnityReleaseCatalog.SupportedVersion
            && !dialogs.ShowConfirm(
                localization["Settings.UnityEditor.WarningTitle"],
                localization.Format("Settings.UnityEditor.Warning", UnityReleaseCatalog.SupportedVersion),
                localization["Settings.UnityEditor.AddAnyway"]))
        {
            logger.LogInformation("取消添加 Unity 编辑器：{Version}", editor.Version);

            return false;
        }

        var added = settings.Current.UnityEditors;

        if (!added.Contains(executablePath, StringComparer.OrdinalIgnoreCase))
        {
            added.Add(executablePath);
            settings.Save();
        }

        logger.LogInformation("添加 Unity 编辑器：{Version} {Path}", editor.Version, executablePath);
        toasts.Show(localization.Format("Settings.UnityEditor.Added", editor.Version));

        return true;
    }
}
