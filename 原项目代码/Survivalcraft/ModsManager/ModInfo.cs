using System.Text.Json;
using NuGet.Versioning;

namespace Game {
    public enum LoadOrder {
        Survivalcraft = -2147483648,
        ThemeMod = -16384,
        Default = 0,
        HelpfulMod = 16384
    }

    public class ModInfo {
        /// <summary>
        /// 模组名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 模组版本，建议符合 Semver 标准： https://semver.org/lang/zh-CN/ <br/>
        /// 另外，相比 Semver 最多 3 个数字（1.2.3），这里支持 4 个数字（1.2.3.4）
        /// </summary>
        public string Version;
        /// <summary>
        /// 模组适配的 API 版本，低于 1.8 的模组将警告不兼容，但还是会尝试加载
        /// </summary>
        public string ApiVersion;
        /// <summary>
        /// 模组描述
        /// </summary>
        public string Description;
        /// <summary>
        /// 模组适配的生存战争游戏版本，没有实际用处
        /// </summary>
        public string ScVersion;
        public string Link;
        public string Author;
        public string PackageName;

        /// <summary>
        /// 模组加载顺序，值越小越先加载，默认为 0<br/>
        /// 建议：主题模组 -100000~-10000，辅助模组 10000~100000<br/>
        /// 注意：玩家能在游戏中手动修改顺序
        /// </summary>
        public int LoadOrder = (int)Game.LoadOrder.Default;
        public List<string> Dependencies = [];
        public NuGetVersion NuGetVersion;
        public VersionRange ApiVersionRange;
        public Dictionary<string, VersionRange> DependencyRanges = [];

        /// <summary>
        ///     非持久性模组<br/>
        ///     该项为 true 时，将不会在存档中记录该模组，从而在移除该模组后，进入存档时，不会警告未安装该模组；适用于不在存档中存储数据、添加方块、添加实体的模组
        /// </summary>
        public bool NonPersistentMod = false;

        /// <summary>
        ///     玩法影响等级，用于标识模组对游戏平衡性的影响程度
        /// </summary>
        public GameplayImpactLevel GameplayImpactLevel = GameplayImpactLevel.Cosmetic;

        /// <summary>
        ///     数据驱动设置页面树，由 ModsManager.ParseAllModSettings 在模组 dll 加载后解析 RawSettings 填充
        /// </summary>
        public List<ModSettingPage> Settings;

        /// <summary>
        ///     设置项的原始 JSON（Clone 脱离 JsonDocument 生命周期），延迟到 dll 加载后由 ParseAllModSettings 解析为 Settings。<br/>
        ///     来源优先级：modsettings.json &gt; modinfo.json 的 Settings 字段。<br/>
        ///     延迟原因：ParseSettings 的 ResolveType/ResolveWidget 依赖模组类型，而 DeserializeJson 阶段模组 dll 尚未加载。
        /// </summary>
        public JsonElement RawSettings;

        public override int GetHashCode() =>
            // ReSharper disable NonReadonlyMemberInGetHashCode
            HashCode.Combine(Name, PackageName, Version);
        // ReSharper restore NonReadonlyMemberInGetHashCode

        public override bool Equals(object obj) => obj is ModInfo && obj.GetHashCode() == GetHashCode();
    }
}