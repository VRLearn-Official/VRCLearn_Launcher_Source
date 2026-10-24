using System.IO;
using VRCLearn.App.ViewModels;
using VRCLearn.Core.Templates;

namespace VRCLearn.App.Services;

public sealed class DropImportService(
    ProjectsViewModel projects,
    BackupsViewModel backups,
    TemplatesViewModel templates,
    LocalizationService localization,
    ToastService toasts)
{
    private static bool Supports(string pageKey) =>
        pageKey is "projects" or "backups" or "templates";

    public static bool CanAccept(string pageKey, IReadOnlyList<string> paths) =>
        Supports(pageKey)
        && pageKey switch
        {
            "projects" => paths.Any(Directory.Exists),
            "backups" => paths.Any(IsArchive),
            "templates" => paths.Any(IsTemplate),
            _ => false,
        };

    public async Task ImportAsync(string pageKey, IReadOnlyList<string> paths)
    {
        switch (pageKey)
        {
            case "projects":
            {
                var directories = paths.Where(Directory.Exists).ToList();

                if (directories.Count == 0)
                {
                    Invalid();
                    return;
                }

                foreach (var directory in directories)
                {
                    await projects.AddAsync(directory);
                }

                return;
            }
            case "backups":
            {
                var archives = paths.Where(IsArchive).ToList();

                if (archives.Count == 0)
                {
                    Invalid();
                    return;
                }

                await backups.ImportFilesAsync(archives);
                return;
            }
            case "templates":
            {
                var files = paths.Where(IsTemplate).ToList();

                if (files.Count == 0)
                {
                    Invalid();
                    return;
                }

                await templates.ImportAsync(files);
                return;
            }
        }
    }

    private static bool IsArchive(string path) =>
        File.Exists(path)
        && string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase);

    private static bool IsTemplate(string path) =>
        File.Exists(path)
        && string.Equals(
            Path.GetExtension(path),
            TemplateFormat.Extension,
            StringComparison.OrdinalIgnoreCase);

    private void Invalid() => toasts.Show(localization["Drop.Invalid"]);
}
