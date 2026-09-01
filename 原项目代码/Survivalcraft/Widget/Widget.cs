using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Serialization;
using XmlUtilities;

namespace Game {
    public class Widget : IDisposable {
        public class DrawContext {
            public List<DrawItem> m_drawItems = [];

            public static List<DrawItem> m_drawItemsCache = [];

            public readonly PrimitivesRenderer2D PrimitivesRenderer2D = new();

            public readonly PrimitivesRenderer3D PrimitivesRenderer3D = new();

            public readonly PrimitivesRenderer2D CursorPrimitivesRenderer2D = new();

            /// <summary>
            ///     绘制 rootWidget 及其子 Widget。
            /// </summary>
            /// <param name="rootWidget"></param>
            // ReSharper disable MemberHidesStaticFromOuterClass
            public virtual void DrawWidgetsHierarchy(Widget rootWidget)
                // ReSharper restore MemberHidesStaticFromOuterClass
            {
                m_drawItems.Clear();
                CollateDrawItems(rootWidget, Display.ScissorRectangle);
                AssignDrawItemsLayers();
                RenderDrawItems();
                ReturnDrawItemsToCache();
            }

            /// <summary>
            ///     根据 Widget 的层级关系 以及 Widget 的各项绘制有关的属性生成 DrawItem。
            /// </summary>
            /// <param name="widget">DrawItem 所属的 Widget。</param>
            /// <param name="scissorRectangle">Widget 父级的 ScissorRectangle。</param>
            public virtual void CollateDrawItems(Widget widget, Rectangle scissorRectangle) {
                if (!widget.IsVisible
                    || !widget.IsDrawEnabled) {
                    // 当 Widget 不可见或 Widget 绘制被禁用时。
                    return;
                }
                bool isWidgetInBound = widget.GlobalBounds.Intersection(
                    new BoundingRectangle(scissorRectangle.Left, scissorRectangle.Top, scissorRectangle.Right, scissorRectangle.Bottom)
                );
                Rectangle? widgetScissorRectangle = null;
                if (widget.ClampToBounds && isWidgetInBound) {
                    // 如果 Widget 需要将自身绘制的区域限制在 Widget 的 Bounds 内，
                    // 则生成一个用于设置 ScissorRectangle 的 DrawItem
                    widgetScissorRectangle = scissorRectangle;
                    int left = (int)MathF.Floor(widget.GlobalBounds.Min.X - 0.5f);
                    int top = (int)MathF.Floor(widget.GlobalBounds.Min.Y - 0.5f);
                    int right = (int)MathF.Ceiling(widget.GlobalBounds.Max.X - 0.5f);
                    int bottom = (int)MathF.Ceiling(widget.GlobalBounds.Max.Y - 0.5f);
                    scissorRectangle = Rectangle.Intersection(new Rectangle(left, top, right - left, bottom - top), widgetScissorRectangle.Value);
                    DrawItem drawItem = GetDrawItemFromCache();
                    drawItem.ScissorRectangle = scissorRectangle;
                    m_drawItems.Add(drawItem);
                }
                if (widget.IsDrawRequired && isWidgetInBound) {
                    // 如果 Widget 需要绘制，则生成一个用于绘制的 DrawItem。
                    DrawItem drawItem = GetDrawItemFromCache();
                    drawItem.Widget = widget;
                    m_drawItems.Add(drawItem);
                }
                if (isWidgetInBound || !widget.ClampToBounds) {
                    if (widget is ContainerWidget containerWidget) {
                        // 生成子 Widget 的 DrawItem。
                        foreach (Widget child in containerWidget.Children) {
                            CollateDrawItems(child, scissorRectangle);
                        }
                    }
                }
                if (widget.IsOverdrawRequired && isWidgetInBound) {
                    // 如果 Widget 需要 Overdraw，则生成一个用于 Overdraw 的 DrawItem。
                    DrawItem drawItem = GetDrawItemFromCache();
                    drawItem.Widget = widget;
                    drawItem.IsOverdraw = true;
                    m_drawItems.Add(drawItem);
                }
                if (widgetScissorRectangle.HasValue) {
                    // 如果 Widget 需要将自身绘制区域限制在 Widget 的 Bounds 内，则生成一个用于设置 ScissorRectangle 的 DrawItem。
                    DrawItem drawItem = GetDrawItemFromCache();
                    drawItem.ScissorRectangle = widgetScissorRectangle;
                    m_drawItems.Add(drawItem);
                }
                widget.WidgetsHierarchyInput?.Draw(this);
            }

