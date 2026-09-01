namespace Game {
    public class ClothingSlot {
        /// <summary>
        ///     调用自定义部位（比如手臂）的ClothingSlot，可以用ClothingSlot.ClothingSlots["Arms"]
        /// </summary>
        public static Dictionary<string, ClothingSlot> ClothingSlots = new();

        public static Dictionary<int, ClothingSlot> ClothingSlotsByInt = new();
        public static ClothingSlot Head => ClothingSlotsByInt[0];
        public static ClothingSlot Torso => ClothingSlotsByInt[1];
        public static ClothingSlot Legs => ClothingSlotsByInt[2];
        public static ClothingSlot Feet => ClothingSlotsByInt[3];

        public static void AddClothingSlot(string name) {
            ClothingSlots[name] = new ClothingSlot { Name = name, StableId = ClothingSlots.Count };
            ClothingSlotsByInt[ClothingSlots[name].StableId] = ClothingSlots[name];
        }

        public static void Initialize() {
            ClothingSlots.Clear();
            AddClothingSlot("Head");
            AddClothingSlot("Torso");
            AddClothingSlot("Legs");
            AddClothingSlot("Feet");
            ClothingSlots["Head"].MessageWhenLeastInsulated = LanguageControl.Get(ComponentVitalStats.fName, 41);
            ClothingSlots["Torso"].MessageWhenLeastInsulated = LanguageControl.Get(ComponentVitalStats.fName, 42);
            ClothingSlots["Legs"].MessageWhenLeastInsulated = LanguageControl.Get(ComponentVitalStats.fName, 43);
            ClothingSlots["Feet"].MessageWhenLeastInsulated = LanguageControl.Get(ComponentVitalStats.fName, 44);
            ClothingSlots["Head"].BasicInsulation = 2f;
            ClothingSlots["Torso"].BasicInsulation = 0.2f;
            ClothingSlots["Legs"].BasicInsulation = 0.4f;
            ClothingSlots["Feet"].BasicInsulation = 2f;
            ModsManager.HookAction(
                "InitializeClothingSlots",
                loader => {
                    loader.InitializeClothingSlots();
                    return false;
                }
            );
        }

        public int StableId;

        public float BasicInsulation = 1e8f;

        public string Name;
        public virtual string MessageWhenLeastInsulated { get; set; } = string.Empty;

        // 显式转换操作符
        public static explicit operator int(ClothingSlot slot) => slot.StableId;
        public static implicit operator ClothingSlot(int id) => ClothingSlotsByInt.TryGetValue(id, out ClothingSlot slot) ? slot : null;
    }
}