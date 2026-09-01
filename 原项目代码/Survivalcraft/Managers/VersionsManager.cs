using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public static class VersionsManager {
        public static List<VersionConverter> m_versionConverters;

        public enum Platform {
            Windows = 0, Android = 1, Linux = 2, Other = 3, Browser = 4, IOS = 5
        }

        public static string PlatformString => CurrentPlatform.ToString();

        public static Platform CurrentPlatform {
            get {
#if WINDOWS
                return Platform.Windows;
#elif ANDROID
                return Platform.Android;
#elif LINUX
                return Platform.Linux;
#elif BROWSER
                return Platform.Browser;
#elif IOS
                return Platform.IOS;
#else
                return Platform.Other;
#endif
            }
        }

        /// <summary>
        ///     Win32NT:Windows
        ///     Unix:Linux
        /// </summary>
        public static PlatformID PlatformID = Environment.OSVersion.Platform;

        public static string PlatformTag = RuntimeInformation.OSDescription;
        public static BuildConfiguration BuildConfiguration => BuildConfiguration.Release;

        public static string Version { get; set; }

        public static string SerializationVersion { get; set; }

        public static string LastLaunchedVersion { get; set; }

        static VersionsManager() {
            m_versionConverters = []; //List
            Assembly assembly = typeof(VersionsManager).GetTypeInfo().Assembly;
            AssemblyName assemblyName = new(assembly.FullName!);
            Version = $"{assemblyName.Version!.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}.{assemblyName.Version.Revision}";
            SerializationVersion = $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}";
#pragma warning disable IL2026
            foreach (TypeInfo definedType in assembly.DefinedTypes) {
#pragma warning restore IL2026
                if (!definedType.IsAbstract
                    && !definedType.IsInterface
                    && typeof(VersionConverter).GetTypeInfo().IsAssignableFrom(definedType)) {
#pragma warning disable IL2072
                    VersionConverter item = (VersionConverter)Activator.CreateInstance(definedType.AsType());
#pragma warning restore IL2072
                    m_versionConverters.Add(item);
                }
            }
        }

        public static void Initialize() {
            LastLaunchedVersion = SettingsManager.LastLaunchedVersion;
            SettingsManager.LastLaunchedVersion = Version;
            if (Version != LastLaunchedVersion) { }
        }

        public static void UpgradeProjectXml(XElement projectNode) {
            string attributeValue = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
            if (attributeValue != SerializationVersion) {
                foreach (VersionConverter item in FindTransform(attributeValue, SerializationVersion, m_versionConverters, 0)
                    ?? throw new InvalidOperationException(
                        $"Cannot find conversion path from version \"{attributeValue}\" to version \"{SerializationVersion}\""
                    )) {
                    Log.Information($"Upgrading world version \"{item.SourceVersion}\" to \"{item.TargetVersion}\".");
                    item.ConvertProjectXml(projectNode);
                }
                string attributeValue2 = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
                if (attributeValue2 != SerializationVersion) {
                    throw new InvalidOperationException(
                        $"Upgrade produced invalid project version. Expected \"{SerializationVersion}\", found \"{attributeValue2}\"."
                    );
                }
            }
        }

        public static void UpgradeWorld(string directoryName) {
            WorldInfo worldInfo = WorldsManager.GetWorldInfo(directoryName);
            if (worldInfo == null) {
                throw new InvalidOperationException($"Cannot determine version of world at \"{directoryName}\"");
            }
            if (worldInfo.SerializationVersion != SerializationVersion) {
                ProgressManager.UpdateProgress($"Upgrading World To {SerializationVersion}", 0f);
                foreach (VersionConverter item in FindTransform(worldInfo.SerializationVersion, SerializationVersion, m_versionConverters, 0)
                    ?? throw new InvalidOperationException(
                        $"Cannot find conversion path from version \"{worldInfo.SerializationVersion}\" to version \"{SerializationVersion}\""
                    )) {
                    Log.Information($"Upgrading world version \"{item.SourceVersion}\" to \"{item.TargetVersion}\".");
                    item.ConvertWorld(directoryName);
                }
                WorldInfo worldInfo2 = WorldsManager.GetWorldInfo(directoryName);
                if (worldInfo2.SerializationVersion != SerializationVersion) {
                    throw new InvalidOperationException(
                        $"Upgrade produced invalid project version. Expected \"{SerializationVersion}\", found \"{worldInfo2.SerializationVersion}\"."
                    );
                }
            }
        }

        public static int CompareVersions(string v1, string v2) {
            string[] array = v1.Split('.');
            string[] array2 = v2.Split('.');
            for (int i = 0; i < MathUtils.Min(array.Length, array2.Length); i++) {
                int num = !int.TryParse(array[i], out int result) || !int.TryParse(array2[i], out int result2)
                    ? string.CompareOrdinal(array[i], array2[i])
                    : result - result2;
                if (num != 0) {
                    return num;
                }
            }
            return array.Length - array2.Length;
        }

        public static List<VersionConverter> FindTransform(string sourceVersion,
            string targetVersion,
            IEnumerable<VersionConverter> converters,
            int depth) {
            if (depth > 100) {
                throw new InvalidOperationException(
                    "Too deep recursion when searching for version converters. Check for possible loops in transforms."
                );
            }
            if (sourceVersion == targetVersion) {
                return [];
            }
            List<VersionConverter> result = null;
            int num = 2147483647;
            IEnumerable<VersionConverter> versionConverters = converters as List<VersionConverter> ?? converters.ToList();
            foreach (VersionConverter converter in versionConverters) {
                if (converter.SourceVersion == sourceVersion) {
                    List<VersionConverter> list = FindTransform(converter.TargetVersion, targetVersion, versionConverters, depth + 1);
                    if (list != null
                        && list.Count < num) {
                        num = list.Count;
                        list.Insert(0, converter);
                        result = list;
                    }
                }
            }
            return result;
        }
    }
}