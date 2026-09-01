using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;
using XmlUtilities;

namespace Game {
    public class ClothingData {
        public ClothingData() { }

        public ClothingData(XElement item) {
            int.TryParse(item.Attribute("Index").Value, out int clothIndex);
            clothIndex &= 1023;
            string newDescription = item.Attribute("Description")?.Value;
            string newDisplayName = item.Attribute("DisplayName")?.Value;
            if (newDescription != null
                && newDescription.StartsWith('[')
                && newDescription.EndsWith(']')
                && LanguageControl.TryGetBlock(newDescription.Substring(1, newDescription.Length - 2), "Description", out string d)) {
                newDescription = d;
            }
            if (newDisplayName != null
                && newDisplayName.StartsWith('[')
                && newDisplayName.EndsWith(']')
                && LanguageControl.TryGetBlock(newDisplayName.Substring(1, newDisplayName.Length - 2), "DisplayName", out string n)) {
                newDisplayName = n;
            }
            xElement = item;
            Index = clothIndex;
            DisplayName = newDisplayName;
            string slotName = XmlUtils.GetAttributeValue<string>(item, "Slot");
            Slot = ClothingSlot.ClothingSlots[slotName];
            ArmorProtection = XmlUtils.GetAttributeValue<float>(item, "ArmorProtection");
            Sturdiness = XmlUtils.GetAttributeValue<float>(item, "Sturdiness");
            Insulation = XmlUtils.GetAttributeValue<float>(item, "Insulation");
            MovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item, "MovementSpeedFactor");
            SteedMovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item, "SteedMovementSpeedFactor");
            DensityModifier = XmlUtils.GetAttributeValue<float>(item, "DensityModifier");
            IsOuter = XmlUtils.GetAttributeValue<bool>(item, "IsOuter");
            CanBeDyed = XmlUtils.GetAttributeValue<bool>(item, "CanBeDyed");
            Layer = XmlUtils.GetAttributeValue<int>(item, "Layer");
            PlayerLevelRequired = XmlUtils.GetAttributeValue<int>(item, "PlayerLevelRequired");
            ImpactSoundsFolder = XmlUtils.GetAttributeValue<string>(item, "ImpactSoundsFolder");
            Description = newDescription;
            DisplayIndex = XmlUtils.GetAttributeValue<int>(item, "DisplayIndex", -1);
            string textureRoute = XmlUtils.GetAttributeValue<string>(item, "TextureName");
            if (XmlUtils.GetAttributeValue<bool>(item, "UseLazyLoading", false)) {
                _textureName = textureRoute; // 保存纹理名称用于按需加载
            }
            else {
                Texture = ContentManager.Get<Texture2D>(textureRoute);//立即加载纹理
            }
        }

        public string _textureName;

        public Texture2D Texture;

        public XElement xElement;

        public int Index;

        public int DisplayIndex;

        public ClothingSlot Slot;

        public float ArmorProtection;

        public float Sturdiness;

        public float Insulation;

        public float MovementSpeedFactor;

        public float SteedMovementSpeedFactor;

        public float DensityModifier;

        public string DisplayName;

        public string Description;

        public string ImpactSoundsFolder;

        public bool IsOuter;

        public bool CanBeDyed;

        public int Layer;

        public int PlayerLevelRequired;

        /// <summary>
        ///     装备
        /// </summary>
        public Action<int, ComponentClothing> Mount;

        /// <summary>
        ///     卸载
        /// </summary>
        public Action<int, ComponentClothing> Dismount;

        /// <summary>
        ///     ComponentClothing更新时触发。
        /// </summary>
        public Action<int, ComponentClothing> Update;

        /// <summary>
        ///     模组可以向Dictionary里面添加特殊数据，另一个模组可以从Dictionary读取数据，以实现模组联动效果
        /// </summary>
        public ValuesDictionary DictionaryForOtherMods = new();

        /// <summary>
        ///     计算单件护甲的防御
        /// </summary>
        /// <param name="componentClothing"></param>
        /// <param name="clothesBeforeProtection">在结算防御前，玩家的衣物列表</param>
        /// <param name="clothesAfterProtection">在结算防御后，玩家将会有的的衣物列表</param>
        /// <param name="sequence">表示这是结算到第几件护甲</param>
        /// <param name="attackment">导致这次ApplyArmorProtection的攻击，注意attackment.AttackPower指的是被任何护甲结算前的原始攻击力</param>
        /// <param name="attackPowerAfterProtection">被该件护甲结算后，剩余的攻击力</param>
        public virtual void ApplyArmorProtection(ComponentClothing componentClothing,
            List<int> clothesBeforeProtection,
            List<int> clothesAfterProtection,
            int sequence,
            Attackment attackment,
            ref float attackPowerAfterProtection) {
            int value = clothesBeforeProtection[sequence];
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            float maxDurability = block.GetDurability(value) + 1;
            float remainingSturdiness = (maxDurability - block.GetDamage(value)) / maxDurability * Sturdiness;
            float damageToAbsorb = MathF.Min(
                attackPowerAfterProtection * MathUtils.Saturate(ArmorProtection / attackment.ArmorProtectionDivision),
                remainingSturdiness
            );
            if (damageToAbsorb > 0f) {
                attackPowerAfterProtection -= damageToAbsorb;
                if (componentClothing.m_subsystemGameInfo.WorldSettings.GameMode != 0) {
                    float x2 = damageToAbsorb / Sturdiness * maxDurability + 0.001f;
                    int damageCount = (int)(MathF.Floor(x2) + (componentClothing.m_random.Bool(MathUtils.Remainder(x2, 1f)) ? 1 : 0));
                    clothesAfterProtection[sequence] = BlocksManager.DamageItem(value, damageCount, componentClothing.Entity);
                    Block blockDamaged = BlocksManager.Blocks[Terrain.ExtractContents(clothesAfterProtection[sequence])];
                    if (!blockDamaged.CanWear(clothesAfterProtection[sequence])) {
                        componentClothing.m_subsystemParticles.AddParticleSystem(
                            new BlockDebrisParticleSystem(
                                componentClothing.m_subsystemTerrain,
                                componentClothing.m_componentBody.Position + componentClothing.m_componentBody.StanceBoxSize / 2f,
                                1f,
                                1f,
                                Color.White,
                                0
                            )
                        );
                    }
                }
                if (!string.IsNullOrEmpty(ImpactSoundsFolder)) {
                    componentClothing.m_subsystemAudio.PlayRandomSound(
                        ImpactSoundsFolder,
                        1f,
                        componentClothing.m_random.Float(-0.3f, 0.3f),
                        componentClothing.m_componentBody.Position,
                        4f,
                        0.15f
                    );
                }
            }
        }

        /// <summary>
        ///     在ComponentClothing中每帧都会调用的UpdateGraduallyDamagedOverTime()，主要用于控制衣物随时间逐渐损坏
        /// </summary>
        /// <param name="componentClothing"></param>
        /// <param name="indexInClothesList"></param>
        /// <param name="dt"></param>
        public virtual void UpdateGraduallyDamagedOverTime(ComponentClothing componentClothing, int indexInClothesList, float dt) {
            try {
                if (componentClothing.m_subsystemGameInfo.WorldSettings.GameMode == 0
                    || !componentClothing.m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                    return;
                }
                {
                    float num2 = componentClothing.m_componentVitalStats.Wetness > 0f ? 10f * Sturdiness : 20f * Sturdiness;
                    double num3 = Math.Floor(componentClothing.m_lastTotalElapsedGameTime.Value / num2);
                    if (Math.Floor(componentClothing.m_subsystemGameInfo.TotalElapsedGameTime / num2) > num3
                        && componentClothing.m_random.Float(0f, 1f) < 0.75f) {
                        componentClothing.m_clothesList[indexInClothesList] = BlocksManager.DamageItem(
                            componentClothing.m_clothesList[indexInClothesList],
                            1,
                            componentClothing.Entity
                        );
                        //检查衣服是否已损坏
                        int damagedClothingBlockValue = componentClothing.m_clothesList[indexInClothesList];
                        Block clothingBlock = BlocksManager.Blocks[Terrain.ExtractContents(damagedClothingBlockValue)];
                        if (!clothingBlock.CanWear(damagedClothingBlockValue)) {
                            componentClothing.m_subsystemParticles.AddParticleSystem(
                                new BlockDebrisParticleSystem(
                                    componentClothing.m_subsystemTerrain,
                                    componentClothing.m_componentBody.Position + componentClothing.m_componentBody.StanceBoxSize / 2f,
                                    1f,
                                    1f,
                                    Color.White,
                                    0
                                )
                            );
                            componentClothing.m_componentGui.DisplaySmallMessage(
                                LanguageControl.Get(typeof(ComponentClothing).Name, 2),
                                Color.White,
                                true,
                                true
                            );
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Error(ex);
            }
        }

        /// <summary>
        ///     在ComponentClothing执行SetClothes()时触发，用于调整ComponentClothing中的一些参数
        /// </summary>
        /// <param name="componentClothing"></param>
        public virtual void OnClotheSet(ComponentClothing componentClothing) {
            componentClothing.InsulationBySlots[Slot] += Insulation;
            componentClothing.SteedMovementSpeedFactor *= SteedMovementSpeedFactor;
            componentClothing.m_densityModifierApplied += DensityModifier;
        }

        /// <summary>
        ///     获取衣物穿着在身上时的颜色附加
        /// </summary>
        /// <returns></returns>
        public virtual Color GetColor(ComponentClothing componentClothing, int value) {
            int data = Terrain.ExtractData(value);
            return SubsystemPalette.GetFabricColor(componentClothing.m_subsystemTerrain, ClothingBlock.GetClothingColor(data));
        }
    }
}