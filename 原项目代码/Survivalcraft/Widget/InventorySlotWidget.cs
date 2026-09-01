using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;

namespace Game {
    public class InventorySlotWidget : CanvasWidget, IDragTargetWidget {
        public BevelledRectangleWidget m_rectangleWidget;

        public RectangleWidget m_highlightWidget;

        public BlockIconWidget m_blockIconWidget;

        public LabelWidget m_countWidget;

        public ValueBarWidget m_healthBarWidget;

        public RectangleWidget m_editOverlayWidget;

        public RectangleWidget m_interactiveOverlayWidget;

        public RectangleWidget m_foodOverlayWidget;

        public LabelWidget m_splitLabelWidget;

        public GameWidget m_gameWidget;

        public DragHostWidget m_dragHostWidget;

        public IInventory m_inventory;

        public int m_slotIndex;

        public DragMode? m_dragMode;

        public bool m_focus;

        public int m_lastCount = -1;

        public InventoryDragData m_inventoryDragData;

        public SubsystemTerrain m_subsystemTerrain;

        public ComponentPlayer m_componentPlayer;

        public Entity m_entity;

        public const string fName = "InventorySlotWidget";

        public virtual bool HideBlockIcon { get; set; }

        public virtual bool HideEditOverlay { get; set; }

        public virtual bool HideInteractiveOverlay { get; set; }

        public virtual bool HideFoodOverlay { get; set; }

        public virtual bool HideHighlightRectangle { get; set; }

        public virtual bool HideHealthBar { get; set; }

        public virtual bool ProcessingOnly { get; set; }

        public virtual Color CenterColor {
            get => m_rectangleWidget.CenterColor;
            set => m_rectangleWidget.CenterColor = value;
        }

        public virtual Color BevelColor {
            get => m_rectangleWidget.BevelColor;
            set => m_rectangleWidget.BevelColor = value;
        }

        public virtual Matrix? CustomViewMatrix {
            get => m_blockIconWidget.CustomViewMatrix;
            set => m_blockIconWidget.CustomViewMatrix = value;
        }

        public virtual GameWidget GameWidget {
            get {
                if (m_gameWidget == null) {
                    for (ContainerWidget parentWidget = ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget) {
                        if (parentWidget is GameWidget gameWidget) {
                            m_gameWidget = gameWidget;
                            break;
                        }
                        // In VR mode GuiWidget is reparented to VrGuiRootWidget (detached
                        // from GameWidget.Children), so walk up stops at VrGuiRootWidget
                        // without finding GameWidget. Use the stored reference instead.
                        if (parentWidget is VrGuiRootWidget vrRoot && vrRoot.OwnerGameWidget != null) {
                            m_gameWidget = vrRoot.OwnerGameWidget;
                            break;
                        }
                    }
                }
                return m_gameWidget;
            }
        }

        public virtual DragHostWidget DragHostWidget {
            get {
                m_dragHostWidget ??= GameWidget?.FindChild<DragHostWidget>(false);
                return m_dragHostWidget;
            }
        }

