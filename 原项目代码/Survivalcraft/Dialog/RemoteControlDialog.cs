#if !IOS && !BROWSER
using System.Net;
using System.Xml.Linq;

namespace Game {
    public class RemoteControlDialog : Dialog {
        public readonly LabelWidget m_statusLabel;
        public readonly ButtonWidget m_statusButton;
        public readonly LabelWidget m_addressLabel;
        public readonly ButtonWidget m_portButton;
        public readonly LabelWidget m_passwordLabel;
        public readonly ButtonWidget m_passwordButton;
        public readonly ButtonWidget m_closeButton;

        public RemoteControlDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/RemoteControlDialog");
            LoadContents(this, node);
            m_statusLabel = Children.Find<LabelWidget>("RemoteControlDialog.StatusLabel");
            m_statusButton = Children.Find<ButtonWidget>("RemoteControlDialog.StatusButton");
            m_addressLabel = Children.Find<LabelWidget>("RemoteControlDialog.AddressLabel");
            m_portButton = Children.Find<ButtonWidget>("RemoteControlDialog.PortButton");
            m_passwordLabel = Children.Find<LabelWidget>("RemoteControlDialog.PasswordLabel");
            m_passwordButton = Children.Find<ButtonWidget>("RemoteControlDialog.PasswordButton");
            m_closeButton = Children.Find<ButtonWidget>("RemoteControlDialog.CloseButton");
            m_statusLabel.Text = LanguageControl.Get("ContentWidgets", "RemoteControlDialog", JsInterface.httpListener.IsListening ? "4" : "5");
            m_addressLabel.Text = $"http://{IPAddress.Loopback}:{JsInterface.httpPort}/";
            m_passwordLabel.Text = JsInterface.httpPassword;
        }

        public override void Update() {
            if (m_statusButton.IsClicked) {
                if (JsInterface.httpListener.IsListening) {
                    JsInterface.StopHttpListener();
                    ModsManager.SetConfig("RemoteControlEnabled", "false");
                    m_statusLabel.Text = LanguageControl.Get("ContentWidgets", "RemoteControlDialog", "5");
                }
                else {
                    Task.Run(JsInterface.StartHttpListener);
                    ModsManager.SetConfig("RemoteControlEnabled", "true");
                    m_statusLabel.Text = LanguageControl.Get("ContentWidgets", "RemoteControlDialog", "4");
                }
            }
            if (m_portButton.IsClicked) {
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new TextBoxDialog(
                        LanguageControl.Get("ContentWidgets", "RemoteControlDialog", "8"),
                        JsInterface.httpPort.ToString(),
                        5,
                        str => {
                            if (int.TryParse(str, out int port)) {
                                JsInterface.SetHttpPort(port, true);
                                m_addressLabel.Text = $"http://{IPAddress.Loopback}:{port}/";
                            }
                        }
                    )
                );
            }
            if (m_passwordButton.IsClicked) {
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new TextBoxDialog(
                        LanguageControl.Get("ContentWidgets", "RemoteControlDialog", "10"),
                        JsInterface.httpPassword,
                        18,
                        str => {
                            JsInterface.httpPassword = str;
                            ModsManager.SetConfig("RemoteControlPassword", str);
                        }
                    )
                );
            }
            if (Input.Back
                || Input.Cancel
                || m_closeButton.IsClicked) {
                Dismiss();
            }
        }

        public void Dismiss() {
            DialogsManager.HideDialog(this);
        }
    }
}
#else
namespace Game {
    public class RemoteControlDialog : Dialog {

    }
}
#endif
