#if !ANDROID
using System.Diagnostics;
#endif
using Engine;

namespace Game {
    public static class WebBrowserManager {
        public static void LaunchBrowser(string url) {
            if (string.IsNullOrEmpty(url)) {
                return;
            }
#if !BROWSER
            if (!url.Contains("://")) {
                url = $"https://{url}";
            }
#endif
            try {
#if ANDROID
                Window.Activity.OpenLink(url);
#elif BROWSER
                Engine.Browser.BrowserInterop.OpenUrlInNewTab(url);
#else
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
#endif
            }
            catch (Exception ex) {
                Log.Error($"Error launching web browser with URL \"{url}\". Reason: {ex.Message}");
            }
        }
    }
}