using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemEggBlockBehavior : SubsystemBlockBehavior {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemCreatureSpawn m_subsystemCreatureSpawn;

        public EggBlock m_eggBlock = (EggBlock)BlocksManager.Blocks[118];

        public Random m_random = new();

        public const string fName = "SubsystemEggBlockBehavior";

        public override int[] HandledBlocks => [];

        public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem) {
            int data = Terrain.ExtractData(worldItem.Value);
            bool isCooked = EggBlock.GetIsCooked(data);
            bool isLaid = EggBlock.GetIsLaid(data);
            if (!isCooked
                && (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || m_random.Float(0f, 1f) <= (isLaid ? 0.15f : 1f))) {
                try {
                    EggBlock.EggType eggType = m_eggBlock.GetEggType(data);
                    Entity entity = DatabaseManager.CreateEntity(Project, eggType.TemplateName, true);
                    entity.FindComponent<ComponentBody>(true).Position = worldItem.Position;
                    entity.FindComponent<ComponentBody>(true).Rotation = Quaternion.CreateFromAxisAngle(
                        Vector3.UnitY,
                        m_random.Float(0f, (float)Math.PI * 2f)
                    );
                    entity.FindComponent<ComponentSpawn>(true).SpawnDuration = 0.25f;
                    Project.AddEntity(entity);
                }
                catch (Exception e) {
                    Log.Error($"Spawning creature from egg (index: {(data >> 4) & 0xFFF}) error: {e}");
                    if (worldItem is Projectile projectile) {
                        ComponentGui componentGui = projectile.Owner?.Entity.FindComponent<ComponentGui>();
                        if (componentGui != null) {
                            componentGui.DisplaySmallMessage(LanguageControl.Get(fName, "1"), Color.White, true, false);
                        }
                    }
                }
            }
            return true;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemCreatureSpawn = Project.FindSubsystem<SubsystemCreatureSpawn>(true);
        }
    }
}