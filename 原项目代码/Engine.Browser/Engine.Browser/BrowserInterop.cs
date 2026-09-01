#pragma warning disable CA1416
using System.Runtime.InteropServices.JavaScript;
using Engine.Input;

namespace Engine.Browser {
    public static partial class BrowserInterop {
        //main.js should be in the final project with <OutputType>Exe</OutputType>
        [JSImport("initialize", "main.js")]
        public static partial void Initialize(IntPtr sharedInputMemoryPtr);

        [JSImport("getTitle", "main.js")]
        public static partial string GetTitle();

        [JSImport("setTitle", "main.js")]
        public static partial void SetTitle(string title);

        [JSImport("getLanguage", "main.js")]
        public static partial string GetLanguage();

        [JSImport("close", "main.js")]
        public static partial void Close();

        [JSImport("reload", "main.js")]
        public static partial void Reload();

        [JSImport("setDocumentLang", "main.js")]
        public static partial void SetDocumentLang(string lang);

        [JSImport("openUrlInNewTab", "main.js")]
        public static partial void OpenUrlInNewTab(string url);

        [JSImport("setNeedPointerLock", "main.js")]
        public static partial void SetNeedPointerLock(bool need);

        [JSImport("showOpenFilePicker", "main.js")]
        public static partial Task<JSObject> ShowOpenFilePicker(string[] descAndExtArray, int[] extCounts, string defaultPath);

        [JSImport("getFileName", "main.js")]
        public static partial string GetFileName(JSObject file);

        [JSImport("getFileBytes", "main.js")]
        public static partial Task<JSObject> GetFileBytes(JSObject file);

        [JSImport("returnSelf", "main.js")]
        public static partial byte[] JSObject2ByteArray(JSObject obj);

        [JSImport("showSaveFilePicker", "main.js")]
        public static partial Task<JSObject> ShowSaveFilePicker(string fileName, string mimeType);

        [JSImport("saveBytesToFileHandle", "main.js")]
        public static partial Task SaveBytesToFileHandle(JSObject fileHandle, byte[] bytes);

        [JSImport("setFullscreen", "main.js")]
        public static partial void SetFullscreen(bool fullscreen);

        [JSImport("showKeyboard", "main.js")]
        public static partial string ShowKeyboard(string title, string defaultText);

        [JSImport("setContentPtr", "main.js")]
        public static partial void SetContentPtr(IntPtr ptr);

        [JSImport("firstFramePrepared", "main.js")]
        public static partial void FirstFramePrepared();

        [JSExport]
        public static async Task OnGamepadConnected(int index, string name) => GamePad.GamepadConnectedHandler(index, name);

        [JSExport]
        public static async Task OnDrop(byte[] data, string fileName) {
            Stream stream = new MemoryStream(data);
            stream.Position = 0;
            Window.FileDropHandler(stream, fileName);
        }
    }
}