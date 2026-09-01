#if WINDOWS
#pragma warning disable CA1416
using Microsoft.Win32;

namespace Game.Managers {
    static class WindowsRegisterManager {
        public static string SurvivalcraftPath => $"{ModsManager.ExternalPath}Survivalcraft.exe";

        public static void RegisterFileType(string keyName, string keyValue, string extension) {
            //keyName = "WPCFile";
            //keyValue = "资源包文件";
            //bool isCreateRegistry = true;
            try {
                // 检查 文件关联是否创建
                RegistryKey isExCommand = Registry.ClassesRoot.OpenSubKey(keyName);
                if (isExCommand == null) {
                    //isCreateRegistry = true;
                }
                else {
                    if (isExCommand.GetValue("Create")?.ToString() == SurvivalcraftPath) {
                        //isCreateRegistry = false;
                    }
                    else {
                        Registry.ClassesRoot.DeleteSubKeyTree(keyName);
                        //isCreateRegistry = true;
                    }
                }
            }
            catch (Exception) {
                //isCreateRegistry = true;
            }

            // 假如 文件关联 还没有创建，或是关联位置已被改变
            //if (isCreateRegistry) 
            {
                try {
                    if (SurvivalcraftPath == null) {
                        return;
                    }
                    RegistryKey key = Registry.ClassesRoot.CreateSubKey(keyName);
                    if (key == null) {
                        return;
                    }
                    key.SetValue("Create", SurvivalcraftPath);
                    RegistryKey keyico = key.CreateSubKey("DefaultIcon");
                    if (keyico == null) {
                        return;
                    }
                    keyico.SetValue("", $"{SurvivalcraftPath},0");
                    key.SetValue("", keyValue);
                    key = key.CreateSubKey("Shell");
                    if (key == null) {
                        return;
                    }
                    key = key.CreateSubKey("Open");
                    if (key == null) {
                        return;
                    }
                    key = key.CreateSubKey("Command");
                    if (key == null) {
                        return;
                    }

                    // 关联的位置
                    key.SetValue("", $@"{SurvivalcraftPath} %1/");

                    // 关联的文件扩展名,
                    keyName = extension;
                    key = Registry.ClassesRoot.CreateSubKey(keyName);
                    if (key == null) {
                        return;
                    }
                    key.SetValue("", keyValue);
                }
                catch (Exception) {
                    // ignored
                }
            }
        }
    }
}
#endif