            /// <summary>
            ///     指定 DrawItem 的 Layer（层级）。
            /// </summary>
            public virtual void AssignDrawItemsLayers() {
                for (int i = 0; i < m_drawItems.Count; i++) {
                    DrawItem drawItem = m_drawItems[i];
                    for (int j = i + 1; j < m_drawItems.Count; j++) {
                        DrawItem drawItem2 = m_drawItems[j];
                        if (drawItem.ScissorRectangle.HasValue
                            || drawItem2.ScissorRectangle.HasValue) {
                            drawItem2.Layer = MathUtils.Max(drawItem2.Layer, drawItem.Layer + 1);
                        }
                        else if (TestOverlap(drawItem.Widget, drawItem2.Widget)) {
                            drawItem2.Layer = MathUtils.Max(drawItem2.Layer, drawItem.Layer + 1);
                        }
                    }
                }
                m_drawItems.Sort();
                ModsManager.HookAction(
                    "OnDrawItemAssigned",
                    loader => {
                        loader.OnDrawItemAssigned(this);
                        return false;
                    }
                );
            }

            /// <summary>
            /// </summary>
            public virtual void RenderDrawItems() {
                Rectangle scissorRectangle = Display.ScissorRectangle;
                int currentLayer = 0;
                foreach (DrawItem drawItem in m_drawItems) {
                    if (LayersLimit >= 0
                        && drawItem.Layer > LayersLimit) {
                        break;
                    }
                    if (drawItem.Layer != currentLayer) {
                        currentLayer = drawItem.Layer;
                        PrimitivesRenderer3D.Flush(Matrix.Identity);
                        PrimitivesRenderer2D.Flush();
                    }
                    if (drawItem.Widget != null) {
                        Action afterWidgetDraw = null;
                        bool skip = false;
                        ModsManager.HookAction(
                            "BeforeWidgetDrawItemRender",
                            loader => {
                                loader.BeforeWidgetDrawItemRender(
                                    drawItem,
                                    out bool skipVanillaDraw,
                                    out Action afterDrawAction,
                                    ref scissorRectangle,
                                    this
                                );
                                if (afterDrawAction != null) {
                                    afterWidgetDraw += afterDrawAction;
                                }
                                skip |= skipVanillaDraw;
                                return false;
                            }
                        );
                        if (!skip) {
                            if (drawItem.IsOverdraw) {
                                drawItem.Widget.Overdraw(this);
                            }
                            else {
                                drawItem.Widget.Draw(this);
                            }
                        }
                        afterWidgetDraw?.Invoke();
                    }
                    else {
                        Display.ScissorRectangle = Rectangle.Intersection(scissorRectangle, drawItem.ScissorRectangle!.Value);
                    }
                }
                PrimitivesRenderer3D.Flush(Matrix.Identity);
                PrimitivesRenderer2D.Flush();
                Display.ScissorRectangle = scissorRectangle;
                CursorPrimitivesRenderer2D.Flush();
            }

            /// <summary>
            ///     从缓存中获取一个 <see cref="DrawItem" /> 实例，用于减少实例创建次数以缓解 GC 压力。
            /// </summary>
            /// <returns>搜索到的实例</returns>
            public virtual DrawItem GetDrawItemFromCache() {
                if (m_drawItemsCache.Count > 0) {
                    DrawItem result = m_drawItemsCache[^1];
                    m_drawItemsCache.RemoveAt(m_drawItemsCache.Count - 1);
                    return result;
                }
                return new DrawItem();
            }