        public InventorySlotWidget() {
            Size = new Vector2(72f, 72f);
            List<Widget> list = new();
            //格子边框
            m_rectangleWidget = new BevelledRectangleWidget { BevelSize = -2f, DirectionalLight = 0.15f, CenterColor = Color.Transparent };
            list.Add(m_rectangleWidget);
            //格子背景色
            m_highlightWidget = new RectangleWidget { FillColor = Color.Transparent, OutlineColor = Color.Transparent };
            list.Add(m_highlightWidget);
            //方块图标
            m_blockIconWidget = new BlockIconWidget {
                HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center, Margin = new Vector2(2f, 2f)
            };
            list.Add(m_blockIconWidget);
            //方块数量标志
            m_countWidget = new LabelWidget {
                FontScale = 1f, HorizontalAlignment = WidgetAlignment.Far, VerticalAlignment = WidgetAlignment.Far, Margin = new Vector2(6f, 2f)
            };
            list.Add(m_countWidget);
            //耐久条
            m_healthBarWidget = new ValueBarWidget {
                LayoutDirection = LayoutDirection.Vertical,
                HorizontalAlignment = WidgetAlignment.Near,
                VerticalAlignment = WidgetAlignment.Far,
                BarsCount = 3,
                FlipDirection = true,
                LitBarColor = new Color(32, 128, 0),
                UnlitBarColor = new Color(24, 24, 24, 64),
                BarSize = new Vector2(12f, 12f),
                BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/ProgressBar"),
                Margin = new Vector2(4f, 4f)
            };
            list.Add(m_healthBarWidget);
            //右上角显示物品的编辑、交互、腐烂信息的面板
            StackPanelWidget stackPanelWidget = new() {
                Direction = LayoutDirection.Horizontal, HorizontalAlignment = WidgetAlignment.Far, Margin = new Vector2(3f, 3f)
            };
            //标记可交互方块的手标记
            m_interactiveOverlayWidget = new RectangleWidget {
                Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/InteractiveItemOverlay"),
                Size = new Vector2(13f, 14f),
                FillColor = new Color(160, 160, 160),
                OutlineColor = Color.Transparent
            };
            stackPanelWidget.Children.Add(m_interactiveOverlayWidget);
            //标记可编辑方块的编辑标记
            m_editOverlayWidget = new RectangleWidget {
                Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/EditItemOverlay"),
                Size = new Vector2(12f, 14f),
                FillColor = new Color(160, 160, 160),
                OutlineColor = Color.Transparent
            };
            stackPanelWidget.Children.Add(m_editOverlayWidget);
            //标记可腐烂方块的食物标记
            m_foodOverlayWidget = new RectangleWidget {
                Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/FoodItemOverlay"),
                Size = new Vector2(11f, 14f),
                FillColor = new Color(160, 160, 160),
                OutlineColor = Color.Transparent
            };
            stackPanelWidget.Children.Add(m_foodOverlayWidget);
            //完成stackPanelWidget的操作
            list.Add(stackPanelWidget);
            //红框Split标记
            m_splitLabelWidget = new LabelWidget {
                Text = LanguageControl.Get(fName, "1"),
                Color = new Color(255, 64, 0),
                HorizontalAlignment = WidgetAlignment.Near,
                VerticalAlignment = WidgetAlignment.Near,
                Margin = new Vector2(2f, 0f)
            };
            list.Add(m_splitLabelWidget);
            //为mod提供的标记
            ModsManager.HookAction(
                "OnInventorySlotWidgetDefined",
                loader => {
                    loader.OnInventorySlotWidgetDefined(this, out List<Widget> childrenWidgetsToAdd);
                    if (childrenWidgetsToAdd != null) {
                        list.AddRange(childrenWidgetsToAdd);
                    }
                    return false;
                }
            );
            //最后将Array放到Childred中
            Children.Add(list.ToArray());
        }

        public virtual void AssignInventorySlot(IInventory inventory, int slotIndex) {
            m_inventory = inventory;
            m_slotIndex = slotIndex;
            m_subsystemTerrain = inventory?.Project.FindSubsystem<SubsystemTerrain>(true);
            UpdateEnvironmentData(m_blockIconWidget.DrawBlockEnvironmentData);
        }

