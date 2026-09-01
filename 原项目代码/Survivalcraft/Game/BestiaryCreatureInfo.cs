using TemplatesDatabase;

namespace Game {
    public class BestiaryCreatureInfo {
        public int Order;

        public ValuesDictionary EntityValuesDictionary;

        public string DisplayName;

        public string Description;

        public string ModelName;

        public string TextureOverride;

        public float Mass;

        public float AttackResilience;

        public float AttackPower;

        public float MovementSpeed;

        public float JumpHeight;

        public bool IsHerding;

        public bool CanBeRidden;

        public bool HasSpawnerEgg;

        public List<ComponentLoot.Loot> Loot;
    }
}