            /// <summary>
            ///     对 <see cref="DrawItem" /> 进行复用，并存储到缓存列表内。
            /// </summary>
            public virtual void ReturnDrawItemsToCache() {
                foreach (DrawItem drawItem in m_drawItems) {
                    drawItem.Widget = null;
                    drawItem.Layer = 0;
                    drawItem.IsOverdraw = false;
                    drawItem.ScissorRectangle = null;
                    m_drawItemsCache.Add(drawItem);
                }
            }
        }

        // ReSharper disable UnassignedField.Global
        public Action<Vector2> MeasureOverride1;

        public Action Update1;
        // ReSharper restore UnassignedField.Global

        /// <summary>
        ///     绘制任务，有多种类型，绘制任务会按照 <see cref="Layer" /> 进行排序。
        /// </summary>
        public class DrawItem : IComparable<DrawItem> {
            /// <summary>
            ///     绘制任务所在的层级，值越小，绘制越靠前，绘制靠前的绘制任务会被靠后的覆盖。
            /// </summary>
            public int Layer;

            /// <summary>
            ///     是否为 Overdraw 任务，绘制任务被定义为需要绘制 Widget 时，该值决定了 <see cref="Widget.Draw" /> 或 <see cref="Widget.Overdraw" /> 的调用
            ///     （当 <see cref="IsOverdraw" /> 为 true 时则调用 <see cref="Widget.Overdraw" />，否则调用 <see cref="Widget.Draw" />）。
            /// </summary>
            public bool IsOverdraw;

            /// <summary>
            ///     绘制任务所属的 Widget
            /// </summary>
            public Widget Widget;

            /// <summary>
            ///     绘制任务所绑定的 ScissorRectangle，可为空。
            /// </summary>
            public Rectangle? ScissorRectangle;

            public int CompareTo(DrawItem other) => Layer - other.Layer;
        }

        public bool m_isVisible;

        public bool m_isEnabled;

        public Vector2 m_actualSize;

        public Vector2 m_desiredSize;

        public Vector2 m_parentDesiredSize;

        public BoundingRectangle m_globalBounds;

        public Vector2 m_parentOffset;

        public bool m_isLayoutTransformIdentity = true;

        public bool m_isRenderTransformIdentity = true;

        public Matrix m_layoutTransform = Matrix.Identity;

        public Matrix m_renderTransform = Matrix.Identity;

        public Matrix m_globalTransform = Matrix.Identity;

        public Matrix? m_invertedGlobalTransform;

        public float? m_globalScale;

        public Color m_colorTransform = Color.White;

        public Color m_globalColorTransform;

        public WidgetInput m_widgetsHierarchyInput;

        public static Queue<DrawContext> m_drawContextsCache = new();

        public static int LayersLimit = -1;

        public static bool DrawWidgetBounds = false;

        public WidgetInput WidgetsHierarchyInput {
            get => m_widgetsHierarchyInput;
            set {
                if (value != null) {
                    if (value.m_widget != null
                        && value.m_widget != this) {
                        throw new InvalidOperationException("WidgetInput already assigned to another widget.");
                    }
                    value.m_widget = this;
                    m_widgetsHierarchyInput = value;
                }
                else if (m_widgetsHierarchyInput != null) {
                    m_widgetsHierarchyInput.m_widget = null;
                    m_widgetsHierarchyInput = null;
                }
            }
        }

        public WidgetInput Input {
            get {
                Widget widget = this;
                do {
                    if (widget.WidgetsHierarchyInput != null) {
                        return widget.WidgetsHierarchyInput;
                    }
                    widget = widget.ParentWidget;
                }
                while (widget != null);
                return WidgetInput.EmptyInput;
            }
        }

