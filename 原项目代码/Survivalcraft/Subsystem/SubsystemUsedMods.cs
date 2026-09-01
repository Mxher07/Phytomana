using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemUsedMods : Subsystem {
        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary modsDict = new();
            int i = 0;
            foreach (ModEntity modEntity in ModsManager.ModList) {
                if (modEntity is SurvivalCraftModEntity
                    || modEntity is FastDebugModEntity) {
                    continue;
                }
                if (modEntity.modInfo.NonPersistentMod) {
                    continue;
                }
                ValuesDictionary modInfoDict = new();
                modInfoDict.SetValue("Name", modEntity.modInfo.Name);
                modInfoDict.SetValue("Version", modEntity.modInfo.Version);
                modInfoDict.SetValue("ApiVersion", modEntity.modInfo.ApiVersion);
                modInfoDict.SetValue("ScVersion", modEntity.modInfo.ScVersion);
                modInfoDict.SetValue("Author", modEntity.modInfo.Author);
                modInfoDict.SetValue("Link", modEntity.modInfo.Link);
                modInfoDict.SetValue("PackageName", modEntity.modInfo.PackageName);
                modsDict.SetValue(i.ToString(), modInfoDict);
                i++;
            }
            valuesDictionary.SetValue("ModsCount", i);
            valuesDictionary.SetValue("Mods", modsDict);
        }
    }
}