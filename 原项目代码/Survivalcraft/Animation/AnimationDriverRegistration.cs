using Engine.Animation;
using Game.Animation.Drivers;

namespace Game.Animation {
    /// <summary>
    /// 动画驱动器注册
    /// 在游戏启动时注册所有内置驱动器类型
    /// </summary>
    public static class AnimationDriverRegistration {
        static bool s_registered;
        static readonly object s_lock = new();

        /// <summary>
        /// 是否已注册
        /// </summary>
        public static bool IsRegistered => s_registered;

        /// <summary>
        /// 注册所有游戏层驱动器
        /// </summary>
        public static void Register() {
            lock (s_lock) {
                if (s_registered) {
                    return;
                }

                // 注册基础驱动器
                AnimationDriverManager.Register("LookAt", typeof(LookAtDriver));
                AnimationDriverManager.Register("LookAtDriver", typeof(LookAtDriver));
                AnimationDriverManager.Register("Death", typeof(DeathDriver));
                AnimationDriverManager.Register("DeathDriver", typeof(DeathDriver));
                AnimationDriverManager.Register("Expression", typeof(ExpressionDriver));
                AnimationDriverManager.Register("ExpressionDriver", typeof(ExpressionDriver));

                // 注册四足动物驱动器
                AnimationDriverManager.Register("FourLeggedWalk", typeof(FourLeggedWalkDriver));
                AnimationDriverManager.Register("FourLeggedWalkDriver", typeof(FourLeggedWalkDriver));
                AnimationDriverManager.Register("FourLeggedTrot", typeof(FourLeggedWalkDriver)); // Trot 使用同一个驱动器
                AnimationDriverManager.Register("FourLeggedCanter", typeof(FourLeggedWalkDriver)); // Canter 使用同一个驱动器
                AnimationDriverManager.Register("FourLeggedAttack", typeof(FourLeggedAttackDriver));
                AnimationDriverManager.Register("FourLeggedAttackDriver", typeof(FourLeggedAttackDriver));
                AnimationDriverManager.Register("FourLeggedDeath", typeof(FourLeggedDeathDriver));
                AnimationDriverManager.Register("FourLeggedDeathDriver", typeof(FourLeggedDeathDriver));
                AnimationDriverManager.Register("FourLeggedFeed", typeof(FourLeggedFeedDriver));
                AnimationDriverManager.Register("FourLeggedFeedDriver", typeof(FourLeggedFeedDriver));

                // 注册鱼类驱动器
                AnimationDriverManager.Register("FishSwim", typeof(FishSwimDriver));
                AnimationDriverManager.Register("FishSwimDriver", typeof(FishSwimDriver));
                AnimationDriverManager.Register("FishAttack", typeof(FishAttackDriver));
                AnimationDriverManager.Register("FishAttackDriver", typeof(FishAttackDriver));
                AnimationDriverManager.Register("FishDeath", typeof(FishDeathDriver));
                AnimationDriverManager.Register("FishDeathDriver", typeof(FishDeathDriver));

                // 注册鸟类驱动器
                AnimationDriverManager.Register("BirdWalk", typeof(BirdWalkDriver));
                AnimationDriverManager.Register("BirdWalkDriver", typeof(BirdWalkDriver));
                AnimationDriverManager.Register("BirdFly", typeof(BirdFlyDriver));
                AnimationDriverManager.Register("BirdFlyDriver", typeof(BirdFlyDriver));
                AnimationDriverManager.Register("BirdPeck", typeof(BirdPeckDriver));
                AnimationDriverManager.Register("BirdPeckDriver", typeof(BirdPeckDriver));
                AnimationDriverManager.Register("BirdAttack", typeof(BirdAttackDriver));
                AnimationDriverManager.Register("BirdAttackDriver", typeof(BirdAttackDriver));
                AnimationDriverManager.Register("BirdDeath", typeof(BirdDeathDriver));
                AnimationDriverManager.Register("BirdDeathDriver", typeof(BirdDeathDriver));

                // 注册不能飞的鸟类驱动器
                AnimationDriverManager.Register("FlightlessBirdWalk", typeof(FlightlessBirdWalkDriver));
                AnimationDriverManager.Register("FlightlessBirdWalkDriver", typeof(FlightlessBirdWalkDriver));
                AnimationDriverManager.Register("FlightlessBirdFeed", typeof(FlightlessBirdFeedDriver));
                AnimationDriverManager.Register("FlightlessBirdFeedDriver", typeof(FlightlessBirdFeedDriver));
                AnimationDriverManager.Register("FlightlessBirdAttack", typeof(FlightlessBirdAttackDriver));
                AnimationDriverManager.Register("FlightlessBirdAttackDriver", typeof(FlightlessBirdAttackDriver));
                AnimationDriverManager.Register("FlightlessBirdDeath", typeof(FlightlessBirdDeathDriver));
                AnimationDriverManager.Register("FlightlessBirdDeathDriver", typeof(FlightlessBirdDeathDriver));

                // 注册人类驱动器
                AnimationDriverManager.Register("HumanWalk", typeof(HumanWalkDriver));
                AnimationDriverManager.Register("HumanWalkDriver", typeof(HumanWalkDriver));
                AnimationDriverManager.Register("HumanAttack", typeof(HumanAttackDriver));
                AnimationDriverManager.Register("HumanAttackDriver", typeof(HumanAttackDriver));
                AnimationDriverManager.Register("HumanRide", typeof(HumanRideDriver));
                AnimationDriverManager.Register("HumanRideDriver", typeof(HumanRideDriver));
                AnimationDriverManager.Register("HumanDeath", typeof(HumanDeathDriver));
                AnimationDriverManager.Register("HumanDeathDriver", typeof(HumanDeathDriver));
                AnimationDriverManager.Register("HumanAim", typeof(HumanAimDriver));
                AnimationDriverManager.Register("HumanAimDriver", typeof(HumanAimDriver));
                AnimationDriverManager.Register("HumanMine", typeof(HumanMineDriver));
                AnimationDriverManager.Register("HumanMineDriver", typeof(HumanMineDriver));
                s_registered = true;
            }
        }

        /// <summary>
        /// 重置注册状态（仅用于测试）
        /// </summary>
        public static void Reset() {
            lock (s_lock) {
                s_registered = false;
                // 清除 AnimationDriverManager 中的所有注册
                AnimationDriverManager.Clear();
            }
        }
    }
}