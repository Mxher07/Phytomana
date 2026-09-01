using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentGui : Component, IUpdateable, IDrawable {
        public class ModalPanelAnimationData {
            public Widget NewWidget;

            public Widget OldWidget;

            public float Factor;
        }

        public class Message {
            public string LargeText;

            public string SmallText;

            public double StartTime;

            public float Duration;
        }

        public static string fName = "ComponentGui";

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSky m_subsystemSky;

        public SubsystemWeather m_subsystemWeather;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        public ComponentPlayer m_componentPlayer;

        public ContainerWidget m_leftControlsContainerWidget;

        public ContainerWidget m_rightControlsContainerWidget;

        public ContainerWidget m_moveContainerWidget;

        public ContainerWidget m_lookContainerWidget;

        public RectangleWidget m_moveRectangleWidget;

        public RectangleWidget m_lookRectangleWidget;

        public ContainerWidget m_moveRectangleContainerWidget;

        public ContainerWidget m_lookRectangleContainerWidget;

        public ContainerWidget m_movePadContainerWidget;

        public ContainerWidget m_lookPadContainerWidget;

        public ContainerWidget m_moveButtonsContainerWidget;

        public ContainerWidget m_modalPanelContainerWidget;

        public ContainerWidget m_largeMessageWidget;

        public MessageWidget m_messageWidget;

        public ButtonWidget m_backButtonWidget;

        public ButtonWidget m_inventoryButtonWidget;

        public ButtonWidget m_clothingButtonWidget;

        public ButtonWidget m_moreButtonWidget;

        public Widget m_moreContentsWidget;

        public ButtonWidget m_lightningButtonWidget;

        public ButtonWidget m_precipitationButtonWidget;

        public ButtonWidget m_fogButtonWidget;

        public ButtonWidget m_photoButtonWidget;

        public ButtonWidget m_helpButtonWidget;

        public ButtonWidget m_timeOfDayButtonWidget;

        public ButtonWidget m_cameraButtonWidget;

        public ButtonWidget m_creativeFlyButtonWidget;

        public ButtonWidget m_crouchButtonWidget;

        public ButtonWidget m_mountButtonWidget;

        public ButtonWidget m_editItemButton;

        public float m_sidePanelsFactor;

        public ModalPanelAnimationData m_modalPanelAnimationData;

        public Message m_message;

        public KeyboardHelpDialog m_keyboardHelpDialog;

        public GamepadHelpDialog m_gamepadHelpDialog;

        public double m_lastMountableCreatureSearchTime;

        public bool m_keyboardHelpMessageShown;

        public bool m_gamepadHelpMessageShown;

        public static Func<Widget> OpenClothingWidget;

        public ComponentInput m_componentInput;

        public GameWidget GameWidget { get; set; }

        public ContainerWidget ControlsContainerWidget { get; set; }

        public TouchInputWidget ViewWidget { get; set; }

        public TouchInputWidget MoveWidget { get; set; }

        public MoveRoseWidget MoveRoseWidget { get; set; }

        public TouchInputWidget LookWidget { get; set; }

        public ShortInventoryWidget ShortInventoryWidget { get; set; }

        public ValueBarWidget HealthBarWidget { get; set; }

        public ValueBarWidget FoodBarWidget { get; set; }

        public ValueBarWidget TemperatureBarWidget { get; set; }

        public LabelWidget LevelLabelWidget { get; set; }

        public virtual bool ShowTouchWidget => m_componentPlayer.ComponentInput.IsControlledByTouch;

        public Widget ModalPanelWidget {
            get {
                if (m_modalPanelContainerWidget == null
                    || m_modalPanelContainerWidget.Children.Count <= 0) {
                    return null;
                }
                return m_modalPanelContainerWidget.Children[0];
            }
            set {
                if (value != ModalPanelWidget) {
                    if (m_modalPanelAnimationData != null) {
                        EndModalPanelAnimation();
                    }
                    m_modalPanelAnimationData = new ModalPanelAnimationData { OldWidget = ModalPanelWidget, NewWidget = value };
                    if (value != null) {
                        value.HorizontalAlignment = WidgetAlignment.Center;
                        m_modalPanelContainerWidget.Children.Insert(0, value);
                    }
                    UpdateModalPanelAnimation();
                    m_componentPlayer.GameWidget.Input.Clear();
                    m_componentPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(null, -1);
                    ModsManager.HookAction(
                        "OnModalPanelWidgetSet",
                        loader => {
                            loader.OnModalPanelWidgetSet(this, ModalPanelWidget, value);
                            return false;
                        }
                    );
                }
            }
        }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => [9];

        public virtual void DisplayLargeMessage(string largeText, string smallText, float duration, float delay) {
            m_message = new Message { LargeText = largeText, SmallText = smallText, Duration = duration, StartTime = Time.RealTime + delay };
        }

        public virtual void DisplaySmallMessage(string text, Color color, bool blinking, bool playNotificationSound) {
            MessageWidget.Message message = new(text, color, blinking);
            DisplaySmallMessage(message, playNotificationSound);
        }

        // ReSharper disable MethodOverloadWithOptionalParameter
        public virtual void DisplaySmallMessage(string text, Color color, bool blinking, bool playNotificationSound, float fontScale = 1f) {
            // ReSharper restore MethodOverloadWithOptionalParameter
            MessageWidget.Message message = new(text, color, blinking, fontScale);
            DisplaySmallMessage(message, playNotificationSound);
        }

        /// <summary>
        ///     模组如果需要自定义类型的DisplaySmallMessage，比如彩虹渐变色的Message，可以自己建立对应的Message类型，并调用这个方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="playNotificationSound"></param>
        public virtual void DisplaySmallMessage(MessageWidget.Message message, bool playNotificationSound) {
            m_messageWidget.DisplayMessage(message);
            if (playNotificationSound) {
                m_subsystemAudio.PlaySound("Audio/UI/Message", 1f, 0f, 0f, 0f);
            }
        }

        public virtual bool IsGameMenuDialogVisible() {
            foreach (Dialog dialog in DialogsManager.Dialogs) {
                if (dialog.ParentWidget == m_componentPlayer.GuiWidget
                    && dialog is GameMenuDialog) {
                    return true;
                }
            }
            return false;
        }

        public virtual void Update(float dt) {
            HandleInput();
            UpdateWidgets();
            ModsManager.HookAction(
                "GuiUpdate",
                modLoader => {
                    modLoader.GuiUpdate(this);
                    return false;
                }
            );
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_componentInput = Entity.FindComponent<ComponentInput>();
            GameWidget = m_componentPlayer.GameWidget;
            ContainerWidget guiWidget = m_componentPlayer.GuiWidget;
            m_backButtonWidget = guiWidget.Children.Find<ButtonWidget>("BackButton");
            m_inventoryButtonWidget = guiWidget.Children.Find<ButtonWidget>("InventoryButton");
            m_clothingButtonWidget = guiWidget.Children.Find<ButtonWidget>("ClothingButton");
            m_moreButtonWidget = guiWidget.Children.Find<ButtonWidget>("MoreButton");
            m_moreContentsWidget = guiWidget.Children.Find<Widget>("MoreContents");
            m_helpButtonWidget = guiWidget.Children.Find<ButtonWidget>("HelpButton");
            m_photoButtonWidget = guiWidget.Children.Find<ButtonWidget>("PhotoButton");
            m_lightningButtonWidget = guiWidget.Children.Find<ButtonWidget>("LightningButton");
            m_precipitationButtonWidget = guiWidget.Children.Find<ButtonWidget>("PrecipitationButton");
            m_fogButtonWidget = guiWidget.Children.Find<ButtonWidget>("FogButton");
            m_timeOfDayButtonWidget = guiWidget.Children.Find<ButtonWidget>("TimeOfDayButton");
            m_cameraButtonWidget = guiWidget.Children.Find<ButtonWidget>("CameraButton");
            m_creativeFlyButtonWidget = guiWidget.Children.Find<ButtonWidget>("CreativeFlyButton");
            m_crouchButtonWidget = guiWidget.Children.Find<ButtonWidget>("CrouchButton");
            m_mountButtonWidget = guiWidget.Children.Find<ButtonWidget>("MountButton");
            m_editItemButton = guiWidget.Children.Find<ButtonWidget>("EditItemButton");
            MoveWidget = guiWidget.Children.Find<TouchInputWidget>("Move");
            MoveRoseWidget = guiWidget.Children.Find<MoveRoseWidget>("MoveRose");
            LookWidget = guiWidget.Children.Find<TouchInputWidget>("Look");
            ViewWidget = m_componentPlayer.ViewWidget;
            HealthBarWidget = guiWidget.Children.Find<ValueBarWidget>("HealthBar");
            FoodBarWidget = guiWidget.Children.Find<ValueBarWidget>("FoodBar");
            TemperatureBarWidget = guiWidget.Children.Find<ValueBarWidget>("TemperatureBar");
            LevelLabelWidget = guiWidget.Children.Find<LabelWidget>("LevelLabel");
            m_modalPanelContainerWidget = guiWidget.Children.Find<ContainerWidget>("ModalPanelContainer");
            ControlsContainerWidget = guiWidget.Children.Find<ContainerWidget>("ControlsContainer");
            m_leftControlsContainerWidget = guiWidget.Children.Find<ContainerWidget>("LeftControlsContainer");
            m_rightControlsContainerWidget = guiWidget.Children.Find<ContainerWidget>("RightControlsContainer");
            m_moveContainerWidget = guiWidget.Children.Find<ContainerWidget>("MoveContainer");
            m_lookContainerWidget = guiWidget.Children.Find<ContainerWidget>("LookContainer");
            m_moveRectangleWidget = guiWidget.Children.Find<RectangleWidget>("MoveRectangle");
            m_lookRectangleWidget = guiWidget.Children.Find<RectangleWidget>("LookRectangle");
            m_moveRectangleContainerWidget = guiWidget.Children.Find<ContainerWidget>("MoveRectangleContainer");
            m_lookRectangleContainerWidget = guiWidget.Children.Find<ContainerWidget>("LookRectangleContainer");
            m_moveRectangleWidget = guiWidget.Children.Find<RectangleWidget>("MoveRectangle");
            m_lookRectangleWidget = guiWidget.Children.Find<RectangleWidget>("LookRectangle");
            m_movePadContainerWidget = guiWidget.Children.Find<ContainerWidget>("MovePadContainer");
            m_lookPadContainerWidget = guiWidget.Children.Find<ContainerWidget>("LookPadContainer");
            m_moveButtonsContainerWidget = guiWidget.Children.Find<ContainerWidget>("MoveButtonsContainer");
            ShortInventoryWidget = guiWidget.Children.Find<ShortInventoryWidget>("ShortInventory");
            m_largeMessageWidget = guiWidget.Children.Find<ContainerWidget>("LargeMessage");
            m_messageWidget = guiWidget.Children.Find<MessageWidget>("Message");
            m_keyboardHelpMessageShown = valuesDictionary.GetValue<bool>("KeyboardHelpMessageShown");
            m_gamepadHelpMessageShown = valuesDictionary.GetValue<bool>("GamepadHelpMessageShown");
            WorldSettings worldSettings = m_subsystemGameInfo.WorldSettings;
            GameMode gameMode = worldSettings.GameMode;
            bool isCreative = gameMode == GameMode.Creative;
            m_creativeFlyButtonWidget.IsVisible = isCreative;
            m_timeOfDayButtonWidget.IsVisible = isCreative;
            m_lightningButtonWidget.IsVisible = isCreative;
            m_precipitationButtonWidget.IsVisible = isCreative && worldSettings.AreWeatherEffectsEnabled;
            m_fogButtonWidget.IsVisible = isCreative && worldSettings.AreWeatherEffectsEnabled;
            //启动冒险模式
            bool adventureEnabled = worldSettings.AreAdventureSurvivalMechanicsEnabled;
            HealthBarWidget.IsVisible = !isCreative;
            FoodBarWidget.IsVisible = !isCreative && adventureEnabled;
            TemperatureBarWidget.IsVisible = !isCreative && adventureEnabled;
            LevelLabelWidget.IsVisible = !isCreative && adventureEnabled;
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("KeyboardHelpMessageShown", m_keyboardHelpMessageShown);
            valuesDictionary.SetValue("GamepadHelpMessageShown", m_gamepadHelpMessageShown);
        }

        public override void OnEntityAdded() {
            ShortInventoryWidget.AssignComponents(m_componentPlayer.ComponentMiner.Inventory);
        }

        public override void OnEntityRemoved() {
            ShortInventoryWidget.AssignComponents(null);
            m_message = null;
        }

        public override void Dispose() {
            ModalPanelWidget = null;
            m_keyboardHelpDialog = null;
            if (ShortInventoryWidget != null) {
                ShortInventoryWidget.AssignComponents(null);
            }
        }

        public virtual void UpdateSidePanelsAnimation() {
            float num = MathUtils.Min(Time.FrameDuration, 0.1f);
            bool showModelWidget = ModalPanelWidget != null && (m_modalPanelAnimationData == null || m_modalPanelAnimationData.NewWidget != null);
            float targetSidePanelsFactor = (ShowTouchWidget | showModelWidget) ? 0 : 1;
            float x = targetSidePanelsFactor - m_sidePanelsFactor;
            if (MathF.Abs(x) > 0.01f) {
                m_sidePanelsFactor += Math.Clamp(12f * MathUtils.PowSign(x, 0.75f) * num, 0f - MathF.Abs(x), MathF.Abs(x));
            }
            else {
                m_sidePanelsFactor = targetSidePanelsFactor;
            }
            m_leftControlsContainerWidget.RenderTransform = Matrix.CreateTranslation(
                (m_leftControlsContainerWidget.ActualSize.X + m_componentPlayer.GuiWidget.MarginLeft / ScreensManager.FinalUiScale) * (0f - m_sidePanelsFactor),
                0f,
                0f
            );
            m_rightControlsContainerWidget.RenderTransform = Matrix.CreateTranslation(
                (m_rightControlsContainerWidget.ActualSize.X + m_componentPlayer.GuiWidget.MarginRight / ScreensManager.FinalUiScale) * m_sidePanelsFactor,
                0f,
                0f
            );
        }

        public virtual void UpdateModalPanelAnimation() {
            m_modalPanelAnimationData.Factor += 6f * MathUtils.Min(Time.FrameDuration, 0.1f);
            if (m_modalPanelAnimationData.Factor < 1f) {
                float factor = m_modalPanelAnimationData.Factor;
                float num = 0.5f + 0.5f * MathF.Pow(1f - factor, 0.1f);
                float num2 = 0.5f + 0.5f * MathF.Pow(factor, 0.1f);
                float s = 1f - factor;
                float s2 = factor;
                if (m_modalPanelAnimationData.OldWidget != null) {
                    Vector2 actualSize = m_modalPanelAnimationData.OldWidget.ActualSize;
                    m_modalPanelAnimationData.OldWidget.ColorTransform = Color.White * s;
                    m_modalPanelAnimationData.OldWidget.RenderTransform =
                        Matrix.CreateTranslation((0f - actualSize.X) / 2f, (0f - actualSize.Y) / 2f, 0f)
                        * Matrix.CreateScale(num, num, 1f)
                        * Matrix.CreateTranslation(actualSize.X / 2f, actualSize.Y / 2f, 0f);
                }
                if (m_modalPanelAnimationData.NewWidget != null) {
                    Vector2 actualSize2 = m_modalPanelAnimationData.NewWidget.ActualSize;
                    m_modalPanelAnimationData.NewWidget.ColorTransform = Color.White * s2;
                    m_modalPanelAnimationData.NewWidget.RenderTransform =
                        Matrix.CreateTranslation((0f - actualSize2.X) / 2f, (0f - actualSize2.Y) / 2f, 0f)
                        * Matrix.CreateScale(num2, num2, 1f)
                        * Matrix.CreateTranslation(actualSize2.X / 2f, actualSize2.Y / 2f, 0f);
                }
            }
            else {
                EndModalPanelAnimation();
            }
        }

        public virtual void EndModalPanelAnimation() {
            if (m_modalPanelAnimationData.OldWidget != null) {
                m_modalPanelContainerWidget.Children.Remove(m_modalPanelAnimationData.OldWidget);
            }
            if (m_modalPanelAnimationData.NewWidget != null) {
                m_modalPanelAnimationData.NewWidget.ColorTransform = Color.White;
                m_modalPanelAnimationData.NewWidget.RenderTransform = Matrix.Identity;
            }
            m_modalPanelAnimationData = null;
        }

        public virtual void UpdateWidgets() {
            ComponentRider componentRider = m_componentPlayer.ComponentRider;
            ComponentSleep componentSleep = m_componentPlayer.ComponentSleep;
            //ComponentInput componentInput = m_componentPlayer.ComponentInput;
            //WorldSettings worldSettings = m_subsystemGameInfo.WorldSettings;
            //GameMode gameMode = worldSettings.GameMode;
            UpdateSidePanelsAnimation();
            if (m_modalPanelAnimationData != null) {
                UpdateModalPanelAnimation();
            }
            if (m_message != null) {
                double realTime = Time.RealTime;
                m_largeMessageWidget.IsVisible = true;
                LabelWidget labelWidget = m_largeMessageWidget.Children.Find<LabelWidget>("LargeLabel");
                LabelWidget labelWidget2 = m_largeMessageWidget.Children.Find<LabelWidget>("SmallLabel");
                labelWidget.Text = m_message.LargeText;
                labelWidget2.Text = m_message.SmallText;
                labelWidget.IsVisible = !string.IsNullOrEmpty(m_message.LargeText);
                labelWidget2.IsVisible = !string.IsNullOrEmpty(m_message.SmallText);
                float num = (float)Math.Min(
                    MathUtils.Saturate(2.0 * (realTime - m_message.StartTime)),
                    MathUtils.Saturate(2.0 * (m_message.StartTime + m_message.Duration - realTime))
                );
                labelWidget.Color = new Color(num, num, num, num);
                labelWidget2.Color = new Color(num, num, num, num);
                if (Time.RealTime > m_message.StartTime + m_message.Duration) {
                    m_message = null;
                }
            }
            else {
                m_largeMessageWidget.IsVisible = false;
            }
            ControlsContainerWidget.IsVisible = m_componentPlayer.PlayerData.IsReadyForPlaying
                && m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled
                && componentSleep.SleepFactor <= 0f;
            m_moveRectangleContainerWidget.IsVisible = !SettingsManager.HideMoveLookPads && ShowTouchWidget;
            m_lookRectangleContainerWidget.IsVisible = !SettingsManager.HideMoveLookPads && ShowTouchWidget && (SettingsManager.LookControlMode == LookControlMode.Pad || SettingsManager.MoveControlMode != MoveControlMode.Buttons);
            //m_lookPadContainerWidget.IsVisible = SettingsManager.LookControlMode != LookControlMode.SplitTouch;
            MoveRoseWidget.IsVisible = ShowTouchWidget;
            m_moreContentsWidget.IsVisible = m_moreButtonWidget.IsChecked;
            m_moveButtonsContainerWidget.IsVisible = SettingsManager.MoveControlMode == MoveControlMode.Buttons;
            m_movePadContainerWidget.IsVisible = SettingsManager.MoveControlMode == MoveControlMode.Pad;
            if (SettingsManager.LeftHandedLayout) {
                m_moveContainerWidget.HorizontalAlignment = WidgetAlignment.Far;
                m_lookContainerWidget.HorizontalAlignment = WidgetAlignment.Near;
                m_moveRectangleWidget.FlipHorizontal = true;
                m_lookRectangleWidget.FlipHorizontal = false;
            }
            else {
                m_moveContainerWidget.HorizontalAlignment = WidgetAlignment.Near;
                m_lookContainerWidget.HorizontalAlignment = WidgetAlignment.Far;
                m_moveRectangleWidget.FlipHorizontal = false;
                m_lookRectangleWidget.FlipHorizontal = true;
            }
            m_moveContainerWidget.Margin = new Vector2(SettingsManager.MoveWidgetMarginX, SettingsManager.MoveWidgetMarginY)
                * ControlsContainerWidget.ActualSize;
            m_precipitationButtonWidget.IsChecked = m_subsystemWeather.IsPrecipitationStarted;
            m_fogButtonWidget.IsChecked = m_subsystemWeather.IsFogStarted;
            m_crouchButtonWidget.IsChecked = m_componentPlayer.ComponentBody.TargetCrouchFactor > 0f;
            m_creativeFlyButtonWidget.IsChecked = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
            m_inventoryButtonWidget.IsChecked = IsInventoryVisible();
            m_clothingButtonWidget.IsChecked = IsClothingVisible();
            if (IsActiveSlotEditable()
                || m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.HasValue) {
                m_crouchButtonWidget.IsVisible = false;
                m_mountButtonWidget.IsVisible = false;
                m_editItemButton.IsVisible = true;
            }
            else if (componentRider != null
                && componentRider.Mount != null) {
                m_crouchButtonWidget.IsVisible = false;
                m_mountButtonWidget.IsChecked = true;
                m_mountButtonWidget.IsVisible = true;
                m_editItemButton.IsVisible = false;
            }
            else {
                m_mountButtonWidget.IsChecked = false;
                if (componentRider != null
                    && Time.FrameStartTime - m_lastMountableCreatureSearchTime > 0.5) {
                    m_lastMountableCreatureSearchTime = Time.FrameStartTime;
                    if (componentRider.FindNearestMount() != null) {
                        m_crouchButtonWidget.IsVisible = false;
                        m_mountButtonWidget.IsVisible = true;
                        m_editItemButton.IsVisible = false;
                    }
                    else {
                        m_crouchButtonWidget.IsVisible = true;
                        m_mountButtonWidget.IsVisible = false;
                        m_editItemButton.IsVisible = false;
                    }
                }
            }
            if (!m_componentPlayer.IsAddedToProject
                || m_componentPlayer.ComponentHealth.Health == 0f
                || componentSleep.IsSleeping
                || m_componentPlayer.ComponentSickness.IsPuking) {
                ModalPanelWidget = null;
            }
            if (m_componentPlayer.ComponentSickness.IsSick) {
                m_componentPlayer.ComponentGui.HealthBarWidget.LitBarColor = new Color(166, 175, 103);
            }
            else {
                m_componentPlayer.ComponentGui.HealthBarWidget.LitBarColor = m_componentPlayer.ComponentFlu.HasFlu
                    ? new Color(0, 48, 255)
                    : new Color(224, 24, 0);
            }
        }

        public virtual void HandleInput() {
            WidgetInput input = m_componentPlayer.GameWidget.Input;
            PlayerInput playerInput = m_componentPlayer.ComponentInput.PlayerInput;
            ComponentRider componentRider = m_componentPlayer.ComponentRider;
            if (m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled) {
                if (!m_keyboardHelpMessageShown
                    && (m_componentPlayer.PlayerData.InputDevice & WidgetInputDevice.Keyboard) != 0
                    && Time.PeriodicEvent(7.0, 0.0)) {
                    m_keyboardHelpMessageShown = true;
                    DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, true);
                }
                else if (!m_gamepadHelpMessageShown
                    && (m_componentPlayer.PlayerData.InputDevice & WidgetInputDevice.Gamepads) != 0
                    && Time.PeriodicEvent(7.0, 0.0)) {
                    m_gamepadHelpMessageShown = true;
                    DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, true, true);
                }
            }
            if (playerInput.KeyboardHelp) {
                ScreensManager.SwitchScreen("KeyboardMapping");
            }
            if (playerInput.GamepadHelp) {
                ScreensManager.SwitchScreen("GamepadMapping");
            }
            if (m_helpButtonWidget.IsClicked) {
                ScreensManager.SwitchScreen("Help");
            }
            if (playerInput.ToggleInventory
                || m_inventoryButtonWidget.IsClicked) {
                if (IsInventoryVisible()) {
                    ModalPanelWidget = null;
                }
                else {
                    ModalPanelWidget = m_componentPlayer.ComponentMiner.Inventory is ComponentCreativeInventory
                        ? new CreativeInventoryWidget(m_componentPlayer.Entity)
                        : new FullInventoryWidget(
                            m_componentPlayer.ComponentMiner.Inventory,
                            m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(true)
                        );
                }
            }
            if (playerInput.ToggleClothing
                || m_clothingButtonWidget.IsClicked) {
                if (IsClothingVisible()) {
                    ModalPanelWidget = null;
                }
                else {
                    ClothingWidget clothingWidget = new(m_componentPlayer);
                    ModsManager.HookAction(
                        "ClothingWidgetOpen",
                        modLoader => {
                            modLoader.ClothingWidgetOpen(this, clothingWidget);
                            return false;
                        }
                    );
                    ModalPanelWidget = clothingWidget;
                }
            }
            if (m_crouchButtonWidget.IsClicked
                || playerInput.ToggleCrouch) {
                if (m_componentPlayer.ComponentBody.TargetCrouchFactor == 0f) {
                    if (m_componentPlayer.ComponentBody.StandingOnValue.HasValue) {
                        m_componentPlayer.ComponentBody.TargetCrouchFactor = 1f;
                        DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, false, false);
                    }
                }
                else {
                    m_componentPlayer.ComponentBody.TargetCrouchFactor = 0f;
                    DisplaySmallMessage(LanguageControl.Get(fName, 4), Color.White, false, false);
                }
            }
            if (componentRider != null
                && (m_mountButtonWidget.IsClicked || playerInput.ToggleMount)) {
                bool flag = componentRider.Mount != null;
                if (flag) {
                    componentRider.StartDismounting();
                }
                else {
                    ComponentMount componentMount = componentRider.FindNearestMount();
                    if (componentMount != null) {
                        componentRider.StartMounting(componentMount);
                    }
                }
                if (componentRider.Mount != null != flag) {
                    DisplaySmallMessage(
                        componentRider.Mount != null ? LanguageControl.Get(fName, 5) : LanguageControl.Get(fName, 6),
                        Color.White,
                        false,
                        false
                    );
                }
            }
            if ((m_editItemButton.IsClicked || playerInput.EditItem)
                && m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.HasValue) {
                Point3 value = m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.Value;
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(value.X, value.Y, value.Z);
                SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(cellValue));
                for (int i = 0;
                    i < blockBehaviors.Length && !blockBehaviors[i].OnEditBlock(value.X, value.Y, value.Z, cellValue, m_componentPlayer);
                    i++) { }
            }
            else if ((m_editItemButton.IsClicked || playerInput.EditItem)
                && IsActiveSlotEditable()) {
                IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
                if (inventory != null) {
                    int activeSlotIndex = inventory.ActiveSlotIndex;
                    int value = inventory.GetSlotValue(activeSlotIndex);
                    int num = Terrain.ExtractContents(value);
                    if (BlocksManager.Blocks[num].IsEditable_(value)) {
                        SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(num));
                        for (int i = 0;
                            i < blockBehaviors.Length && !blockBehaviors[i].OnEditInventoryItem(inventory, activeSlotIndex, m_componentPlayer);
                            i++) { }
                    }
                }
            }
            if (m_creativeFlyButtonWidget.IsVisible
                && (m_creativeFlyButtonWidget.IsClicked || playerInput.ToggleCreativeFly)
                && componentRider.Mount == null) {
                bool isCreativeFlyEnabled = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
                m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled = !isCreativeFlyEnabled;
                if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled != isCreativeFlyEnabled) {
                    if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled) {
                        m_componentPlayer.ComponentLocomotion.JumpOrder = 1f;
                        DisplaySmallMessage(LanguageControl.Get(fName, 7), Color.White, false, false);
                    }
                    else {
                        DisplaySmallMessage(LanguageControl.Get(fName, 8), Color.White, false, false);
                    }
                }
            }
            if (m_cameraButtonWidget.IsClicked
                || playerInput.SwitchCameraMode) {
                ModsManager.HookAction(
                    "OnCameraChange",
                    modLoader => {
                        modLoader.OnCameraChange(m_componentPlayer, this);
                        return false;
                    }
                );
            }
            if (m_photoButtonWidget.IsClicked
                || playerInput.TakeScreenshot) {
                ScreenCaptureManager.CapturePhoto(
                    delegate { DisplaySmallMessage(LanguageControl.Get(fName, 13), Color.White, false, false); },
                    delegate { DisplaySmallMessage(LanguageControl.Get(fName, 14), Color.White, false, false); }
                );
            }
            if (m_lightningButtonWidget.IsVisible
                && (m_lightningButtonWidget.IsClicked || playerInput.Lighting)) {
                Matrix matrix = Matrix.CreateFromQuaternion(m_componentPlayer.ComponentCreatureModel.EyeRotation);
                m_subsystemWeather.ManualLightingStrike(m_componentPlayer.ComponentCreatureModel.EyePosition, matrix.Forward);
            }
            if (m_precipitationButtonWidget.IsVisible
                && (m_precipitationButtonWidget.IsClicked || playerInput.Precipitation)) {
                if (m_subsystemWeather.IsPrecipitationStarted) {
                    m_subsystemWeather.ManualPrecipitationEnd();
                    DisplaySmallMessage(LanguageControl.Get(fName, 20), Color.White, false, false);
                }
                else {
                    m_subsystemWeather.ManualPrecipitationStart();
                    DisplaySmallMessage(LanguageControl.Get(fName, 21), Color.White, false, false);
                }
            }
            if (m_fogButtonWidget.IsVisible
                && (m_fogButtonWidget.IsClicked || playerInput.Fog)) {
                if (m_subsystemWeather.IsFogStarted) {
                    m_subsystemWeather.ManualFogEnd();
                    DisplaySmallMessage(LanguageControl.Get(fName, 22), Color.White, false, false);
                }
                else {
                    m_subsystemWeather.ManualFogStart();
                    DisplaySmallMessage(LanguageControl.Get(fName, 23), Color.White, false, false);
                }
            }
            if (m_timeOfDayButtonWidget.IsVisible
                && (m_timeOfDayButtonWidget.IsClicked || playerInput.TimeOfDay)) {
                float num2 = IntervalUtils.Interval(m_subsystemTimeOfDay.TimeOfDay, m_subsystemTimeOfDay.Middawn);
                float num3 = IntervalUtils.Interval(m_subsystemTimeOfDay.TimeOfDay, m_subsystemTimeOfDay.Midday);
                float num4 = IntervalUtils.Interval(m_subsystemTimeOfDay.TimeOfDay, m_subsystemTimeOfDay.Middusk);
                float num5 = IntervalUtils.Interval(m_subsystemTimeOfDay.TimeOfDay, m_subsystemTimeOfDay.Midnight);
                float num6 = MathUtils.Min(num2, num3, num4, num5);
                if (num2 == num6) {
                    m_subsystemTimeOfDay.TimeOfDayOffset += num2;
                    DisplaySmallMessage(LanguageControl.Get(fName, 15), Color.White, false, false);
                }
                else if (num3 == num6) {
                    m_subsystemTimeOfDay.TimeOfDayOffset += num3;
                    DisplaySmallMessage(LanguageControl.Get(fName, 16), Color.White, false, false);
                }
                else if (num4 == num6) {
                    m_subsystemTimeOfDay.TimeOfDayOffset += num4;
                    DisplaySmallMessage(LanguageControl.Get(fName, 17), Color.White, false, false);
                }
                else if (num5 == num6) {
                    m_subsystemTimeOfDay.TimeOfDayOffset += num5;
                    DisplaySmallMessage(LanguageControl.Get(fName, 18), Color.White, false, false);
                }
            }
            if (ModalPanelWidget != null) {
                if (input.Cancel
                    || input.Back
                    || m_backButtonWidget.IsClicked) {
                    ModalPanelWidget = null;
                }
            }
            else if (input.Back
                || m_backButtonWidget.IsClicked) {
                DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new GameMenuDialog(m_componentPlayer));
            }
        }

        public virtual bool IsClothingVisible() => ModalPanelWidget is ClothingWidget;

        public virtual bool IsInventoryVisible() {
            if (ModalPanelWidget != null) {
                return !IsClothingVisible();
            }
            return false;
        }

        public virtual bool IsActiveSlotEditable() {
            IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
            if (inventory != null) {
                int activeSlotIndex = inventory.ActiveSlotIndex;
                int value = inventory.GetSlotValue(activeSlotIndex);
                int num = Terrain.ExtractContents(value);
                if (BlocksManager.Blocks[num].IsEditable_(value)) {
                    return true;
                }
            }
            return false;
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            ModsManager.HookAction(
                "GuiDraw",
                modloader => {
                    modloader.GuiDraw(this, camera, drawOrder);
                    return false;
                }
            );
        }
    }
}