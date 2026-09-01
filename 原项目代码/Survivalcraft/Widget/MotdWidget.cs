using System.Text.Json;
using Engine;
using Engine.Input;

namespace Game {
    public class MotdWidget : CanvasWidget {
        public class LineData {
            public float Time;

            public Widget Widget;
        }

        public CanvasWidget m_containerWidget;

        public List<LineData> m_lines = [];

        public int m_currentLineIndex;

        public double m_lastLineChangeTime;

        public int m_tapsCount;

        public bool Noticed;

        public MotdWidget() {
            m_containerWidget = new CanvasWidget();
            Children.Add(m_containerWidget);
            MotdManager.MessageOfTheDayUpdated += MotdManager_MessageOfTheDayUpdated;
            MotdManager_MessageOfTheDayUpdated();
        }

        public override void Update() {
            if (!Noticed
                && MotdManager.UpdateResult != null) {
                JsonElement jsonDocument = MotdManager.UpdateResult.RootElement;
                if (jsonDocument.GetProperty("update").GetString() == "1") {
                    try {
                        DialogsManager.ShowDialog(
                            ScreensManager.m_screens["MainMenu"],
                            new MessageDialog(
                                jsonDocument.GetProperty("title").GetString(),
                                jsonDocument.GetProperty("content").GetString(),
                                jsonDocument.GetProperty("btn").GetString(),
                                LanguageControl.Cancel,
                                btn => {
                                    if (btn == MessageDialogButton.Button1) {
                                        WebBrowserManager.LaunchBrowser(jsonDocument.GetProperty("url").GetString());
                                    }
                                }
                            )
                        );
                    }
                    catch (Exception e) {
                        Log.Error($"Failed processing Update check. Reason: {e.Message}");
                    }
                    finally {
                        Noticed = true;
                    }
                }
            }
            if (Input.Tap.HasValue) {
                Widget widget = HitTestGlobal(Input.Tap.Value);
                if (widget != null
                    && (widget == this || widget.IsChildWidgetOf(this))) {
                    m_tapsCount++;
                }
            }
            if (m_tapsCount >= 5) {
                m_tapsCount = 0;
                MotdManager.ForceRedownload();
                AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            }
            if (Input.IsKeyDownOnce(Key.PageUp)) {
                GotoLine(m_currentLineIndex - 1);
            }
            if (Input.IsKeyDownOnce(Key.PageDown)) {
                GotoLine(m_currentLineIndex + 1);
            }
            if (m_lines.Count > 0) {
                m_currentLineIndex %= m_lines.Count;
                double realTime = Time.RealTime;
                if (m_lastLineChangeTime == 0.0
                    || realTime - m_lastLineChangeTime >= m_lines[m_currentLineIndex].Time) {
                    GotoLine(m_lastLineChangeTime != 0.0 ? m_currentLineIndex + 1 : 0);
                }
                float num2 = (float)(realTime - m_lastLineChangeTime);
                float num3 = (float)(m_lastLineChangeTime + m_lines[m_currentLineIndex].Time - 0.33000001311302185 - realTime);
                SetWidgetPosition(
                    position: new Vector2(
                        !(num2 < num3)
                            ? ActualSize.X * (1f - MathUtils.PowSign(MathF.Sin(MathUtils.Saturate(1.5f * num3) * (float)Math.PI / 2f), 0.33f))
                            : ActualSize.X * (MathUtils.PowSign(MathF.Sin(MathUtils.Saturate(1.5f * num2) * (float)Math.PI / 2f), 0.33f) - 1f),
                        0f
                    ),
                    widget: m_containerWidget
                );
                m_containerWidget.Size = ActualSize;
            }
            else {
                m_containerWidget.Children.Clear();
            }
        }

        public void GotoLine(int index) {
            if (m_lines.Count > 0) {
                m_currentLineIndex = MathUtils.Max(index, 0) % m_lines.Count;
                m_containerWidget.Children.Clear();
                m_containerWidget.Children.Add(m_lines[m_currentLineIndex].Widget);
                m_lastLineChangeTime = Time.RealTime;
                m_tapsCount = 0;
            }
        }

        public void Restart() {
            m_currentLineIndex = 0;
            m_lastLineChangeTime = 0.0;
        }

        public void MotdManager_MessageOfTheDayUpdated() {
            m_lines.Clear();
            if (MotdManager.MessageOfTheDay != null) {
                foreach (MotdManager.Line line in MotdManager.MessageOfTheDay.Lines) {
                    try {
                        LineData item = ParseLine(line);
                        m_lines.Add(item);
                    }
                    catch (Exception ex) {
                        Log.Warning($"Error loading MOTD line {MotdManager.MessageOfTheDay.Lines.IndexOf(line) + 1}. Reason: {ex.Message}");
                    }
                }
            }
            Restart();
        }

        public LineData ParseLine(MotdManager.Line line) {
            LineData lineData = new() { Time = line.Time };
            if (line.Node != null) {
                lineData.Widget = LoadWidget(null, line.Node, null);
            }
            else {
                if (string.IsNullOrEmpty(line.Text)) {
                    throw new InvalidOperationException("Invalid MOTD line.");
                }
                StackPanelWidget stackPanelWidget = new() {
                    Direction = LayoutDirection.Vertical, HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center
                };
                string[] array = line.Text.Replace("\r", "").Split(["\n"], StringSplitOptions.None);
                for (int i = 0; i < array.Length; i++) {
                    string text = array[i].Trim();
                    if (!string.IsNullOrEmpty(text)) {
                        LabelWidget widget = new() {
                            Text = text, HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center, DropShadow = true
                        };
                        stackPanelWidget.Children.Add(widget);
                    }
                }
                lineData.Widget = stackPanelWidget;
            }
            return lineData;
        }
    }
}