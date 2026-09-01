using System.Diagnostics;
using System.Xml.Linq;

namespace Game {
    public class RunJsDialog : Dialog {
        public readonly TextBoxWidget m_inputBox;
        public readonly LabelWidget m_outputBox;

        public readonly LabelWidget m_timeCostedLabel;

        public readonly ButtonWidget m_runButton;
        public readonly ButtonWidget m_copyOutputButton;
        public readonly ButtonWidget m_closeButton;
        public readonly ButtonWidget m_serverButton;

        public RunJsDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/RunJsDialog");
            LoadContents(this, node);
            m_inputBox = Children.Find<TextBoxWidget>("RunJsDialog.Input");
            m_outputBox = Children.Find<LabelWidget>("RunJsDialog.Output");
            m_timeCostedLabel = Children.Find<LabelWidget>("RunJsDialog.TimeCosted");
            m_runButton = Children.Find<ButtonWidget>("RunJsDialog.RunButton");
            m_copyOutputButton = Children.Find<ButtonWidget>("RunJsDialog.CopyOutputButton");
            m_closeButton = Children.Find<ButtonWidget>("RunJsDialog.CloseButton");
            m_serverButton = Children.Find<ButtonWidget>("RunJsDialog.ServerButton");
            m_inputBox.HasFocus = true;
            m_inputBox.Enter += delegate { Dismiss(true); };
            m_inputBox.Escape += delegate { Dismiss(false); };
        }

        public override void Update() {
            if (Input.Back
                || Input.Cancel) {
                Dismiss(false);
            }
            else if (Input.Ok) {
                Dismiss(true);
            }
            else if (m_runButton.IsClicked) {
                Dismiss(true);
            }
            else if (m_copyOutputButton.IsClicked) {
                ClipboardManager.ClipboardString = m_outputBox.Text;
            }
            else if (m_closeButton.IsClicked) {
                Dismiss(false);
            }
            else if (m_serverButton.IsClicked) {
#if !IOS && !BROWSER
                DialogsManager.ShowDialog(ParentWidget, new RemoteControlDialog());
#endif
            }
        }

        public void Dismiss(bool flag) {
            if (flag) {
#if !IOS && !BROWSER
                Stopwatch stopwatch = Stopwatch.StartNew();
                string result = JsInterface.Evaluate(m_inputBox.Text);
                stopwatch.Stop();
                TimeSpan timeCosted = stopwatch.Elapsed;
                m_outputBox.Text = result;
                m_timeCostedLabel.Text = $"{Math.Floor(timeCosted.TotalSeconds)}s {timeCosted.Milliseconds}ms";
#endif
            }
            else {
                DialogsManager.HideDialog(this);
            }
        }
    }
}