using System;
using Engine;
using Game;

namespace Phytomana {
    /// <summary>
    /// 须弥花群世界生成器：在全新生成的区块上种出「花群」，形成一片片同色花海景观。
    /// 只在钩子 OnTerrainContentsGenerated（地形内容生成完毕后）调用，
    /// 存档加载的区块不走生成状态机，因此不会重复生成、玩家挖掉的花也不会复活。
    ///
    /// 生成规则：
    /// 1. 大花群：以 2×2 区块（32×32 格）为一个区域，每区域随机 0~2 群，
    ///    每群 4~6 朵，整群同色且同一区域的群共享颜色（形成成片的色彩分区）；
    /// 2. 小散群：每个区块有小概率（默认 12%）生成一群 2~5 朵的小散群，
    ///    位置/数量/颜色全随机；
    /// 3. 只把花种在「草方块表面 + 上方为空气」的格子，绝不替换原版草、花、树等植被；
    /// 4. 全部随机数由（世界种子 + 坐标 + 盐值）确定性推导，同一区块重新生成的结果完全一致。
    /// </summary>
    public static class SumeruPatchGenerator {
        /// <summary>大花群的区域尺寸：2×2 区块（32×32 格）。</summary>
        public const int RegionCells = 32;

        /// <summary>每区域大花群数量分布：roll &lt; 30 → 0 群，&lt; 75 → 1 群，否则 2 群。</summary>
        public const int BigPatchZeroRoll = 30;
        public const int BigPatchOneRoll = 75;

        /// <summary>每个区块生成小散群的概率（百分比）。</summary>
        public const int SmallGroupChancePercent = 12;

        /// <summary>花群中心距区域/区块边缘的余量，保证花丛完整落在所属范围内不跨区。</summary>
        public const int PatchMargin = 3;
        public const int SmallGroupMargin = 2;

        /// <summary>表面扫描的最高起始高度。</summary>
        public const int SurfaceScanTop = 248;

        public const int FlowerSaltRegionColor = 0x5F10;
        public const int FlowerSaltRegionPatches = 0x5F11;
        public const int FlowerSaltSmallGroup = 0x5F12;

        public static int m_sumeruFlowerIndex = -1;
        public static int m_grassIndex = -1;
        public static int m_worldSeed;

        /// <summary>由 PhytomanaMod 在 BlocksInitalized（方块索引就绪）时调用。</summary>
        public static void Initialize() {
            m_sumeruFlowerIndex = BlocksManager.GetBlockIndex<SumeruFlowerBlock>();
            m_grassIndex = BlocksManager.GetBlockIndex<GrassBlock>();
        }

        /// <summary>由 PhytomanaMod 在 OnProjectLoaded 时调用（主线程读一次世界种子，地形线程只读静态值）。</summary>
        public static void SetWorldSeed(int worldSeed) {
            m_worldSeed = worldSeed;
        }

        /// <summary>地形内容生成完毕的区块回调：种下该区块范围内应出现的全部花朵。</summary>
        public static void OnChunkGenerated(TerrainChunk chunk) {
            if (m_sumeruFlowerIndex < 0 || m_grassIndex < 0) {
                return;
            }
            Terrain terrain = chunk.Terrain;
            if (terrain == null) {
                return;
            }
            Point2 origin = chunk.Origin;
            PlaceRegionPatches(terrain, chunk, origin);
            PlaceSmallGroup(terrain, chunk, origin);
        }

        /// <summary>大花群：计算所属区域（2×2 区块）的确定性花群列表，只种落在本区块内的部分。</summary>
        public static void PlaceRegionPatches(Terrain terrain, TerrainChunk chunk, Point2 origin) {
            int regionX = origin.X >> 5;
            int regionZ = origin.Y >> 5;
            // 同一区域的所有区块共享颜色，形成「一片一片」的同色花海分区。
            int regionColor = MixHash(m_worldSeed, FlowerSaltRegionColor, regionX, regionZ) % 16;
            System.Random patchRng = new(MixHash(m_worldSeed, FlowerSaltRegionPatches, regionX, regionZ));
            int roll = patchRng.Next(100);
            int patchCount = roll < BigPatchZeroRoll ? 0 : (roll < BigPatchOneRoll ? 1 : 2);
            for (int i = 0; i < patchCount; i++) {
                // 花群中心留在区域内部（余量 3），花丛半径 ≤2 不会跨出区域边界。
                int centerX = (regionX << 5) + PatchMargin + patchRng.Next(RegionCells - PatchMargin * 2);
                int centerZ = (regionZ << 5) + PatchMargin + patchRng.Next(RegionCells - PatchMargin * 2);
                int flowerCount = 4 + patchRng.Next(3);
                PlaceBlob(terrain, origin, centerX, centerZ, regionColor, flowerCount, patchRng, radius: 2);
            }
        }