        public override void Update() {
            if (m_inventory == null
                || DragHostWidget == null) {
                return;
            }
            WidgetInput input = Input;
            ComponentPlayer viewPlayer = GetViewPlayer();
            int slotValue = m_inventory.GetSlotValue(m_slotIndex);
            int num = Terrain.ExtractContents(slotValue);
            Block block = BlocksManager.Blocks[num];
            UpdateEnvironmentData(m_blockIconWidget.DrawBlockEnvironmentData);
            m_blockIconWidget.DrawBlockEnvironmentData.Owner = m_entity;
            if (m_focus && !input.Press.HasValue) {
                m_focus = false;
            }
            else if (input.Tap.HasValue
                && HitTestGlobal(input.Tap.Value) == this) {
                m_focus = true;
            }
            if (input.SpecialClick.HasValue
                && HitTestGlobal(input.SpecialClick.Value.Start) == this
                && HitTestGlobal(input.SpecialClick.Value.End) == this) {
                IInventory inventory = null;
                foreach (InventorySlotWidget item in ((ContainerWidget)RootWidget).AllChildren.OfType<InventorySlotWidget>()) {
                    if (item.m_inventory != null
                        && item.m_inventory != m_inventory
                        && item.Input == Input
                        && item.IsEnabledGlobal
                        && item.IsVisibleGlobal) {
                        inventory = item.m_inventory;
                        break;
                    }
                }
                if (inventory != null) {
                    int num2 = ComponentInventoryBase.FindAcquireSlotForItem(inventory, slotValue);
                    if (num2 >= 0) {
                        HandleMoveItem(m_inventory, m_slotIndex, inventory, num2, m_inventory.GetSlotCount(m_slotIndex));
                    }
                }
            }
            if (input.Click.HasValue
                && HitTestGlobal(input.Click.Value.Start) == this
                && HitTestGlobal(input.Click.Value.End) == this) {
                bool flag = false;
                if (viewPlayer != null) {

                    IInventory splitSourceInventory = viewPlayer.ComponentInput.SplitSourceInventory;
                    int splitSourceSlotIndex = viewPlayer.ComponentInput.SplitSourceSlotIndex;

                    if (splitSourceInventory == m_inventory
                        && splitSourceSlotIndex == m_slotIndex) {
                        viewPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(null, -1);
                        flag = true;
                    }
                    else if (splitSourceInventory != null) {
                       
                        int totalCount = splitSourceInventory.GetSlotCount(splitSourceSlotIndex);
                        int splitCount = CalculateSplitCount(totalCount, DragMode.SingleItem);

                        flag = HandleMoveItem(
                            splitSourceInventory,
                            splitSourceSlotIndex,
                            m_inventory,
                            m_slotIndex,
                            splitCount
                        );
                        AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                    }
                }
                if (!flag
                    && m_inventory.ActiveSlotIndex != m_slotIndex
                    && m_slotIndex < 10) {
                    m_inventory.ActiveSlotIndex = m_slotIndex;
                    if (m_inventory.ActiveSlotIndex == m_slotIndex) {
                        AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                    }
                }
            }
            if (!m_focus
                || ProcessingOnly
                || viewPlayer == null) {
                return;
            }
            Vector2? hold = input.Hold;
            if (hold.HasValue
                && HitTestGlobal(hold.Value) == this
                && !DragHostWidget.IsDragInProgress
                && m_inventory.GetSlotCount(m_slotIndex) > 0
                && (viewPlayer.ComponentInput.SplitSourceInventory != m_inventory || viewPlayer.ComponentInput.SplitSourceSlotIndex != m_slotIndex)) {
                input.Clear();
                viewPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(m_inventory, m_slotIndex);
                AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            }
            Vector2? drag = input.Drag;
            if (!drag.HasValue
                || HitTestGlobal(drag.Value) != this
                || DragHostWidget.IsDragInProgress) {
                return;
            }
            int slotCount = m_inventory.GetSlotCount(m_slotIndex);
            if (slotCount > 0) {
                DragMode dragMode = input.DragMode;
                if (viewPlayer.ComponentInput.SplitSourceInventory == m_inventory
                    && viewPlayer.ComponentInput.SplitSourceSlotIndex == m_slotIndex) {
                    dragMode = SettingsManager.DragHalfInSplit ? DragMode.HalfItems : DragMode.SingleItem;
                }

                // 计算物品分割的数量
                int num3 = CalculateSplitCount(slotCount, dragMode);

                ContainerWidget containerWidget = (ContainerWidget)LoadWidget(
                    null,
                    ContentManager.Get<XElement>("Widgets/InventoryDragWidget"),
                    null
                );
                containerWidget.Children.Find<BlockIconWidget>("InventoryDragWidget.Icon").Value = Terrain.ReplaceLight(slotValue, 15);
                containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Name").Text = block.GetDisplayName(m_subsystemTerrain, slotValue);
                containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Count").Text = num3.ToString();
                containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Count").IsVisible = !(m_inventory is ComponentCreativeInventory)
                    && !(m_inventory is ComponentFurnitureInventory);
                UpdateEnvironmentData(containerWidget.Children.Find<BlockIconWidget>("InventoryDragWidget.Icon").DrawBlockEnvironmentData);
                DragHostWidget.BeginDrag(
                    containerWidget,
                    new InventoryDragData { Inventory = m_inventory, SlotIndex = m_slotIndex, DragMode = dragMode },
                    delegate { m_dragMode = null; }
                );
                m_dragMode = dragMode;
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            if (m_inventory != null) {
                bool flag = m_inventory is ComponentCreativeInventory || m_inventory is ComponentFurnitureInventory;
                int num = m_inventory.GetSlotCount(m_slotIndex);
                if (!flag
                    && m_dragMode.HasValue) {
                    num = m_dragMode.Value switch {
                        DragMode.AllItems => 0,
                        DragMode.SingleItem => num - 1,
                        DragMode.HalfItems => num - (num + 1) / 2,
                        _ => 0
                    };
                }
                m_rectangleWidget.IsVisible = true;
                if (num > 0) {
                    int slotValue = m_inventory.GetSlotValue(m_slotIndex);
                    int num2 = Terrain.ExtractContents(slotValue);
                    Block block = BlocksManager.Blocks[num2];
                    bool flag2 = block.GetRotPeriod(slotValue) > 0 && block.GetDamage(slotValue) > 0;
                    m_blockIconWidget.Value = Terrain.ReplaceLight(slotValue, 15);
                    m_blockIconWidget.IsVisible = !HideBlockIcon;
                    if (num != m_lastCount) {
                        m_countWidget.Text = num.ToString();
                        m_lastCount = num;
                    }
                    m_countWidget.IsVisible = num > 1 && !flag;
                    m_editOverlayWidget.IsVisible = !HideEditOverlay && block.IsEditable_(slotValue);
                    m_interactiveOverlayWidget.IsVisible = !HideInteractiveOverlay && block.IsInteractive(m_subsystemTerrain, slotValue);
                    m_foodOverlayWidget.IsVisible = !HideFoodOverlay && block.GetRotPeriod(slotValue) > 0;
                    m_foodOverlayWidget.FillColor = flag2 ? new Color(128, 64, 0) : new Color(160, 160, 160);
                    if (!flag) {
                        float percent = block.GetBlockHealth(slotValue);
                        if (percent >= 0) {
                            m_healthBarWidget.IsVisible = true;
                            m_healthBarWidget.Value = percent;
                        }
                        else {
                            m_healthBarWidget.IsVisible = false;
                        }
                    }
                    else {
                        m_healthBarWidget.IsVisible = false;
                    }
                }
                else {
                    m_blockIconWidget.IsVisible = false;
                    m_countWidget.IsVisible = false;
                    m_healthBarWidget.IsVisible = false;
                    m_editOverlayWidget.IsVisible = false;
                    m_interactiveOverlayWidget.IsVisible = false;
                    m_foodOverlayWidget.IsVisible = false;
                }
                m_highlightWidget.IsVisible = !HideHighlightRectangle;
                m_highlightWidget.OutlineColor = Color.Transparent;
                m_highlightWidget.FillColor = Color.Transparent;
                m_splitLabelWidget.IsVisible = false;
                if (m_slotIndex == m_inventory.ActiveSlotIndex) {
                    m_highlightWidget.OutlineColor = new Color(0, 0, 0);
                    m_highlightWidget.FillColor = new Color(0, 0, 0, 80);
                }
                if (IsSplitMode()) {
                    m_highlightWidget.OutlineColor = new Color(255, 64, 0);
                    m_splitLabelWidget.IsVisible = true;
                }
            }
            else {
                m_rectangleWidget.IsVisible = false;
                m_highlightWidget.IsVisible = false;
                m_blockIconWidget.IsVisible = false;
                m_countWidget.IsVisible = false;
                m_healthBarWidget.IsVisible = false;
                m_editOverlayWidget.IsVisible = false;
                m_interactiveOverlayWidget.IsVisible = false;
                m_foodOverlayWidget.IsVisible = false;
                m_splitLabelWidget.IsVisible = false;
            }
            IsDrawRequired = m_inventoryDragData != null;
            base.MeasureOverride(parentAvailableSize);
            ModsManager.HookAction(
                "InventorySlotWidgetMeasureOverride",
                loader => {
                    loader.InventorySlotWidgetMeasureOverride(this, parentAvailableSize);
                    return false;
                }
            );
        }

        public override void Draw(DrawContext dc) {
            if (m_inventory != null
                && m_inventoryDragData != null) {
                int slotValue = m_inventoryDragData.Inventory.GetSlotValue(m_inventoryDragData.SlotIndex);
                if (m_inventory.GetSlotProcessCapacity(m_slotIndex, slotValue) >= 0
                    || m_inventory.GetSlotCapacity(m_slotIndex, slotValue) > 0) {
                    float num = 80f * GlobalTransform.Right.Length();
                    Vector2 center = Vector2.Transform(ActualSize / 2f, GlobalTransform);
                    FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(100);
                    flatBatch2D.QueueEllipse(center, new Vector2(num), 0f, new Color(0, 0, 0, 96) * GlobalColorTransform, 64);
                    flatBatch2D.QueueEllipse(center, new Vector2(num - 0.5f), 0f, new Color(0, 0, 0, 64) * GlobalColorTransform, 64);
                    flatBatch2D.QueueEllipse(center, new Vector2(num + 0.5f), 0f, new Color(0, 0, 0, 48) * GlobalColorTransform, 64);
                    flatBatch2D.QueueDisc(center, new Vector2(num), 0f, new Color(0, 0, 0, 48) * GlobalColorTransform, 64);
                }
            }
            m_inventoryDragData = null;
        }

        public void DragOver(Widget dragWidget, object data) {
            m_inventoryDragData = data as InventoryDragData;
        }

        public virtual void DragDrop(Widget dragWidget, object data) {
            if (m_inventory != null
                && data is InventoryDragData inventoryDragData) {
                HandleDragDrop(inventoryDragData.Inventory, inventoryDragData.SlotIndex, inventoryDragData.DragMode, m_inventory, m_slotIndex);
            }
        }

        void UpdateEnvironmentData(DrawBlockEnvironmentData environmentData) {
            environmentData.SubsystemTerrain = m_subsystemTerrain;
            if (!(m_inventory is Component)) {
                return;
            }
            Component component = (Component)m_inventory;
            ComponentFrame componentFrame = component.Entity.FindComponent<ComponentFrame>();
            if (componentFrame != null) {
                Point3 point = Terrain.ToCell(componentFrame.Position);
                environmentData.InWorldMatrix = componentFrame.Matrix;
                environmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(point.X, point.Z);
                environmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(point.X, point.Z);
            }
            else {
                ComponentBlockEntity componentBlockEntity = component.Entity.FindComponent<ComponentBlockEntity>();
                if (componentBlockEntity != null) {
                    Point3 coordinates = componentBlockEntity.Coordinates;
                    environmentData.InWorldMatrix = Matrix.Identity;
                    environmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(coordinates.X, coordinates.Z);
                    environmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(coordinates.X, coordinates.Z);
                }
            }
            ComponentVitalStats componentVitalStats = component.Entity.FindComponent<ComponentVitalStats>();
            if (componentVitalStats != null) {
                environmentData.EnvironmentTemperature = componentVitalStats.EnvironmentTemperature;
            }
        }

        public virtual ComponentPlayer GetViewPlayer() {
            if (GameWidget == null) {
                return null;
            }
            return GameWidget.PlayerData.ComponentPlayer;
        }

        public virtual bool IsSplitMode() {
            ComponentPlayer viewPlayer = GetViewPlayer();
            if (viewPlayer != null) {
                if (m_inventory != null
                    && m_inventory == viewPlayer.ComponentInput.SplitSourceInventory) {
                    return m_slotIndex == viewPlayer.ComponentInput.SplitSourceSlotIndex;
                }
            }
            return false;
        }

        /// <summary>
        /// 计算分割的数量
        /// </summary>
        /// <param name="totalCount">总数</param>
        /// <param name="dragMode"></param>
        /// <returns>数量</returns>
        private int CalculateSplitCount(int totalCount, DragMode dragMode) {
            int splitCount = totalCount; // 默认是全部数量

            switch (dragMode) {
                case DragMode.AllItems://全部
                    splitCount = totalCount;
                    break;
                case DragMode.SingleItem://单个
                    splitCount = MathUtils.Min(totalCount, 1); 
                    break;
                case DragMode.HalfItems://均分
                    splitCount = (totalCount + 1) / 2;
                    break;
            }

            ModsManager.HookAction(
                "OnInventorySlotWidgetCalculateSplitCount",
                loader => {
                    loader.OnInventorySlotWidgetCalculateSplitCount(
                        this,
                        totalCount,
                        dragMode,
                        ref splitCount
                    );
                    return false;
                }
            );

            // 确保数量在1到totalCount之间
            return MathUtils.Clamp(splitCount, 1, totalCount);
        }

        public virtual bool HandleMoveItem(IInventory sourceInventory,
            int sourceSlotIndex,
            IInventory targetInventory,
            int targetSlotIndex,
            int count) {
            bool moved_ = false;
            ModsManager.HookAction(
                "HandleMoveInventoryItem",
                loader => {
                    loader.HandleMoveInventoryItem(
                        this,
                        sourceInventory,
                        sourceSlotIndex,
                        targetInventory,
                        targetSlotIndex,
                        ref count,
                        out bool moved
                    );
                    moved_ |= moved;
                    return false;
                }
            );
            int slotValue = sourceInventory.GetSlotValue(sourceSlotIndex);
            int slotValue2 = targetInventory.GetSlotValue(targetSlotIndex);
            int slotCount = sourceInventory.GetSlotCount(sourceSlotIndex);
            int slotCount2 = targetInventory.GetSlotCount(targetSlotIndex);
            if (slotCount2 == 0
                || slotValue == slotValue2) {
                int num = MathUtils.Min(targetInventory.GetSlotCapacity(targetSlotIndex, slotValue) - slotCount2, slotCount, count);
                if (num > 0) {
                    int count2 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num);
                    targetInventory.AddSlotItems(targetSlotIndex, slotValue, count2);
                    return true;
                }
            }
            return moved_;
        }

