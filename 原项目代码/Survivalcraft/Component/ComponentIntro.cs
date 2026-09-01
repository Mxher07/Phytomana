using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentIntro : Component, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemGameInfo m_subsystemGameInfo;

        public ComponentPlayer m_componentPlayer;

        public bool m_playIntro;

        public static string fName = "ComponentIntro";

        public StateMachine m_stateMachine = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public static Vector2 FindOceanDirection(ITerrainContentsGenerator generator, Vector2 position) {
            float num = float.MaxValue;
            Vector2 result = Vector2.Zero;
            for (int i = 0; i < 36; i++) {
                Vector2 vector = Vector2.CreateFromAngle(i / 36f * 2f * (float)Math.PI);
                Vector2 vector2 = position + 50f * vector;
                float num2 = generator.CalculateOceanShoreDistance(vector2.X, vector2.Y);
                if (num2 < num) {
                    result = vector;
                    num = num2;
                }
            }
            return result;
        }

        public virtual void Update(float dt) {
            if (m_playIntro) {
                m_playIntro = false;
                m_stateMachine.TransitionTo("ShipView");
            }
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_playIntro = valuesDictionary.GetValue<bool>("PlayIntro") && SettingsManager.AllowInitialIntro;
            m_stateMachine.AddState("ShipView", ShipView_Enter, ShipView_Update, null);
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("PlayIntro", m_playIntro);
        }

        public virtual void ShipView_Enter() {
            ComponentBody componentBody = m_componentPlayer.Entity.FindComponent<ComponentBody>(true);
            Vector2 vector = FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, componentBody.Position.XZ);
            Vector2 vector2 = componentBody.Position.XZ + 25f * vector;
            bool isPlayerMounted = m_componentPlayer.ComponentRider.Mount != null;
            Vector2 vector3 = vector2;
            float num = float.MinValue;
            for (int i = Terrain.ToCell(vector2.Y) - 15; i < Terrain.ToCell(vector2.Y) + 15; i++) {
                for (int j = Terrain.ToCell(vector2.X) - 15; j < Terrain.ToCell(vector2.X) + 15; j++) {
                    float num2 = ScoreShipPosition(componentBody.Position.XZ, j, i);
                    if (num2 > num) {
                        num = num2;
                        vector3 = new Vector2(j, i);
                    }
                }
            }
            DatabaseObject databaseObject = Project.GameDatabase.Database.FindDatabaseObject(
                "IntroShip",
                Project.GameDatabase.EntityTemplateType,
                true
            );
            ValuesDictionary valuesDictionary = new();
            valuesDictionary.PopulateFromDatabaseObject(databaseObject);
            Entity entity = Project.CreateEntity(valuesDictionary);
            Vector3 vector4 = new(vector3.X, m_subsystemTerrain.TerrainContentsGenerator.OceanLevel + 0.5f, vector3.Y);
            entity.FindComponent<ComponentFrame>(true).Position = vector4;
            entity.FindComponent<ComponentIntroShip>(true).Heading = Vector2.Angle(vector, -Vector2.UnitY);
            Project.AddEntity(entity);
            m_subsystemTime.QueueGameTimeDelayedExecution(
                2.0,
                delegate { m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get(fName, 1), 5f, 0f); }
            );
            m_subsystemTime.QueueGameTimeDelayedExecution(
                7.0,
                delegate {
                    if (isPlayerMounted) {
                        m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get(fName, 2), 5f, 0f);
                    }
                    else {
                        m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get(fName, 3), 5f, 0f);
                    }
                }
            );
            m_subsystemTime.QueueGameTimeDelayedExecution(
                12.0,
                delegate { m_componentPlayer.ComponentGui.DisplayLargeMessage(null, LanguageControl.Get(fName, 4), 5f, 0f); }
            );
            IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
            m_componentPlayer.GameWidget.ActiveCamera = introCamera;
            introCamera.CameraPosition = vector4 + new Vector3(12f * vector.X, 8f, 12f * vector.Y) + new Vector3(-5f * vector.Y, 0f, 5f * vector.X);
            introCamera.TargetPosition = m_componentPlayer.ComponentCreatureModel.EyePosition + 2.5f * new Vector3(vector.X, 0f, vector.Y);
            introCamera.Speed = 0f;
            introCamera.TargetCameraPosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
        }

        public virtual void ShipView_Update() {
            IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
            introCamera.Speed = MathUtils.Lerp(0f, 8f, MathUtils.Saturate(((float)m_subsystemGameInfo.TotalElapsedGameTime - 6f) / 3f));
            if (Vector3.Distance(introCamera.TargetCameraPosition, introCamera.CameraPosition) < 0.3f) {
                m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
                m_stateMachine.TransitionTo(null);
            }
        }

        public virtual float ScoreShipPosition(Vector2 playerPosition, int x, int z) {
            float num = 0f;
            float num2 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(x, z);
            if (num2 > -8f) {
                num -= 100f;
            }
            num -= 0.25f * num2;
            float num3 = Vector2.Distance(playerPosition, new Vector2(x, z));
            num -= MathF.Abs(num3 - 20f);
            int num4 = 0;
            TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
            if (chunkAtCell != null
                && chunkAtCell.State >= TerrainChunkState.InvalidLight) {
                int oceanLevel = m_subsystemTerrain.TerrainContentsGenerator.OceanLevel;
                int num5 = oceanLevel;
                while (num5 >= oceanLevel - 5
                    && num5 >= 0) {
                    int cellContentsFast = chunkAtCell.GetCellContentsFast(x & 0xF, num5, z & 0xF);
                    if (cellContentsFast != 18
                        && cellContentsFast != 92) {
                        break;
                    }
                    num5--;
                    num4++;
                }
            }
            else {
                num4 = 2;
            }
            if (num4 < 2) {
                num -= 100f;
            }
            return num + 2f * num4;
        }
    }
}