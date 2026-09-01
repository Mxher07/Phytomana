using System.Xml.Linq;
using Engine;

namespace Game {
    public class CameraManageScreen : Screen {
        public Widget KeyInfoWidget(object item) {
            XElement node = ContentManager.Get<XElement>("Widgets/KeyboardMappingItem");
            ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node, null);
            LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("Name");
            LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BoundKey");
            bool enable = SettingsManager.GetCameraManageSetting(item.ToString()) >= 0;
            labelWidget.Text = LanguageControl.Get("CameraManage", item.ToString());
            labelWidget2.Text = LanguageControl.Get("ContainerWidget", "CameraManageScreen", enable ? "Enabled" : "Disabled");
            labelWidget2.Color = enable ? Color.White : Color.Gray;
            m_widgetsByString[item.ToString()] = containerWidget;
            return containerWidget;
        }

        public ListPanelWidget m_camerasList;
        public BevelledButtonWidget m_enableButton;
        public BevelledButtonWidget m_disableButton;
        public BevelledButtonWidget m_upButton;
        public BevelledButtonWidget m_downButton;
        public BevelledButtonWidget m_resetButton;
        public Dictionary<string, ContainerWidget> m_widgetsByString = new();

        public static int EnabledCamerasCount => ModSettingsManager.CombinedCameraManageSettings.Count(item => Convert.ToInt32(item.Value) >= 0);

        public CameraManageScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/CameraManageScreen");
            LoadContents(this, node);
            m_camerasList = Children.Find<ListPanelWidget>("KeysList");
            m_camerasList.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(m_camerasList.ItemWidgetFactory, KeyInfoWidget);
            m_camerasList.ScrollPosition = 0f;
            m_camerasList.ScrollSpeed = 0f;
            m_camerasList.ItemClicked += item => {
                if (m_camerasList.SelectedItem == item) {
                    m_camerasList.SelectedItem = null;
                }
                else {
                    m_camerasList.SelectedItem = item;
                }
            };
            m_enableButton = Children.Find<BevelledButtonWidget>("EnableCamera");
            m_disableButton = Children.Find<BevelledButtonWidget>("DisableCamera");
            m_upButton = Children.Find<BevelledButtonWidget>("Up");
            m_downButton = Children.Find<BevelledButtonWidget>("Down");
            m_resetButton = Children.Find<BevelledButtonWidget>("Reset");
        }

        public override void Update() {
            string selectedCameraName = m_camerasList.SelectedItem?.ToString() ?? string.Empty;
            int enabledCount = EnabledCamerasCount;
            int selectedItemValue = string.IsNullOrEmpty(selectedCameraName) ? -2 : SettingsManager.GetCameraManageSetting(selectedCameraName);
            m_enableButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue < 0;
            m_disableButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName)
                && selectedItemValue >= 0
                && selectedCameraName != "Game.FppCamera"
                && enabledCount > 2; //至少保留2个摄像机，当现存小于等于2个时无法点击禁用按钮
            m_upButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue > 0;
            m_downButton.IsEnabled = !string.IsNullOrEmpty(selectedCameraName) && selectedItemValue >= 0 && selectedItemValue < enabledCount - 1;
            foreach (string key in m_widgetsByString.Keys) {
                LabelWidget labelWidget = m_widgetsByString[key].Children.Find<LabelWidget>("BoundKey");
                bool enable = SettingsManager.GetCameraManageSetting(key) >= 0;
                //labelWidget.Text = SettingsManager.GetCameraManageSetting(key).ToString();
                labelWidget.Text = LanguageControl.Get("ContentWidgets", "CameraManageScreen", enable ? "Enabled" : "Disabled");
                labelWidget.Color = enable ? Color.White : Color.Gray;
            }
            if (m_disableButton.IsClicked) {
                SettingsManager.SetCameraManageSetting(selectedCameraName, -1); //-1表示禁用
                RefreshList();
            }
            if (m_enableButton.IsClicked) {
                SettingsManager.SetCameraManageSetting(selectedCameraName, enabledCount); //将禁用的启用后自动放在最后面
                RefreshList();
                m_camerasList.SelectedIndex = ModSettingsManager.CombinedCameraManageSettings.Count - 1; //列表自动选中最后一个
            }
            if (m_upButton.IsClicked) {
                foreach (KeyValuePair<string, int> item in ModSettingsManager.CombinedCameraManageSettings) { //找到选中摄像机的上一个并将其序号进行替换
                    string key = item.Key;
                    if (SettingsManager.GetCameraManageSetting(key) == selectedItemValue - 1) {
                        SettingsManager.SetCameraManageSetting(key, selectedItemValue);
                        break;
                    }
                }
                SettingsManager.SetCameraManageSetting(selectedCameraName, selectedItemValue - 1);
                int i = m_camerasList.SelectedIndex ?? 1;
                RefreshList();
                m_camerasList.SelectedIndex = i - 1; //刷新列表后重新选中
            }
            if (m_downButton.IsClicked) {
                foreach (KeyValuePair<string, int> item in ModSettingsManager.CombinedCameraManageSettings) { //找到选中摄像机的下一个并将其序号进行替换
                    string key = item.Key;
                    if (SettingsManager.GetCameraManageSetting(key) == selectedItemValue + 1) {
                        SettingsManager.SetCameraManageSetting(key, selectedItemValue);
                        break;
                    }
                }
                SettingsManager.SetCameraManageSetting(selectedCameraName, selectedItemValue + 1);
                int i = m_camerasList.SelectedIndex ?? -1;
                RefreshList();
                m_camerasList.SelectedIndex = i + 1; //刷新列表后重新选中
            }
            if (m_resetButton.IsClicked) {
                MessageDialog dialog = new(
                    LanguageControl.Get("ContentWidgets", "CameraManageScreen", "ResetTitle"),
                    LanguageControl.Get("ContentWidgets", "CameraManageScreen", "ResetText"),
                    LanguageControl.Yes,
                    LanguageControl.No,
                    delegate(MessageDialogButton button) {
                        if (button == MessageDialogButton.Button1) {
                            ResetAll();
                        }
                    }
                );
                DialogsManager.ShowDialog(null, dialog);
            }
            if (Children.Find<ButtonWidget>("TopBar.Back").IsClicked
                || Input.Back
                || Input.Cancel) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }

        public override void Enter(object[] parameters) {
            RefreshList();
        }

        public void ResetAll() {
            SettingsManager.InitializeCameraManageSettings();
            ModSettingsManager.ResetModsCameraManageSettings();
            RefreshList();
        }

        void RefreshList() {
            m_camerasList.ClearItems();
            List<KeyValuePair<string, int>> list = ModSettingsManager.CombinedCameraManageSettings.OrderBy(x => x.Value).ToList();
            int num = 0;
            foreach (KeyValuePair<string, int> item in list) {
                string name = item.Key;
                m_camerasList.AddItem(name);
                int value = item.Value;
                if (value >= 0) { //刷新列表时重新按顺序分配值，避免出现空缺
                    SettingsManager.SetCameraManageSetting(name, num);
                    num++;
                }
            }
        }
    }
}