using VRCLearn.App.Services;
using VRCLearn.Infrastructure.Windows;

namespace VRCLearn.App.ViewModels;

public sealed record CheckReportItem(string Name, bool Passed, string Detail)
{
    public static CheckReportItem From(SystemCheckResult result, LocalizationService localization) =>
        new(
            localization[$"Check.{result.Kind}.Name"],
            result.Passed,
            localization.Format($"Check.{result.Kind}.{(result.Passed ? "Ok" : "Bad")}", result.Detail));
}
