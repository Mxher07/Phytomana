using System.Text.Json.Nodes;
using System.Xml.Linq;
using Engine;

namespace Game {
    public class ViewGameLogDialog : Dialog {
        public ListPanelWidget m_listPanel;

        public ButtonWidget m_copyButton, m_filterButton, m_closeButton, m_uploadButton, m_clearButton;

        public LogType m_filter;

        public static string fName = "ViewGameLogDialog";

        public LabelWidget m_headText, m_adviceText;

        public void SetErrorHead(int headLangIndex, int adviceLangIndex) {
            m_headText.Text = LanguageControl.Get(fName, headLangIndex);
            m_adviceText.Text = LanguageControl.Get(fName, adviceLangIndex);
        }

        public ViewGameLogDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/ViewGameLogDialog");
            LoadContents(this, node);
            m_listPanel = Children.Find<ListPanelWidget>("ViewGameLogDialog.ListPanel");
            m_clearButton = Children.Find<ButtonWidget>("ViewGameLogDialog.Clear");
            m_copyButton = Children.Find<ButtonWidget>("ViewGameLogDialog.Copy");
            m_filterButton = Children.Find<ButtonWidget>("ViewGameLogDialog.Filter");
            m_filterButton.Style = ContentManager.Get<XElement>("Styles/ButtonStyle_160x60");
            m_closeButton = Children.Find<ButtonWidget>("ViewGameLogDialog.Close");
            m_uploadButton = Children.Find<ButtonWidget>("ViewGameLogDialog.Upload");
            m_headText = Children.Find<LabelWidget>("HeadText");
            m_adviceText = Children.Find<LabelWidget>("AdviceText");
            m_listPanel.ItemClicked += delegate(object item) {
                if (m_listPanel.SelectedItem == item) {
                    DialogsManager.ShowDialog(
                        ParentWidget,
                        new MessageDialog(
                            "Log Item",
                            item.ToString(),
                            LanguageControl.Ok,
                            LanguageControl.Get(fName, 13),
                            button => {
                                if (button == MessageDialogButton.Button2) {
                                    ClipboardManager.ClipboardString = item.ToString();
                                }
                            }
                        )
                    );
                }
            };
            PopulateList();
        }

        public override void Update() {
            if (m_clearButton.IsClicked) {
                GameLogSink.m_stream.SetLength(0);
                m_listPanel.ClearItems();
            }
            if (m_copyButton.IsClicked) {
                ClipboardManager.ClipboardString = GameLogSink.GetRecentLog(131072);
            }
            if (m_filterButton.IsClicked) {
                m_filter = m_filter < LogType.Warning ? LogType.Warning :
                    m_filter < LogType.Error ? LogType.Error : LogType.Debug;
                PopulateList();
            }
            if (Input.Cancel
                || m_closeButton.IsClicked) {
                DialogsManager.HideDialog(this);
            }
            if (m_filter == LogType.Debug) {
                m_filterButton.Text = LanguageControl.Get(fName, "15");
            }
            else if (m_filter == LogType.Warning) {
                m_filterButton.Text = LanguageControl.Get(fName, "16");
            }
            else if (m_filter == LogType.Error) {
                m_filterButton.Text = LanguageControl.Get(fName, "17");
            }
            if (m_uploadButton.IsClicked) {
                if (string.IsNullOrEmpty(SettingsManager.ScpboxAccessToken)) {
                    MessageDialog messageDialog = new(
                        LanguageControl.Get(fName, 1),
                        LanguageControl.Get(fName, 2),
                        LanguageControl.Get(fName, 3),
                        LanguageControl.Get(fName, 4),
                        _ => { DialogsManager.HideAllDialogs(); }
                    );
                    DialogsManager.ShowDialog(this, messageDialog);
                }
                else {
                    CancellableProgress cancellableProgress = new();
                    CancellableBusyDialog dialog = new(LanguageControl.Get(fName, 5), true);
                    DialogsManager.ShowDialog(this, dialog);
                    JsonObject jsonObject = new();
                    Dictionary<string, string> dictionary = new();
                    jsonObject.Add("path", $"/GameLog/{DateTime.Now.Ticks}.log");
                    dictionary.Add("Authorization", $"Bearer {SettingsManager.ScpboxAccessToken}");
                    dictionary.Add("Content-Type", "application/octet-stream");
                    dictionary.Add("Dropbox-API-Arg", jsonObject.ToJsonString());
                    MemoryStream memoryStream = new();
                    GameLogSink.m_stream.Seek(0, SeekOrigin.Begin);
                    GameLogSink.m_stream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    WebManager.Post(
                        $"{CommunityServerManager.CurrentChineseInfo.ApiUrl}/com/files/upload",
                        null,
                        dictionary,
                        memoryStream,
                        cancellableProgress,
                        delegate {
                            dialog.LargeMessage = LanguageControl.Get(fName, 6);
                            dialog.m_cancelButtonWidget.Text = "OK";
                            GameLogSink.m_writer.BaseStream.SetLength(0);
                            GameLogSink.m_writer.Flush();
                            PopulateList();
                        },
                        delegate(Exception error) {
                            dialog.LargeMessage = LanguageControl.Get(fName, 7);
                            dialog.SmallMessage = error.Message;
                        }
                    );
                }
            }
        }

        public void PopulateList() {
            m_listPanel.ItemWidgetFactory = delegate(object item) {
                string text = item != null ? item.ToString() : string.Empty;
                Color color = Color.Gray;
                if (text.Contains("ERROR:")) {
                    color = Color.Red;
                }
                else if (text.Contains("WARNING:")) {
                    color = Color.DarkYellow;
                }
                else if (text.Contains("INFO:")) {
                    color = Color.LightGray;
                }
                ModsManager.HookAction(
                    "ModifyLogColor",
                    loader => {
                        loader.ModifyLogColor(text, ref color);
                        return false;
                    }
                );
                return new LabelWidget {
                    Text = text,
                    Font = LabelWidget.BitmapFont,
                    HorizontalAlignment = WidgetAlignment.Near,
                    VerticalAlignment = WidgetAlignment.Center,
                    FontScale = 0.6f,
                    Color = color,
                    Ellipsis = true
                };
            };
            List<string> recentLogLines = GameLogSink.GetRecentLogLines(131072);
            m_listPanel.ClearItems();
            if (recentLogLines.Count > 1000) {
                recentLogLines.RemoveRange(0, recentLogLines.Count - 1000);
            }
            foreach (string item in recentLogLines) {
                if (m_filter == LogType.Warning) {
                    if (!item.Contains("WARNING:")
                        && !item.Contains("ERROR:")) {
                        continue;
                    }
                }
                else if (m_filter == LogType.Error
                    && !item.Contains("ERROR:")) {
                    continue;
                }
                m_listPanel.AddItem(item);
            }
            m_listPanel.ScrollPosition = m_listPanel.Items.Count * m_listPanel.ItemSize;
        }
    }
}