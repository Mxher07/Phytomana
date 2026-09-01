using System.Globalization;
using System.Xml.Linq;
using Engine;

namespace Game {
    public class RecipaediaDescriptionScreen : Screen {
        public BlockIconWidget m_blockIconWidget;

        public LabelWidget m_nameWidget;

        public ButtonWidget m_leftButtonWidget;

        public ButtonWidget m_rightButtonWidget;

        public LabelWidget m_descriptionWidget;

        public LabelWidget m_propertyNames1Widget;

        public LabelWidget m_propertyValues1Widget;

        public LabelWidget m_propertyNames2Widget;

        public LabelWidget m_propertyValues2Widget;

        public int m_index;

        public IList<int> m_valuesList;
        public static string fName = "RecipaediaDescriptionScreen";

        public static RecipaediaDescriptionScreen Default => new();

        public RecipaediaDescriptionScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/RecipaediaDescriptionScreen");
            LoadContents(this, node);
            m_blockIconWidget = Children.Find<BlockIconWidget>("Icon");
            m_nameWidget = Children.Find<LabelWidget>("Name");
            m_leftButtonWidget = Children.Find<ButtonWidget>("Left");
            m_rightButtonWidget = Children.Find<ButtonWidget>("Right");
            m_descriptionWidget = Children.Find<LabelWidget>("Description");
            m_propertyNames1Widget = Children.Find<LabelWidget>("PropertyNames1");
            m_propertyValues1Widget = Children.Find<LabelWidget>("PropertyValues1");
            m_propertyNames2Widget = Children.Find<LabelWidget>("PropertyNames2");
            m_propertyValues2Widget = Children.Find<LabelWidget>("PropertyValues2");
        }

        public override void Enter(object[] parameters) {
            int item = (int)parameters[0];
            m_valuesList = (IList<int>)parameters[1];
            m_index = m_valuesList.IndexOf(item);
            UpdateBlockProperties();
        }