        public Matrix LayoutTransform {
            get => m_layoutTransform;
            set {
                m_layoutTransform = value;
                m_isLayoutTransformIdentity = value == Matrix.Identity;
            }
        }

        public Matrix RenderTransform {
            get => m_renderTransform;
            set {
                m_renderTransform = value;
                m_isRenderTransformIdentity = value == Matrix.Identity;
            }
        }

        public Matrix GlobalTransform => m_globalTransform;

        public float GlobalScale {
            get {
                m_globalScale ??= m_globalTransform.Right.Length();
                return m_globalScale.Value;
            }
        }

        public Matrix InvertedGlobalTransform {
            get {
                m_invertedGlobalTransform ??= Matrix.Invert(m_globalTransform);
                return m_invertedGlobalTransform.Value;
            }
        }

        public BoundingRectangle GlobalBounds => m_globalBounds;

        public Color ColorTransform {
            get => m_colorTransform;
            set => m_colorTransform = value;
        }

        public Color GlobalColorTransform => m_globalColorTransform;

        public virtual string Name { get; set; }

        public object Tag { get; set; }
        public object ExtraData { get; set; }

        public virtual bool IsVisible {
            get => m_isVisible;
            set {
                if (value != m_isVisible) {
                    m_isVisible = value;
                    if (!m_isVisible) {
                        UpdateCeases();
                    }
                }
            }
        }

        public virtual bool IsEnabled {
            get => m_isEnabled;
            set {
                if (value != m_isEnabled) {
                    m_isEnabled = value;
                    if (!m_isEnabled) {
                        UpdateCeases();
                    }
                }
            }
        }

        public virtual bool IsHitTestVisible { get; set; }

        public bool IsVisibleGlobal {
            get {
                if (IsVisible) {
                    if (ParentWidget != null) {
                        return ParentWidget.IsVisibleGlobal;
                    }
                    return true;
                }
                return false;
            }
        }

        public bool IsEnabledGlobal {
            get {
                if (IsEnabled) {
                    if (ParentWidget != null) {
                        return ParentWidget.IsEnabledGlobal;
                    }
                    return true;
                }
                return false;
            }
        }

        public bool ClampToBounds { get; set; }

        public virtual Vector2 Margin {
            [Obsolete("Use MarginLeft, MarginTop, MarginRight and MarginBottom instead.")]
            get => new (MarginLeft, MarginTop);
            set {
                MarginLeft = value.X;
                MarginRight = value.X;
                MarginTop = value.Y;
                MarginBottom = value.Y;
            }
        }

        public virtual float MarginLeft { get; set; }

        public virtual float MarginRight { get; set; }

        public virtual float MarginTop { get; set; }

        public virtual float MarginBottom { get; set; }

        public virtual Vector4 Margin4 {
            get => new(MarginLeft, MarginTop, MarginRight, MarginBottom);
            set {
                MarginLeft = value.X;
                MarginTop = value.Y;
                MarginRight = value.Z;
                MarginBottom = value.W;
            }
        }

        public virtual float MarginHorizontalSum => MarginLeft + MarginRight;

        public virtual float MarginVerticalSum => MarginTop + MarginBottom;

        public virtual Vector2 MarginHorizontalSumAndVerticalSum => new(MarginLeft + MarginRight, MarginTop + MarginBottom);

        public virtual WidgetAlignment HorizontalAlignment { get; set; }

        public virtual WidgetAlignment VerticalAlignment { get; set; }

        public Vector2 ActualSize => m_actualSize;

        public Vector2 DesiredSize {
            get => m_desiredSize;
            set => m_desiredSize = value;
        }

        public Vector2 ParentDesiredSize => m_parentDesiredSize;

        public bool IsUpdateEnabled { get; set; } = true;

        public bool IsDrawEnabled { get; set; } = true;

        public bool IsDrawRequired { get; set; }

        public bool IsOverdrawRequired { get; set; }

        public XElement Style {
            set => LoadContents(null, value);
        }

        public ContainerWidget ParentWidget { get; set; }

