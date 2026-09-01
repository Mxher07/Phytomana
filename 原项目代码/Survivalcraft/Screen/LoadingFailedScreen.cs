using Engine;
using Engine.Graphics;

namespace Game {
    public class LoadingFailedScreen : Screen {
        public ButtonWidget m_enableSafeModeButton;
        public LoadingFailedScreen(string title, IEnumerable<string> details, IEnumerable<string> solveMethods) {
            Children.Clear();
            Children.Add(
                new RectangleWidget {
                    Size = new Vector2(float.PositiveInfinity), FillColor = Color.Black, OutlineColor = Color.Black, OutlineThickness = 0
                }
            );
            ScrollPanelWidget scrollPanelWidget = new() { HorizontalAlignment = WidgetAlignment.Center, Direction = LayoutDirection.Vertical };
            StackPanelWidget widget = new() { Direction = LayoutDirection.Vertical, HorizontalAlignment = WidgetAlignment.Center, Margin = new Vector2(20f)};
            scrollPanelWidget.Children.Add(widget);
            {
                widget.Children.Add(
                    new LabelWidget { Text = title, FontScale = 2, Color = Color.Red, HorizontalAlignment = WidgetAlignment.Center, WordWrap = true }
                );
                widget.Children.Add(new RectangleWidget { ColorTransform = Color.Transparent, Size = new Vector2(float.PositiveInfinity, 40f) });
                foreach (string detail in details) {
                    widget.Children.Add(new LabelWidget { Text = detail, WordWrap = true });
                }
                widget.Children.Add(new RectangleWidget { ColorTransform = Color.Transparent, Size = new Vector2(float.PositiveInfinity, 20f) });
                widget.Children.Add(
                    new LabelWidget {
                        Text = "For solving this problem, please try: \n要解决此问题，请尝试：",
                        Color = Color.Green,
                        WordWrap = true,
                        Margin = new Vector2(0f, 8f)
                    }
                );
                foreach (string method in solveMethods) {
                    widget.Children.Add(new LabelWidget { Text = method, WordWrap = true, Margin = new Vector2(0f, 4f)});
                }
                m_enableSafeModeButton = new BevelledButtonWidget { Text = "Safe Mode 安全模式", Size = new Vector2(310f, 60f), HorizontalAlignment = WidgetAlignment.Center, Margin = new Vector2(0f, 10f)};
                widget.Children.Add(m_enableSafeModeButton);
            }
            Children.Add(scrollPanelWidget);
        }

        public override void Update() {
            if (m_enableSafeModeButton.IsClicked) {
                SettingsManager.SafeMode = true;
                DialogsManager.ShowDialog(null, new MessageDialog("Need Restarting 需要重启", null, "OK", null, null));
            }
        }
    }
}