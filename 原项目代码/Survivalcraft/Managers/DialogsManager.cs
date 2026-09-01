using Engine;

namespace Game {
    public static class DialogsManager {
        public class CoverWidget : RectangleWidget { }

        public class AnimationData {
            public float Factor;

            public int Direction;

            public CoverWidget CoverWidget = new() { OutlineColor = Color.Transparent, FillColor = new Color(0, 0, 0, 192), IsHitTestVisible = true };
        }

        public static Dictionary<Dialog, AnimationData> m_animationData = [];

        public static List<Dialog> m_dialogs = [];

        public static List<Dialog> m_toRemove = [];

        public static ReadOnlyList<Dialog> Dialogs => new(m_dialogs);

        public static bool HasDialogs(Widget parentWidget) {
            if (parentWidget == null) {
                parentWidget = ScreensManager.CurrentScreen ?? ScreensManager.RootWidget;
            }
            foreach (Dialog dialog in m_dialogs) {
                if (dialog.ParentWidget == parentWidget) {
                    return true;
                }
            }
            return false;
        }

        public static bool HasDialogsOrAnimations(Widget parentWidget) {
            if (parentWidget == null) {
                parentWidget = ScreensManager.CurrentScreen ?? ScreensManager.RootWidget;
            }
            if (HasDialogs(parentWidget)) return true;
            foreach (var kvp in m_animationData) {
                if (kvp.Key.ParentWidget == parentWidget) return true;
            }
            return false;
        }

        public static void ShowDialog(ContainerWidget parentWidget, Dialog dialog) {
            ModsManager.HookAction(
                "OnShowDialog",
                loader => {
                    loader.OnShowDialog(ref parentWidget, ref dialog);
                    return false;
                }
            );
            if (dialog == null) {
                return;
            }
            Dispatcher.Dispatch(
                delegate {
                    if (!m_dialogs.Contains(dialog)) {
                        if (parentWidget == null) {
                            parentWidget = ScreensManager.CurrentScreen ?? ScreensManager.RootWidget;
                        }
                        dialog.WidgetsHierarchyInput = null;
                        m_dialogs.Add(dialog);
                        AnimationData animationData = new() { Direction = 1 };
                        m_animationData[dialog] = animationData;
                        parentWidget.Children.Add(animationData.CoverWidget);
                        if (dialog.ParentWidget != null) {
                            dialog.ParentWidget.Children.Remove(dialog);
                        }
                        parentWidget.Children.Add(dialog);
                        UpdateDialog(dialog, animationData);
                        dialog.Input.Clear();
                    }
                }
            );
        }

        public static void HideDialog(Dialog dialog) {
            ModsManager.HookAction(
                "OnHideDialog",
                loader => {
                    loader.OnHideDialog(ref dialog);
                    return false;
                }
            );
            if (dialog == null) {
                return;
            }
            Dispatcher.Dispatch(
                delegate {
                    if (m_dialogs.Contains(dialog)) {
                        dialog.ParentWidget.Input.Clear();
                        dialog.WidgetsHierarchyInput = new WidgetInput(WidgetInputDevice.None);
                        m_dialogs.Remove(dialog);
                        m_animationData[dialog].Direction = -1;
                    }
                }
            );
        }

        public static void HideAllDialogs() {
            Dialog[] array = m_dialogs.ToArray();
            for (int i = 0; i < array.Length; i++) {
                HideDialog(array[i]);
            }
        }

        public static void Update() {
            foreach (KeyValuePair<Dialog, AnimationData> animationDatum in m_animationData) {
                Dialog key = animationDatum.Key;
                AnimationData value = animationDatum.Value;
                if (value.Direction > 0) {
                    value.Factor = MathUtils.Min(value.Factor + 6f * Time.FrameDuration, 1f);
                }
                else if (value.Direction < 0) {
                    value.Factor = MathUtils.Max(value.Factor - 6f * Time.FrameDuration, 0f);
                    if (value.Factor <= 0f) {
                        m_toRemove.Add(key);
                    }
                }
                UpdateDialog(key, value);
            }
            foreach (Dialog item in m_toRemove) {
                AnimationData animationData = m_animationData[item];
                m_animationData.Remove(item);
                item.ParentWidget.Children.Remove(item);
                animationData.CoverWidget.ParentWidget.Children.Remove(animationData.CoverWidget);
            }
            m_toRemove.Clear();
        }

        public static void UpdateDialog(Dialog dialog, AnimationData animationData) {
            if (animationData.Factor < 1f) {
                float factor = animationData.Factor;
                float num = 0.75f + 0.25f * MathF.Pow(animationData.Factor, 0.25f);
                dialog.RenderTransform = Matrix.CreateTranslation((0f - dialog.ActualSize.X) / 2f, (0f - dialog.ActualSize.Y) / 2f, 0f)
                    * Matrix.CreateScale(num, num, 1f)
                    * Matrix.CreateTranslation(dialog.ActualSize.X / 2f, dialog.ActualSize.Y / 2f, 0f);
                dialog.ColorTransform = Color.White * factor;
                animationData.CoverWidget.ColorTransform = Color.White * factor;
            }
            else {
                dialog.RenderTransform = Matrix.Identity;
                dialog.ColorTransform = Color.White;
                animationData.CoverWidget.ColorTransform = Color.White;
            }
        }
    }
}