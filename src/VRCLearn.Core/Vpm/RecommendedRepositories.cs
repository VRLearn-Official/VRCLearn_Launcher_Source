namespace VRCLearn.Core.Vpm;

/// <summary>「一键添加」的入口表。</summary>
/// <param name="Id">本地化用的名字，不是仓库自己的 id。</param>
public sealed record RecommendedRepository(string Id, string Url);

public sealed record RecommendedRepositoryGroup(
    string Id,
    RecommendedRepositoryKind Kind,
    string Homepage,
    IReadOnlyList<RecommendedRepository> Repositories);

public enum RecommendedRepositoryKind
{
    Official,

    Mirror,
}

public static class RecommendedRepositories
{
    /// <summary>界面上的顺序就是这儿的顺序。</summary>
    public static IReadOnlyList<RecommendedRepositoryGroup> All { get; } =
    [
        new(
            "vrchat",
            RecommendedRepositoryKind.Official,
            "https://vrchat.com/home",
            [
                new("official", "https://packages.vrchat.com/official?download"),
                new("curated", "https://packages.vrchat.com/curated?download"),
            ]),
        new(
            "vrcd",
            RecommendedRepositoryKind.Mirror,
            "https://vcc.vrczh.org/",
            [
                new("vrcdOfficial", "https://vpm.vrczh.org/vpm/official"),
                new("vrcdCurated", "https://vpm.vrczh.org/vpm/curated"),
            ]),
        new(
            "vrclearn",
            RecommendedRepositoryKind.Mirror,
            "https://vpm.gamemode1.top/",
            [
                new("vrclearnOfficial", "https://vpm.gamemode1.top/official/index.json"),
                new("vrclearnCurated", "https://vpm.gamemode1.top/curated/index.json"),
            ]),
    ];
}
