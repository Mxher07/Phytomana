namespace Game {
    public interface IInjurable {
        float Health { get; set; }
        float VoidDamageFactor { get; set; } //y轴过高或者过低造成的伤害系数
        float AirLackResilience { get; set; } //溺水伤害抗性
        float MagmaResilience { get; set; } //熔岩伤害抗性
        float CrushResilience { get; set; } //挤压伤害抗性
        float SpikeResilience { get; set; } //尖刺伤害抗性
        float ExplosionResilience { get; set; } //爆炸伤害抗性
        float LastHealth { get; set; }
        void Injure(Injury injury);
        void Die(Injury injury);
    }
}