        public Widget RootWidget {
            get {
                if (ParentWidget == null) {
                    return this;
                }
                return ParentWidget.RootWidget;
            }
        }

        public Widget() {
            IsVisible = true;
            IsHitTestVisible = true;
            IsEnabled = true;
            DesiredSize = new Vector2(1f / 0f);
        }

        public static Widget LoadWidget(object eventsTarget, XElement node, ContainerWidget parentWidget) {
            if (node.Name.LocalName.Contains(".")) {
                throw new NotImplementedException("Node property specification not implemented.");
            }
#pragma warning disable IL2072
            if (Activator.CreateInstance(FindTypeFromXmlName(node.Name.LocalName, node.Name.NamespaceName)) is not Widget widget) {
#pragma warning restore IL2072
                throw new Exception($"Type \"{node.Name.LocalName}\" is not a Widget.");
            }
            ModsManager.HookAction(
                "OnWidgetConstruct",
                loader => {
                    loader.OnWidgetConstruct(ref widget);
                    return false;
                }
            );
            parentWidget?.Children.Add(widget);
            widget.LoadContents(eventsTarget, node);
            return widget;
        }

        public virtual void LoadContents(object eventsTarget, XElement node) {
            LoadProperties(eventsTarget, node);
            LoadChildren(eventsTarget, node);
            ModsManager.HookAction(
                "OnWidgetContentsLoaded",
                loader => {
                    loader.OnWidgetContentsLoaded(this);
                    return false;
                }
            );
        }

        public virtual void LoadProperties(object eventsTarget, XElement node) {
#pragma warning disable IL2072
            IEnumerable<PropertyInfo> runtimeProperties = GetType().GetRuntimeProperties().ToArray();
#pragma warning restore IL2072
            foreach (XAttribute attribute in node.Attributes()) {
                if (!attribute.IsNamespaceDeclaration
                    && !attribute.Name.LocalName.StartsWith('_')) {
                    if (attribute.Name.LocalName.Contains('.')) {
                        string[] array = attribute.Name.LocalName.Split('.');
                        if (array.Length != 2) {
                            throw new InvalidOperationException(
                                $"Attached property reference must have form \"TypeName.PropertyName\", property \"{attribute.Name.LocalName}\" in widget of type \"{GetType().FullName}\"."
                            );
                        }
                        Type type = FindTypeFromXmlName(
                            array[0],
                            attribute.Name.NamespaceName != string.Empty ? attribute.Name.NamespaceName : node.Name.NamespaceName
                        );
                        string setterName = $"Set{array[1]}";
#pragma warning disable IL2072
                        MethodInfo methodInfo = type.GetRuntimeMethods().FirstOrDefault(mi => mi.Name == setterName && mi.IsPublic && mi.IsStatic);
#pragma warning restore IL2072
                        if (!(methodInfo != null)) {
                            throw new InvalidOperationException(
                                $"Attached property public static setter method \"{setterName}\" not found, property \"{attribute.Name.LocalName}\" in widget of type \"{GetType().FullName}\"."
                            );
                        }
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length != 2
                            || !(parameters[0].ParameterType == typeof(Widget))) {
                            throw new InvalidOperationException(
                                $"Attached property setter method must take 2 parameters and first one must be of type Widget, property \"{attribute.Name.LocalName}\" in widget of type \"{GetType().FullName}\"."
                            );
                        }
                        object obj = HumanReadableConverter.ConvertFromString(parameters[1].ParameterType, attribute.Value);
                        methodInfo.Invoke(null, [this, obj]);
                    }
                    else {
                        PropertyInfo propertyInfo = runtimeProperties.FirstOrDefault(pi => pi.Name == attribute.Name.LocalName);
                        if (propertyInfo == null) {
                            Log.Warning(
                                $"Property \"{attribute.Name.LocalName}\" not found in widget of type \"{GetType().FullName}\"."
                            );
                            continue;
                        }
                        if (attribute.Value.StartsWith('{')
                            && attribute.Value.EndsWith('}')) {
                            string name = attribute.Value.Substring(1, attribute.Value.Length - 2);
                            object value = ContentManager.Get(propertyInfo.PropertyType, name, null, false);
                            if (value == null) {
                                Log.Error($"Not Found Res [{name}][{propertyInfo.PropertyType.FullName}] when loading {GetType().FullName}");
                            }
                            else {
                                propertyInfo.SetValue(this, value, null);
                            }
                        }
                        else {
                            object obj2 = HumanReadableConverter.ConvertFromString(propertyInfo.PropertyType, attribute.Value);
                            if (propertyInfo.PropertyType == typeof(string)) {
                                obj2 = ((string)obj2).Replace("\\n", "\n").Replace("\\t", "\t");
                            }
                            propertyInfo.SetValue(this, obj2, null);
                        }
                    }
                }
            }
        }

