using Engine;

namespace Game {
    public class LoginDialog : Dialog {
        public Action<byte[]> succ;
        public Action<Exception> fail;
        public StackPanelWidget MainView;
        public BevelledButtonWidget btna, btnb, btnc;
        public TextBoxWidget txa, txb;
        public BusyDialog busyDialog = new("提示", "登录中");

        public LabelWidget tip = new() {
            HorizontalAlignment = WidgetAlignment.Near, VerticalAlignment = WidgetAlignment.Near, Margin = new Vector2(1f, 1f)
        };

        public Action cancel;

        public LoginDialog() {
            CanvasWidget canvasWidget = new() {
                Size = new Vector2(600f, 240f), HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center
            };
            RectangleWidget rectangleWidget = new() {
                FillColor = new Color(0, 0, 0, 255), OutlineColor = new Color(128, 128, 128, 128), OutlineThickness = 2
            };
            StackPanelWidget stackPanelWidget = new() {
                Direction = LayoutDirection.Vertical,
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Near,
                Margin = new Vector2(10f, 10f)
            };
            Children.Add(canvasWidget);
            canvasWidget.Children.Add(rectangleWidget);
            canvasWidget.Children.Add(stackPanelWidget);
            MainView = stackPanelWidget;
            MainView.Children.Add(tip);
            MainView.Children.Add(makeTextBox("账号:"));
            MainView.Children.Add(makeTextBox("密码:", true));
            MainView.Children.Add(makeButton());
        }

        public Widget makeTextBox(string title, bool passwordMode = false) {
            CanvasWidget canvasWidget = new() { Margin = new Vector2(10, 0) };
            RectangleWidget rectangleWidget = new() {
                FillColor = Color.Black, OutlineColor = Color.White, Size = new Vector2(float.PositiveInfinity, 80)
            };
            StackPanelWidget stack = new() { Direction = LayoutDirection.Horizontal };
            LabelWidget label = new() {
                HorizontalAlignment = WidgetAlignment.Near, VerticalAlignment = WidgetAlignment.Near, Text = title, Margin = new Vector2(1f, 1f)
            };
            TextBoxWidget textBox = new() {
                PasswordMode = passwordMode,
                VerticalAlignment = WidgetAlignment.Center,
                HorizontalAlignment = WidgetAlignment.Stretch,
                Color = new Color(255, 255, 255),
                Margin = new Vector2(4f, 0f),
                Size = new Vector2(float.PositiveInfinity, 80)
            };
            if (title == "账号:") {
                txa = textBox;
            }
            if (title == "密码:") {
                txb = textBox;
            }
            stack.Children.Add(label);
            stack.Children.Add(textBox);
            canvasWidget.Children.Add(rectangleWidget);
            canvasWidget.Children.Add(stack);
            return canvasWidget;
        }

        public Widget makeButton() {
            StackPanelWidget stack = new() { Direction = LayoutDirection.Horizontal };
            BevelledButtonWidget bevelledButtonWidget1 = new() { Size = new Vector2(160, 60), Margin = new Vector2(4f, 0), Text = "登录" };
            BevelledButtonWidget bevelledButtonWidget2 = new() { Size = new Vector2(160, 60), Margin = new Vector2(4f, 0), Text = "注册" };
            BevelledButtonWidget bevelledButtonWidget3 = new() { Size = new Vector2(160, 60), Margin = new Vector2(4f, 0), Text = "取消" };
            stack.Children.Add(bevelledButtonWidget1);
            stack.Children.Add(bevelledButtonWidget2);
            stack.Children.Add(bevelledButtonWidget3);
            btna = bevelledButtonWidget1;
            btnb = bevelledButtonWidget2;
            btnc = bevelledButtonWidget3;
            return stack;
        }

        public override void Update() {
            if (btna.IsClicked) {
                Dictionary<string, string> par = new() { { "user", txa.Text }, { "pass", txb.Text } };
                DialogsManager.ShowDialog(this, busyDialog);
                WebManager.Post(
                    $"{CommunityServerManager.CurrentChineseInfo.ApiUrl}/com/api/login",
                    par,
                    null,
                    new MemoryStream(),
                    new CancellableProgress(),
                    succ,
                    fail
                );
            }
            if (btnb.IsClicked) {
                WebBrowserManager.LaunchBrowser($"{CommunityServerManager.CurrentChineseInfo.ApiUrl}/com/reg");
            }
            if (btnc.IsClicked) {
                cancel?.Invoke();
            }
        }
    }
}