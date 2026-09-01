namespace Game {
    /// <summary>
    ///     玩法影响等级，用于标识模组对游戏平衡性的影响程度
    /// </summary>
    public enum GameplayImpactLevel {
        /// <summary>
        ///     纯装饰品，例如材质包、字体包、光影
        /// </summary>
        Cosmetic = 0,

        /// <summary>
        ///     轻度辅助，例如小地图（不透视）、箱子整理、显示生物血量、合适成本提升玩家能力
        /// </summary>
        Assist = 1,

        /// <summary>
        ///     强力辅助，例如一键撸树、自动化、矿物雷达、低成本提升玩家能力、合适成本的规则破坏
        /// </summary>
        Turbo = 2,

        /// <summary>
        ///     规则破坏，例如无/低成本地大幅提升玩家能力、飞行、传送门、掉落物/产量倍增
        /// </summary>
        Break = 3,

        /// <summary>
        ///     上帝模式，例如无敌、瞬移、无限资源
        /// </summary>
        Godmode = 4
    }
}
