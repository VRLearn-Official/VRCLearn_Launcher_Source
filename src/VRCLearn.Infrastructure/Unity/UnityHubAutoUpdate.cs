using System.Text.Json;
using System.Text.Json.Nodes;
using VRCLearn.Core.Configuration;
using VRCLearn.Infrastructure.IO;

namespace VRCLearn.Infrastructure.Unity;

public static class UnityHubAutoUpdate
{
    private const string SettingName = "hubDisableAutoUpdate";

    public static void SetDisabled(bool disabled) => SetDisabled(SettingsPath(), disabled);

    internal static void SetDisabled(string path, bool disabled)
    {
        var root = File.Exists(path) ? Read(path) : new JsonObject();

        root[SettingName] = disabled;

        AtomicTextFile.Write(path, root.ToJsonString(JsonDefaults.Indented));
    }

    private static string SettingsPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UnityHub",
            "Settings");

    private static JsonObject Read(string path)
    {
        if (JsonNode.Parse(File.ReadAllText(path)) is JsonObject root)
        {
            return root;
        }

        throw new JsonException($"{path} 不是 JSON 对象");
    }
}
