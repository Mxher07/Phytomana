using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 通用花朵行为接线：统一接管所有实现 IPhytoFlowerBlock 的方块，
    /// 放置/破坏/区块装卸时创建、钩子派发、注册与注销花朵节点。
    /// 新增花朵无需再写行为子系统。
    /// </summary>
    public class SubsystemFlowerBehavior : SubsystemBlockBehavior {
        public FlowerTickScheduler m_scheduler;

        public int[] m_handledBlocks;

        public override int[] HandledBlocks {
            get {
                if (m_handledBlocks == null) {
                    List<int> list = [];
                    Block[] blocks = BlocksManager.Blocks;
                    for (int i = 0; i < blocks.Length; i++) {
                        if (blocks[i] is IPhytoFlowerBlock) {
                            list.Add(i);
                        }
                    }
                    m_handledBlocks = list.ToArray();
                }
                return m_handledBlocks;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_scheduler = Project.FindSubsystem<FlowerTickScheduler>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            TilePhytoFlower flower = EnsureFlower(Terrain.ExtractContents(value), x, y, z);
            flower?.OnPlaced();
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            TilePhytoFlower flower = EnsureFlower(Terrain.ExtractContents(value), x, y, z);
            flower?.OnChunkLoad();
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (!m_scheduler.TryGetFlower(point, out TilePhytoFlower flower)) {
                return;
            }
            flower.OnDestroyed();
            m_scheduler.UnregisterFlower(flower, true);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<TilePhytoFlower> list = [];
            foreach (TilePhytoFlower flower in m_scheduler.m_flowers) {
                Point3 point = flower.Position;
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(flower);
                }
            }
            foreach (TilePhytoFlower flower in list) {
                flower.OnChunkUnload();
                m_scheduler.UnregisterFlower(flower, false);
            }
        }

        public TilePhytoFlower EnsureFlower(int contents, int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (m_scheduler.TryGetFlower(point, out TilePhytoFlower existing)) {
                return existing;
            }
            if (BlocksManager.Blocks[contents] is not IPhytoFlowerBlock flowerBlock) {
                return null;
            }
            TilePhytoFlower flower = flowerBlock.CreateFlower(point);
            if (flower == null) {
                return null;
            }
            m_scheduler.RegisterFlower(flower);
            return flower;
        }
    }
}
