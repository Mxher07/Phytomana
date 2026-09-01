using Silk.NET.Windowing.Sdl.iOS;
using SurvivalCraft.IOS;
using System.Reflection;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
// This is the main entry point of the application.
// If you want to use a different Application Delegate class from "AppDelegate"
// you can specify it here.
//UIApplication.Main(args, null, typeof(AppDelegate));




unsafe {

    static nint ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        nint libHandle = 0;
        if (libraryName.StartsWith("bass")) {
            NativeLibrary.TryLoad($"./Frameworks/{libraryName}.framework/{libraryName}", assembly, DllImportSearchPath.ApplicationDirectory, out libHandle);
        }
        return libHandle;
    }
    SilkMobile.RunApp(0, null, _ => {
        NativeLibrary.SetDllImportResolver(typeof(BassNet).Assembly, ImportResolver);
        Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);


        int plugin = Bass.BASS_PluginLoad("bassflac");
        if(plugin == 0)
            throw new Exception("BASS_PluginLoad(bassflac) failed: " + Bass.BASS_ErrorGetCode());

        Engine.Window.Created += () => {
            var kitValue = Engine.Window.m_view.Native.UIKit.Value;
            var window = ObjCRuntime.Runtime.GetNSObject<UIWindow>(kitValue.Window);
            var uiView = window.RootViewController.View;
            uiView.Add(new TouchView(uiView));
        };
        Game.Program.EntryPoint();
    });
}