        public virtual void LoadChildren(object eventsTarget, XElement node) {
            if (node.HasElements) {
                if (this is not ContainerWidget containerWidget) {
                    throw new Exception($"Type \"{node.Name.LocalName}\" is not a ContainerWidget, but it contains child widgets.");
                }
                foreach (XElement item in node.Elements()) {
                    if (IsNodeIncludedOnCurrentPlatform(item)) {
                        Widget widget = null;
                        string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Name", null);
                        if (attributeValue != null) {
                            widget = containerWidget.Children.Find(attributeValue, false);
                        }
                        if (widget != null) {
                            widget.LoadContents(eventsTarget, item);
                        }
                        else {
                            LoadWidget(eventsTarget, item, containerWidget);
                        }
                    }
                }
            }
        }

        public virtual bool IsChildWidgetOf(ContainerWidget containerWidget) {
            if (containerWidget == ParentWidget) {
                return true;
            }
            if (ParentWidget == null) {
                return false;
            }
            return ParentWidget.IsChildWidgetOf(containerWidget);
        }

        public virtual void ChangeParent(ContainerWidget parentWidget) {
            if (parentWidget != ParentWidget) {
                ParentWidget = parentWidget;
                if (parentWidget == null) {
                    UpdateCeases();
                }
            }
        }

        public virtual void Measure(Vector2 parentAvailableSize) {
            if (MeasureOverride1 == null) {
                MeasureOverride(parentAvailableSize);
            }
            else {
                MeasureOverride1(parentAvailableSize);
                return;
            }
            if (DesiredSize.X != 1f / 0f
                && DesiredSize.Y != 1f / 0f) {
                BoundingRectangle boundingRectangle = TransformBoundsToParent(DesiredSize);
                m_parentDesiredSize = boundingRectangle.Size();
                m_parentOffset = -boundingRectangle.Min;
            }
            else {
                m_parentDesiredSize = DesiredSize;
                m_parentOffset = Vector2.Zero;
            }
        }

        public virtual void MeasureOverride(Vector2 parentAvailableSize) { }

        public virtual void Arrange(Vector2 position, Vector2 parentActualSize) {
            float num = m_layoutTransform.M11 * m_layoutTransform.M11;
            float num2 = m_layoutTransform.M12 * m_layoutTransform.M12;
            float num3 = m_layoutTransform.M21 * m_layoutTransform.M21;
            float num4 = m_layoutTransform.M22 * m_layoutTransform.M22;
            m_actualSize.X = (num * parentActualSize.X + num3 * parentActualSize.Y) / (num + num3);
            m_actualSize.Y = (num2 * parentActualSize.X + num4 * parentActualSize.Y) / (num2 + num4);
            m_parentOffset = -TransformBoundsToParent(m_actualSize).Min;
            m_globalColorTransform = ParentWidget != null ? ParentWidget.m_globalColorTransform * m_colorTransform : m_colorTransform;
            if (m_isRenderTransformIdentity) {
                m_globalTransform = m_layoutTransform;
            }
            else {
                m_globalTransform = m_isLayoutTransformIdentity ? m_renderTransform : m_renderTransform * m_layoutTransform;
            }
            m_globalTransform.M41 += position.X + m_parentOffset.X;
            m_globalTransform.M42 += position.Y + m_parentOffset.Y;
            if (ParentWidget != null) {
                m_globalTransform *= ParentWidget.GlobalTransform;
            }
            m_invertedGlobalTransform = null;
            m_globalScale = null;
            m_globalBounds = TransformBoundsToGlobal(m_actualSize);
            ArrangeOverride();
        }

