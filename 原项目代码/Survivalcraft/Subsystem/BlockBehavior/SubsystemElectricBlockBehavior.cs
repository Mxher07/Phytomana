using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemElectricBlockBehavior : SubsystemBlockBehavior {
        public SubsystemElectricity m_subsystemElectricity;

        public override int[] HandledBlocks => [
            133,
            140,
            137,
            143,
            156,
            134,
            135,
            145,
            224,
            146,
            157,
            180,
            181,
            183,
            138,
            139,
            141,
            142,
            184,
            187,
            186,
            188,
            144,
            151,
            179,
            152,
            254,
            253,
            182,
            185,
            56,
            57,
            58,
            83,
            84,
            166,
            194,
            86,
            63,
            97,
            98,
            210,
            211,
            105,
            106,
            107,
            234,
            235,
            236,
            147,
            153,
            154,
            223,
            155,
            243,
            120,
            121,
            199,
            216,
            227,
            237
        ];

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            m_subsystemElectricity.OnElectricElementBlockGenerated(x, y, z);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            m_subsystemElectricity.OnElectricElementBlockAdded(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            m_subsystemElectricity.OnElectricElementBlockRemoved(x, y, z);
        }

        public override void OnBlockModified(int value, int oldValue, int x, int y, int z) {
            m_subsystemElectricity.OnElectricElementBlockModified(x, y, z);
        }

        public override void OnBlockStartMoving(int value, int newValue, int x, int y, int z, MovingBlock movingBlock) {
            m_subsystemElectricity.m_pointsToUpdate[new Point3(x, y, z)] = true;
        }

        public override void OnBlockStopMoving(int value, int oldValue, int x, int y, int z, MovingBlock movingBlock) {
            m_subsystemElectricity.m_pointsToUpdate[new Point3(x, y, z)] = true;
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            m_subsystemElectricity.OnChunkDiscarding(chunk);
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            for (int i = 0; i < 6; i++) {
                m_subsystemElectricity.GetElectricElement(x, y, z, i)
                    ?.OnNeighborBlockChanged(new CellFace(x, y, z, i), neighborX, neighborY, neighborZ);
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            int x = raycastResult.CellFace.X;
            int y = raycastResult.CellFace.Y;
            int z = raycastResult.CellFace.Z;
            for (int i = 0; i < 6; i++) {
                ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(x, y, z, i);
                if (electricElement != null) {
                    return electricElement.OnInteract(raycastResult, componentMiner);
                }
            }
            return false;
        }

        public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) {
            int x = cellFace.X;
            int y = cellFace.Y;
            int z = cellFace.Z;
            int num = 0;
            ElectricElement electricElement;
            while (true) {
                if (num < 6) {
                    electricElement = m_subsystemElectricity.GetElectricElement(x, y, z, num);
                    if (electricElement != null) {
                        break;
                    }
                    num++;
                    continue;
                }
                return;
            }
            electricElement.OnCollide(cellFace, velocity, componentBody);
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            int x = cellFace.X;
            int y = cellFace.Y;
            int z = cellFace.Z;
            int num = 0;
            ElectricElement electricElement;
            while (true) {
                if (num < 6) {
                    electricElement = m_subsystemElectricity.GetElectricElement(x, y, z, num);
                    if (electricElement != null) {
                        break;
                    }
                    num++;
                    continue;
                }
                return;
            }
            electricElement.OnHitByProjectile(cellFace, worldItem);
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(true);
        }
    }
}