        public override void Update() {
            m_leftButtonWidget.IsEnabled = m_index > 0;
            m_rightButtonWidget.IsEnabled = m_index < m_valuesList.Count - 1;
            if (m_leftButtonWidget.IsClicked
                || Input.Left) {
                m_index = MathUtils.Max(m_index - 1, 0);
                UpdateBlockProperties();
            }
            if (m_rightButtonWidget.IsClicked
                || Input.Right) {
                m_index = MathUtils.Min(m_index + 1, m_valuesList.Count - 1);
                UpdateBlockProperties();
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }

        public virtual Dictionary<string, string> GetBlockProperties(int value) {
            Dictionary<string, string> dictionary = new();
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (block.GetEmittedLightAmount(value) > 0) {
                dictionary.Add("Luminosity", block.GetEmittedLightAmount(value).ToString());
            }
            if (block.GetFuelFireDuration(value) > 0f) {
                dictionary.Add("Fuel Value", block.GetFuelFireDuration(value).ToString(CultureInfo.InvariantCulture));
            }
            dictionary.Add(
                "Is Stackable",
                block.GetMaxStacking(value) > 1
                    ? string.Format(LanguageControl.Get(fName, 1), block.GetMaxStacking(value).ToString())
                    : LanguageControl.No
            );
            dictionary.Add("Is Flammable", block.GetFireDuration(value) > 0f ? LanguageControl.Yes : LanguageControl.No);
            if (block.GetNutritionalValue(value) > 0f) {
                dictionary.Add("Nutrition", block.GetNutritionalValue(value).ToString("F",CultureInfo.InvariantCulture));
            }
            if (block.GetRotPeriod(value) > 0) {
                dictionary.Add(
                    "Max Storage Time",
                    string.Format(LanguageControl.Get(fName, 2), $"{2 * block.GetRotPeriod(value) * 60f / 1200f:0.0}")
                );
            }
            if (block.GetBlockDigMethod(value) != 0) {
                dictionary.Add("Digging Method", LanguageControl.Get("DigMethod", block.GetBlockDigMethod(value).ToString()));
                dictionary.Add("Digging Resilience", block.GetDigResilience(value).ToString(CultureInfo.InvariantCulture));
            }
            if (block.GetExplosionResilience(value) > 0f) {
                dictionary.Add("Explosion Resilience", block.GetExplosionResilience(value).ToString(CultureInfo.InvariantCulture));
            }
            if (block.GetExplosionPressure(value) > 0f) {
                dictionary.Add("Explosive Power", block.GetExplosionPressure(value).ToString(CultureInfo.InvariantCulture));
            }
            bool flag = false;
            if (block.GetMeleePower(value) > 1f) {
                dictionary.Add("Melee Power", block.GetMeleePower(value).ToString(CultureInfo.InvariantCulture));
                flag = true;
            }
            if (block.GetMeleePower(value) > 1f) {
                dictionary.Add("Melee Hit Ratio", $"{100f * block.GetMeleeHitProbability(value):0}%");
                flag = true;
            }
            if (block.GetProjectilePower(value) > 1f) {
                dictionary.Add("Projectile Power", block.GetProjectilePower(value).ToString(CultureInfo.InvariantCulture));
                flag = true;
            }
            if (block.GetShovelPower(value) > 1f) {
                dictionary.Add("Shoveling", block.GetShovelPower(value).ToString(CultureInfo.InvariantCulture));
                flag = true;
            }
            if (block.GetHackPower(value) > 1f) {
                dictionary.Add("Hacking", block.GetHackPower(value).ToString(CultureInfo.InvariantCulture));
                flag = true;
            }
            if (block.GetQuarryPower(value) > 1f) {
                dictionary.Add("Quarrying", block.GetQuarryPower(value).ToString(CultureInfo.InvariantCulture));
                flag = true;
            }
            if (flag && block.GetDurability(value) > 0) {
                dictionary.Add("Durability", block.GetDurability(value).ToString());
            }
            if (block.DefaultExperienceCount > 0f) {
                dictionary.Add("Experience Orbs", block.DefaultExperienceCount.ToString(CultureInfo.InvariantCulture));
            }
            if (block.CanWear(value)) {
                ClothingData clothingData = block.GetClothingData(value);
                dictionary.Add("Can Be Dyed", clothingData.CanBeDyed ? LanguageControl.Yes : LanguageControl.No);
                dictionary.Add("Armor Protection", $"{(int)(clothingData.ArmorProtection * 100f)}%");
                dictionary.Add("Armor Durability", clothingData.Sturdiness.ToString(CultureInfo.InvariantCulture));
                dictionary.Add("Insulation", $"{clothingData.Insulation:0.0} clo");
                dictionary.Add("Movement Speed", $"{clothingData.MovementSpeedFactor * 100f:0}%");
            }
            if (GameManager.Project != null
                && block.BlockIndex > 0) {
                dictionary.Add("Dynamic Index", block.BlockIndex.ToString());
                dictionary.Add("Block Data", Terrain.ExtractData(value).ToString());
            }
            ModsManager.HookAction(
                "EditBlockDescriptionScreen",
                loader => {
#pragma warning disable CS0618
                    loader.EditBlockDescriptionScreen(dictionary);
#pragma warning restore CS0618
                    loader.EditBlockDescriptionScreen(dictionary, value);
                    return false;
                }
            );
            return dictionary;
        }

        public virtual void UpdateBlockProperties() {
            if (m_index >= 0
                && m_index < m_valuesList.Count) {
                int value = m_valuesList[m_index];
                int num = Terrain.ExtractContents(value);
                Block block = BlocksManager.Blocks[num];
                m_blockIconWidget.Value = value;
                m_nameWidget.Text = block.GetDisplayName(null, value);
                m_descriptionWidget.Text = block.GetDescription(value);
                m_propertyNames1Widget.Text = string.Empty;
                m_propertyValues1Widget.Text = string.Empty;
                m_propertyNames2Widget.Text = string.Empty;
                m_propertyValues2Widget.Text = string.Empty;
                Dictionary<string, string> blockProperties = GetBlockProperties(value);
                int num2 = 0;
                foreach (KeyValuePair<string, string> item in blockProperties) {
                    if (num2 < blockProperties.Count - blockProperties.Count / 2) {
                        LabelWidget propertyNames1Widget = m_propertyNames1Widget;
                        string keyText = LanguageControl.Get(fName, item.Key) ?? item.Key;
                        //if (String.IsNullOrEmpty(keyText)) keyText = item.Key;
                        propertyNames1Widget.Text = $"{propertyNames1Widget.Text}{keyText}:\n";
                        LabelWidget propertyValues1Widget = m_propertyValues1Widget;
                        propertyValues1Widget.Text = $"{propertyValues1Widget.Text}{item.Value}\n";
                    }
                    else {
                        LabelWidget propertyNames2Widget = m_propertyNames2Widget;
                        propertyNames2Widget.Text = $"{propertyNames2Widget.Text}{LanguageControl.Get(fName, item.Key)}:\n";
                        LabelWidget propertyValues2Widget = m_propertyValues2Widget;
                        propertyValues2Widget.Text = $"{propertyValues2Widget.Text}{item.Value}\n";
                    }
                    num2++;
                }
            }
        }
    }
}