        public virtual void ArrangeOverride() { }

        public virtual void UpdateCeases() { }

        public virtual void Update() { }

        public virtual void Draw(DrawContext dc) { }

        public virtual void Overdraw(DrawContext dc) { }

        public virtual bool HitTest(Vector2 point) {
            Vector2 vector = ScreenToWidget(point);
            if (vector.X >= 0f
                && vector.Y >= 0f
                && vector.X <= ActualSize.X) {
                return vector.Y <= ActualSize.Y;
            }
            return false;
        }

        public virtual Widget HitTestGlobal(Vector2 point, Func<Widget, bool> predicate = null) => HitTestGlobal(RootWidget, point, predicate);

        public virtual Vector2 ScreenToWidget(Vector2 p) => Vector2.Transform(p, InvertedGlobalTransform);

        public virtual Vector2 WidgetToScreen(Vector2 p) => Vector2.Transform(p, GlobalTransform);

        public virtual void Dispose() { }

        public static bool TestOverlap(Widget w1, Widget w2) {
            if (w2.m_globalBounds.Min.X >= w1.m_globalBounds.Max.X - 0.001f) {
                return false;
            }
            if (w2.m_globalBounds.Min.Y >= w1.m_globalBounds.Max.Y - 0.001f) {
                return false;
            }
            if (w1.m_globalBounds.Min.X >= w2.m_globalBounds.Max.X - 0.001f) {
                return false;
            }
            if (w1.m_globalBounds.Min.Y >= w2.m_globalBounds.Max.Y - 0.001f) {
                return false;
            }
            return true;
        }

