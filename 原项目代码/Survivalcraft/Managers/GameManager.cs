using System.Xml.Linq;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;
using XmlUtilities;

namespace Game {
    public static class GameManager {
        public static WorldInfo m_worldInfo;

        public static Project m_project;

        public static SubsystemUpdate m_subsystemUpdate;

        public static ManualResetEvent m_saveCompleted = new(true);

        public static Project Project => m_project;

        public static WorldInfo WorldInfo => m_worldInfo;

        public static List<Func<bool>> SyncDispatcher = [];

        public static event Action<Project> ProjectDisposed;

        public const string fName = "GameManager";

        public static void RepairAndUpgradeWorld(WorldInfo worldInfo) {
            WorldsManager.RepairWorldIfNeeded(worldInfo.DirectoryName);
            VersionsManager.UpgradeWorld(worldInfo.DirectoryName);
        }

        public static void LoadProject(WorldInfo worldInfo, ContainerWidget gamesWidget) {
            DisposeProject();
            WorldsManager.RepairWorldIfNeeded(worldInfo.DirectoryName);
            VersionsManager.UpgradeWorld(worldInfo.DirectoryName);
            BlocksManager.LoadBlocksStaticly = string.IsNullOrEmpty(worldInfo.APIVersion);
            using (Stream stream = Storage.OpenFile(Storage.CombinePaths(worldInfo.DirectoryName, "Project.xml"), OpenFileMode.Read)) {
                ValuesDictionary valuesDictionary = new();
                ValuesDictionary valuesDictionary2 = new();
                valuesDictionary.SetValue("GameInfo", valuesDictionary2);
                valuesDictionary2.SetValue("WorldDirectoryName", worldInfo.DirectoryName);
                ValuesDictionary valuesDictionary3 = new();
                valuesDictionary.SetValue("Views", valuesDictionary3);
                valuesDictionary3.SetValue("GamesWidget", gamesWidget);
                XElement projectNode = XmlUtils.LoadXmlFromStream(stream, null, true);
                ModsManager.HookAction(
                    "ProjectXmlLoad",
                    loader => {
#pragma warning disable CS0618
                        loader.ProjectXmlLoad(projectNode);
#pragma warning restore CS0618
                        loader.ProjectXmlLoad(projectNode, worldInfo, gamesWidget);
                        return false;
                    }
                );
                Project.OnProjectLoad += delegate(Project project) {
                    ModsManager.HookAction(
                        "OnProjectLoaded",
                        loader => {
                            loader.OnProjectLoaded(project);
                            return false;
                        }
                    );
                };
                Project.EntityAdded += (_, arg) => {
                    ModsManager.HookAction(
                        "OnEntityAdd",
                        loader => {
                            loader.OnEntityAdd(arg.Entity);
                            return false;
                        }
                    );
                };
                Project.EntityRemoved += (_, arg) => {
                    ModsManager.HookAction(
                        "OnEntityRemove",
                        loader => {
                            loader.OnEntityRemove(arg.Entity);
                            return false;
                        }
                    );
                };
                Entity.EntityComponentsInitialized += (entity, componentList) => {
                    ModsManager.HookAction(
                        "EntityComponentsInitialized",
                        loader => {
                            loader.EntityComponentsInitialized(entity, componentList);
                            return false;
                        }
                    );
                };
                Project.BeforeSubsystemsAndEntitiesLoad += project => {
                    ModsManager.HookAction(
                        "ProjectBeforeSubsystemsAndEntitiesLoad",
                        loader => {
                            loader.ProjectBeforeSubsystemsAndEntitiesLoad(project);
                            return false;
                        }
                    );
                };
                ProjectData projectData = new(DatabaseManager.GameDatabase, projectNode, valuesDictionary, true);
                m_project = new Project(DatabaseManager.GameDatabase, projectData);
                m_subsystemUpdate = m_project.FindSubsystem<SubsystemUpdate>(true);
            }
            m_worldInfo = worldInfo;
            Log.Information(
                LanguageControl.Get(fName, "1"),
                LanguageControl.Get("GameMode", worldInfo.WorldSettings.GameMode.ToString()),
                LanguageControl.Get("StartingPositionMode", worldInfo.WorldSettings.StartingPositionMode.ToString()),
                worldInfo.WorldSettings.Name,
                SettingsManager.VisibilityRange.ToString(),
                LanguageControl.Get("ResolutionMode", SettingsManager.ResolutionMode.ToString())
            );
            GC.Collect();
        }

        public static void SaveProject(bool waitForCompletion, bool showErrorDialog) {
            if (m_project != null) {
                double realTime = Time.RealTime;
                ProjectData projectData = m_project.Save();
                m_saveCompleted.WaitOne();
                m_saveCompleted.Reset();
                SubsystemGameInfo subsystemGameInfo = m_project.FindSubsystem<SubsystemGameInfo>(true);
                Task.Run(() => InternalSaveProject(projectData, subsystemGameInfo.DirectoryName, showErrorDialog));
                if (waitForCompletion) {
                    m_saveCompleted.WaitOne();
                }
                Log.Verbose(string.Format(LanguageControl.Get(fName, "4"), Math.Round((Time.RealTime - realTime) * 1000.0)));
            }
        }

        public static void InternalSaveProject(ProjectData projectData, string directoryName, bool showErrorDialog) {
            try {
                string projectFileName = Storage.CombinePaths(directoryName, "Project.xml");
                WorldsManager.MakeQuickWorldBackup(directoryName);
                XElement xElement = new("Project");
                ModsManager.HookAction(
                    "ProjectXmlSave",
                    loader => {
                        loader.ProjectXmlSave(xElement);
                        return false;
                    }
                );
                projectData.Save(xElement);
                XmlUtils.SetAttributeValue(xElement, "Version", VersionsManager.SerializationVersion);
                XmlUtils.SetAttributeValue(xElement, "APIVersion", ModsManager.APIVersionString);
                Storage.CreateDirectory(directoryName);
                ModsManager.HookAction(
                    "OnProjectXmlSaved",
                    loader => {
                        loader.OnProjectXmlSaved(xElement);
                        return false;
                    }
                );
                using (Stream stream = Storage.OpenFile(projectFileName, OpenFileMode.Create)) {
                    XmlUtils.SaveXmlToStream(xElement, stream, null, true);
                }
            }
            catch (Exception ex) {
                if (showErrorDialog) {
                    Dispatcher.Dispatch(
                        delegate {
                            DialogsManager.ShowDialog(
                                null,
                                new MessageDialog(
                                    LanguageControl.Get(fName, "2"),
                                    $"{ex.Message}\n{LanguageControl.Get(fName, "3")}",
                                    LanguageControl.Ok,
                                    null,
                                    null
                                )
                            );
                        }
                    );
                }
                Log.Error($"{LanguageControl.Get(fName, "2")}\n{ex}");
            }
            finally {
                m_saveCompleted.Set();
            }
        }

        public static void UpdateProject() {
            if (SyncDispatcher.Count > 0
                && SyncDispatcher[0].Invoke()) {
                SyncDispatcher.RemoveAt(0);
            }
            if (m_project != null) {
                m_subsystemUpdate.Update();
            }
        }

        public static void DisposeProject() {
            if (m_project != null) {
                ProjectDisposed?.Invoke(m_project);
                m_project.Dispose();
                m_project = null;
                m_subsystemUpdate = null;
                m_worldInfo = null;
                ModsManager.HookAction(
                    "OnProjectDisposed",
                    loader => {
                        loader.OnProjectDisposed();
                        return false;
                    }
                );
                GC.Collect();
            }
        }
    }
}