        public virtual bool HandleDragDrop(IInventory sourceInventory,
            int sourceSlotIndex,
            DragMode dragMode,
            IInventory targetInventory,
            int targetSlotIndex) {
            int sourceSlotValue = sourceInventory.GetSlotValue(sourceSlotIndex);
            int targetSlotValue = targetInventory.GetSlotValue(targetSlotIndex);
            int dragCount = sourceInventory.GetSlotCount(sourceSlotIndex);
            int targetSlotCount = targetInventory.GetSlotCount(targetSlotIndex);
            int targetSlotCapacity = targetInventory.GetSlotCapacity(targetSlotIndex, sourceSlotValue);
            int targetSlotProcessCapacity = targetInventory.GetSlotProcessCapacity(targetSlotIndex, sourceSlotValue);

            dragCount = CalculateSplitCount(dragCount, dragMode);

            bool flag = false;
            //先进行Process操作
            ModsManager.HookAction(
                "HandleInventoryDragProcess",
                loader => {
                    loader.HandleInventoryDragProcess(
                        this,
                        sourceInventory,
                        sourceSlotIndex,
                        targetInventory,
                        targetSlotIndex,
                        ref targetSlotProcessCapacity
                    );
                    return false;
                }
            );
            if (targetSlotProcessCapacity > 0) {
                int processCount = sourceInventory.RemoveSlotItems(sourceSlotIndex, MathUtils.Min(dragCount, targetSlotProcessCapacity));
                targetInventory.ProcessSlotItems(
                    targetSlotIndex,
                    sourceSlotValue,
                    dragCount,
                    processCount,
                    out int processedValue,
                    out int processedCount
                );
                if (processedValue != 0
                    && processedCount != 0) {
                    //TODO:ProcessItem允许突破格子物品上限限制
                    int count = MathUtils.Min(sourceInventory.GetSlotCapacity(sourceSlotIndex, processedValue), processedCount);
                    sourceInventory.RemoveSlotItems(sourceSlotIndex, count);
                    sourceInventory.AddSlotItems(sourceSlotIndex, processedValue, count);
                }
                flag = true;
            }
            else if (!ProcessingOnly) {
                bool movedByMods = false;
                ModsManager.HookAction(
                    "HandleInventoryDragMove",
                    loader => {
                        loader.HandleInventoryDragMove(
                            this,
                            sourceInventory,
                            sourceSlotIndex,
                            targetInventory,
                            targetSlotIndex,
                            movedByMods,
                            out bool skip
                        );
                        movedByMods |= skip;
                        return false;
                    }
                );
                if (!movedByMods) {
                    //移动物品
                    if ((targetSlotCount == 0 || sourceSlotValue == targetSlotValue)
                        && targetSlotCount < targetSlotCapacity) {
                        int num2 = MathUtils.Min(targetSlotCapacity - targetSlotCount, dragCount);
                        bool handleMove = HandleMoveItem(sourceInventory, sourceSlotIndex, targetInventory, targetSlotIndex, num2);
                        if (handleMove) {
                            flag = true;
                        }
                    }
                    //交换两个物品栏之间的物品
                    else if (targetInventory.GetSlotCapacity(targetSlotIndex, sourceSlotValue) >= dragCount
                        && sourceInventory.GetSlotCapacity(sourceSlotIndex, targetSlotValue) >= targetSlotCount
                        && sourceInventory.GetSlotCount(sourceSlotIndex) == dragCount) {
                        int count3 = targetInventory.RemoveSlotItems(targetSlotIndex, targetSlotCount);
                        int count4 = sourceInventory.RemoveSlotItems(sourceSlotIndex, dragCount);
                        targetInventory.AddSlotItems(targetSlotIndex, sourceSlotValue, count4);
                        sourceInventory.AddSlotItems(sourceSlotIndex, targetSlotValue, count3);
                        flag = true;
                    }
                }
            }
            if (flag) {
                AudioManager.PlaySound("Audio/UI/ItemMoved", 1f, 0f, 0f);
            }
            return flag;
        }
    }
}