        public static bool IsNodeIncludedOnCurrentPlatform(XElement node) {
            string attributeValue = XmlUtils.GetAttributeValue<string>(node, "_IncludePlatforms", null);
            string attributeValue2 = XmlUtils.GetAttributeValue<string>(node, "_ExcludePlatforms", null);
            if (attributeValue != null
                && attributeValue2 == null) {
                if (attributeValue.Split(' ').Contains(VersionsManager.PlatformString)) {
                    return true;
                }
            }
            else {
                if (attributeValue2 == null
                    || attributeValue != null) {
                    return true;
                }
                if (!attributeValue2.Split(' ').Contains(VersionsManager.PlatformString)) {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateWidgetsHierarchy(Widget rootWidget) {
            if (rootWidget.IsUpdateEnabled) {
                bool isMouseCursorVisible = false;
                UpdateWidgetsHierarchy(rootWidget, ref isMouseCursorVisible);
                Mouse.IsMouseVisible = isMouseCursorVisible;
            }
        }

        public static void LayoutWidgetsHierarchy(Widget rootWidget, Vector2 availableSize) {
            rootWidget.Measure(availableSize);
            rootWidget.Arrange(Vector2.Zero, availableSize);
        }

        public static void DrawWidgetsHierarchy(Widget rootWidget) {
            DrawContext drawContext = m_drawContextsCache.Count > 0 ? m_drawContextsCache.Dequeue() : new DrawContext();
            try {
                drawContext.DrawWidgetsHierarchy(rootWidget);
            }
            finally {
                m_drawContextsCache.Enqueue(drawContext);
            }
        }

        public virtual BoundingRectangle TransformBoundsToParent(Vector2 size) {
            float num = m_layoutTransform.M11 * size.X;
            float num2 = m_layoutTransform.M21 * size.Y;
            float x = num + num2;
            float num3 = m_layoutTransform.M12 * size.X;
            float num4 = m_layoutTransform.M22 * size.Y;
            float x2 = num3 + num4;
            float x3 = MathUtils.Min(0f, num, num2, x);
            float x4 = MathUtils.Max(0f, num, num2, x);
            float y = MathUtils.Min(0f, num3, num4, x2);
            float y2 = MathUtils.Max(0f, num3, num4, x2);
            return new BoundingRectangle(x3, y, x4, y2);
        }

        public virtual BoundingRectangle TransformBoundsToGlobal(Vector2 size) {
            float num = m_globalTransform.M11 * size.X;
            float num2 = m_globalTransform.M21 * size.Y;
            float x = num + num2;
            float num3 = m_globalTransform.M12 * size.X;
            float num4 = m_globalTransform.M22 * size.Y;
            float x2 = num3 + num4;
            float num5 = MathUtils.Min(0f, num, num2, x);
            float num6 = MathUtils.Max(0f, num, num2, x);
            float num7 = MathUtils.Min(0f, num3, num4, x2);
            return new BoundingRectangle(
                y2: MathUtils.Max(0f, num3, num4, x2) + m_globalTransform.M42,
                x1: num5 + m_globalTransform.M41,
                y1: num7 + m_globalTransform.M42,
                x2: num6 + m_globalTransform.M41
            );
        }

        public static Type FindTypeFromXmlName(string name, string namespaceName) {
            if (!string.IsNullOrEmpty(namespaceName)) {
                Uri uri = new(namespaceName);
                if (uri.Scheme == "runtime-namespace") {
                    return TypeCache.FindType($"{uri.AbsolutePath}.{name}", false, true);
                }
                throw new InvalidOperationException("Unknown uri scheme when loading widget. Scheme must be runtime-namespace.");
            }
            throw new InvalidOperationException("Namespace must be specified when creating types in XML.");
        }

        public static Widget HitTestGlobal(Widget widget, Vector2 point, Func<Widget, bool> predicate) {
            if (widget != null
                && widget.IsVisible
                && (!widget.ClampToBounds || widget.HitTest(point))) {
                if (widget is ContainerWidget containerWidget) {
                    WidgetsList children = containerWidget.Children;
                    for (int num = children.Count - 1; num >= 0; num--) {
                        Widget widget2 = HitTestGlobal(children[num], point, predicate);
                        if (widget2 != null) {
                            return widget2;
                        }
                    }
                }
                if (widget.IsHitTestVisible
                    && widget.HitTest(point)
                    && (predicate == null || predicate(widget))) {
                    return widget;
                }
            }
            return null;
        }

        public static void UpdateWidgetsHierarchy(Widget widget, ref bool isMouseCursorVisible) {
            if (!widget.IsVisible
                || !widget.IsUpdateEnabled
                || !widget.IsEnabled) {
                return;
            }
            if (widget.WidgetsHierarchyInput != null) {
                widget.WidgetsHierarchyInput.Update();
                isMouseCursorVisible |= widget.WidgetsHierarchyInput.IsMouseCursorVisible;
            }
            if (widget is ContainerWidget containerWidget) {
                WidgetsList children = containerWidget.Children;
                for (int num = children.Count - 1; num >= 0; num--) {
                    if (num < children.Count) {
                        UpdateWidgetsHierarchy(children[num], ref isMouseCursorVisible);
                    }
                }
            }
            if (widget.Update1 == null) {
                ModsManager.HookAction(
                    "BeforeWidgetUpdate",
                    loader => {
                        loader.BeforeWidgetUpdate(widget);
                        return false;
                    }
                );
                widget.Update();
                ModsManager.HookAction(
                    "AfterWidgetUpdate",
                    loader => {
                        loader.AfterWidgetUpdate(widget);
                        return false;
                    }
                );
            }
            else {
                widget.Update1();
            }
        }
    }
}