        /// <summary>小散群：每个区块按概率生成一群 2~5 朵、颜色/位置全随机的小花丛。</summary>
        public static void PlaceSmallGroup(Terrain terrain, TerrainChunk chunk, Point2 origin) {
            System.Random rng = new(MixHash(m_worldSeed, FlowerSaltSmallGroup, origin.X, origin.Y));
            if (rng.Next(100) >= SmallGroupChancePercent) {
                return;
            }
            int centerX = origin.X + SmallGroupMargin + rng.Next(16 - SmallGroupMargin * 2);
            int centerZ = origin.Y + SmallGroupMargin + rng.Next(16 - SmallGroupMargin * 2);
            int color = rng.Next(16);
            int flowerCount = 2 + rng.Next(4);
            PlaceBlob(terrain, origin, centerX, centerZ, color, flowerCount, rng, radius: 1);
        }

        /// <summary>
        /// 在以 (centerX, centerZ) 为中心的花丛中种花：候选格按随机顺序尝试，
        /// 只种在「草方块表面 + 上方空气」的位置，直到种满目标数量或候选耗尽。
        /// </summary>
        public static void PlaceBlob(
            Terrain terrain,
            Point2 origin,
            int centerX,
            int centerZ,
            int color,
            int flowerCount,
            System.Random rng,
            int radius
        ) {
            // 收集半径内的候选偏移并随机打乱，形成自然的花丛形状。
            Span<int> offsets = stackalloc int[(radius * 2 + 1) * (radius * 2 + 1)];
            int count = 0;
            for (int dx = -radius; dx <= radius; dx++) {
                for (int dz = -radius; dz <= radius; dz++) {
                    if (dx * dx + dz * dz <= radius * radius + 1) {
                        offsets[count++] = (dx << 8) | (dz & 0xFF);
                    }
                }
            }
            for (int i = count - 1; i > 0; i--) {
                int j = rng.Next(i + 1);
                (offsets[i], offsets[j]) = (offsets[j], offsets[i]);
            }
            int value = Terrain.MakeBlockValue(m_sumeruFlowerIndex, 0, color << 1);
            for (int i = 0; i < count && flowerCount > 0; i++) {
                int dx = offsets[i] >> 8;
                int dz = (sbyte)(offsets[i] & 0xFF);
                int x = centerX + dx;
                int z = centerZ + dz;
                // 只处理落在本区块内的格子，跨区块的部分由邻区块按同一份确定性数据自己种。
                if (x < origin.X || x >= origin.X + 16
                    || z < origin.Y || z >= origin.Y + 16) {
                    continue;
                }
                if (TryPlaceFlower(terrain, x, z, value)) {
                    flowerCount--;
                }
            }
        }

        /// <summary>在 (x, z) 列种一朵花：表面须为草方块且其上为空气，绝不替换已有植被。</summary>
        public static bool TryPlaceFlower(Terrain terrain, int x, int z, int value) {
            for (int y = SurfaceScanTop; y > 0; y--) {
                int contents = Terrain.ExtractContents(terrain.GetCellValueFast(x, y, z));
                if (contents == 0) {
                    continue;
                }
                // 第一块非空气：是草方块且上方为空气才可种花，否则该列放弃。
                if (contents != m_grassIndex) {
                    return false;
                }
                if (y + 1 >= 254
                    || Terrain.ExtractContents(terrain.GetCellValueFast(x, y + 1, z)) != 0) {
                    return false;
                }
                terrain.GetChunkAtCell(x, z)?.SetCellValueFast(x & 0xF, y + 1, z & 0xF, value);
                return true;
            }
            return false;
        }

        /// <summary>整数混合哈希（murmur 风格终化），用于从种子+坐标推导确定性随机数。</summary>
        public static int MixHash(int a, int b, int c, int d) {
            uint h = (uint)a;
            h ^= (uint)b * 0x9E3779B1u;
            h ^= (uint)c * 0x85EBCA77u;
            h ^= (uint)d * 0xC2B2AE3Du;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (int)h;
        }
    }
}
