using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Engine;
using Engine.Media;
using Engine.Graphics;
using Engine.Serialization;
using GameEntitySystem;

namespace Game {
    public static class BlocksManager {
        public struct ImageExtrusionKey {
            public Image Image;

            public int Slot;

            public override int GetHashCode() => Image.GetHashCode() ^ Slot.GetHashCode();

            public override bool Equals(object obj) {
                if (obj != null) {
                    ImageExtrusionKey imageExtrusionKey = (ImageExtrusionKey)obj;
                    if (imageExtrusionKey.Image == Image) {
                        return imageExtrusionKey.Slot == Slot;
                    }
                }
                return false;
            }
        }

        public static Block[] m_blocks = new Block[1024];

        public static FluidBlock[] m_fluidBlocks = new FluidBlock[1024];

        public static List<string> m_categories = [];

        public static DrawBlockEnvironmentData m_defaultEnvironmentData = new();

        public static Vector4[] m_slotTexCoords = new Vector4[256];

        public static Dictionary<ImageExtrusionKey, BlockMesh> m_imageExtrusionsCache = [];

        public static Block[] Blocks => m_blocks;

        public static List<BlockAllocateData> BlocksAllocateData = new();
        public static FluidBlock[] FluidBlocks => m_fluidBlocks;

        //将ModBlock和BlockIndex联系起来的表格。
        public static Dictionary<string, int> BlockNameToIndex = new();
        public static Dictionary<Type, int> BlockTypeToIndex = new();

        public static ReadOnlyList<string> Categories => new(m_categories);

        [Obsolete("Use BlockTypeToOriginalIndex.")]
        public static int[] m_originalBlockIndex = new int[1024];

        public static Dictionary<Type, int> BlockTypeToOriginalIndex = new();

        public const int SurvivalCraftBlockCount = 299;

        public static bool DrawImageExtrusionEnabled = true;

        public static bool LoadBlocksStaticly = false;

        public class BlockAllocateDataComparer : IComparer<BlockAllocateData> {
            public static BlockAllocateDataComparer Instance = new();

            public int Compare(BlockAllocateData u1, BlockAllocateData u2) {
                ArgumentNullException.ThrowIfNull(u1);
                ArgumentNullException.ThrowIfNull(u2);
                //首先比对是否已分配，未分配的排前面
                int blockAllocate = (u1.Allocated ? 1 : 0) - (u2.Allocated ? 1 : 0);
                if (blockAllocate != 0) {
                    return blockAllocate;
                }
                //然后比对mod信息：同 mod 的方块聚到一起，保证连续分配 BlockIndex
                //用引用哈希(RuntimeHelpers.GetHashCode)做稳定标识，与 ModEntity 重写的值相等语义解耦；
                //比较而非相减，避免 int 溢出破坏传递性导致排序异常
                int modHash1 = RuntimeHelpers.GetHashCode(u1.ModEntity);
                int modHash2 = RuntimeHelpers.GetHashCode(u2.ModEntity);
                if (modHash1 != modHash2) {
                    return modHash1 < modHash2 ? -1 : 1;
                }
                //mod相同，则比对BlockIndex
                int blockIndexSub = u1.Block.BlockIndex - u2.Block.BlockIndex;
                return blockIndexSub != 0
                    ? blockIndexSub
                    :
                    //方块的Index相同（均未分配），则按方块名顺序分配
                    string.Compare(u1.Block.GetType().Name, u2.Block.GetType().Name, CultureInfo.InvariantCulture, CompareOptions.None);
            }
        }

        public static void AllocateBlock(BlockAllocateData allocateData, int Index) {
            Block block = allocateData.Block;
            m_blocks[Index] = block;
            BlockNameToIndex[block.GetType().Name] = Index;
            BlockTypeToIndex[block.GetType()] = Index;
            if (!BlockTypeToOriginalIndex.ContainsKey(block.GetType())) {
                BlockTypeToOriginalIndex.Add(block.GetType(), allocateData.Block.BlockIndex);
            }

            //修复了加载旧API存档时，会导致方块索引错乱的问题
            allocateData.Block.BlockIndex = Index;
            allocateData.Allocated = true;
            allocateData.Index = Index;
            //修改方块的Index静态字段值
#pragma warning disable IL2072
            FieldInfo fieldInfo = block.GetType().GetRuntimeFields().FirstOrDefault(p => p.Name == "Index" && p.IsPublic && p.IsStatic);
#pragma warning restore IL2072
            if (fieldInfo != null
                && fieldInfo.FieldType == typeof(int)
                && !fieldInfo.IsLiteral) {
                try {
                    fieldInfo.SetValue(null, block.BlockIndex); // 对于静态字段，第一个参数为null
                }
                catch (Exception ex) {
                    Log.Error($"Failed to edit Index of <{block.GetType().AssemblyQualifiedName}>! {ex}");
                }
            }
        }

        public static void Initialize() {
            GameManager.ProjectDisposed += delegate { m_imageExtrusionsCache.Clear(); };
            InitializeCategories();
            CalculateSlotTexCoordTables();
            InitializeBlocks(null);
            PostProcessBlocksLoad();
        }

        public static void ResetBlocks() {
            for (int i = 0; i < m_blocks.Length; i++) {
#pragma warning disable IL2072
                m_blocks[i] = Activator.CreateInstance(m_blocks[i].GetType()) as Block;
#pragma warning restore IL2072
                if (!(m_blocks[i] is AirBlock)) {
                    m_blocks[i].BlockIndex = i;
                }
                if (m_blocks[i] is FluidBlock fluidBlock) {
                    m_fluidBlocks[i] = fluidBlock;
                }
            }
        }

        public static void InitializeCategories() {
            m_categories.Clear();
            m_categories.Add("Terrain");
            m_categories.Add("Minerals");
            m_categories.Add("Plants");
            m_categories.Add("Construction");
            m_categories.Add("Items");
            m_categories.Add("Tools");
            m_categories.Add("Weapons");
            m_categories.Add("Clothes");
            m_categories.Add("Electrics");
            m_categories.Add("Food");
            m_categories.Add("Spawner Eggs");
            m_categories.Add("Painted");
            m_categories.Add("Dyed");
            m_categories.Add("Fireworks");
        }

        /// <summary>
        ///     通过方块名称来获取方块的Index
        /// </summary>
        /// <param name="BlockName">方块名称</param>
        /// <param name="throwIfNotFound">在方块未查找到时是否抛出异常</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static int GetBlockIndex(string BlockName, bool throwIfNotFound = false) {
            bool valueGotten = BlockNameToIndex.TryGetValue(BlockName, out int index);
            if (valueGotten) {
                return index;
            }
            if (throwIfNotFound) {
                throw new KeyNotFoundException($"Block with name <{BlockName}> is not found.");
            }
            return -1;
        }

        /// <summary>
        ///     获取方块的Index
        /// </summary>
        /// <typeparam name="T">方块类型</typeparam>
        /// <param name="throwIfNotFound">在方块没有查找到时是否抛出异常</param>
        /// <param name="mustBeInSameType">方块是否要求必须要和目标类型完全一致</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static int GetBlockIndex<T>(bool throwIfNotFound = false, bool mustBeInSameType = false) where T : Block =>
            GetBlockIndex(typeof(T), throwIfNotFound, mustBeInSameType);

        /// <summary>
        ///     获取方块的Index
        /// </summary>
        /// <param name="blockType">方块类型</param>
        /// <param name="throwIfNotFound">在方块没有查找到时是否抛出异常</param>
        /// <param name="mustBeInSameType">方块是否要求必须要和目标类型完全一致</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static int GetBlockIndex(Type blockType, bool throwIfNotFound = false, bool mustBeInSameType = false) {
            int blockIndexByType = BlockTypeToIndex.GetValueOrDefault(blockType, -1);
            if (blockIndexByType >= 0
                && blockIndexByType < 1024) {
                return blockIndexByType;
            }
            if (mustBeInSameType) {
                if (throwIfNotFound) {
                    throw new KeyNotFoundException($"Block with type <{blockType.AssemblyQualifiedName}> is not found.");
                }
                return -1;
            }
            return GetBlockIndex(blockType.Name, throwIfNotFound);
        }

        /// <summary>
        ///     获取一个方块的通用Block类，具有较好的模组兼容稳定性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="throwIfNotFound"></param>
        /// <returns></returns>
        public static Block GetBlockGeneral<T>(bool throwIfNotFound = false) where T : Block {
            int blockIndex = GetBlockIndex<T>(throwIfNotFound);
            if (blockIndex >= 0
                && blockIndex < 1024) {
                return Blocks[blockIndex];
            }
            return null;
        }

        public static Block GetBlock(string BlockName, bool throwIfNotFound = false) {
            int blockIndex = GetBlockIndex(BlockName, throwIfNotFound);
            if (blockIndex >= 0
                && blockIndex < 1024) {
                return m_blocks[blockIndex];
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="throwIfNotFound">在方块没有查找到时是否抛出异常</param>
        /// <param name="mustBeInSameType">方块是否要求必须要和目标类型完全一致</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">没有找到对应的方块</exception>
        /// <exception cref="InvalidCastException">有名称相同的方块，但类型不相容</exception>
        public static T GetBlock<T>(bool throwIfNotFound = false, bool mustBeInSameType = false) where T : Block {
            int blockIndex = GetBlockIndex<T>(throwIfNotFound, mustBeInSameType);
            if (blockIndex < 0) {
                return null;
            }
            T blockT = Blocks[blockIndex] as T;
            if (blockT == null && throwIfNotFound) {
                throw new InvalidCastException(
                    $"Block <{typeof(T).AssemblyQualifiedName}> is modified into <{Blocks[blockIndex].GetType().AssemblyQualifiedName}> thus not capable for type."
                );
            }
            return blockT; //方块列表中有名为"T"的方块，但无法转化为T也返回null
        }

        /// <param name="blockType">方块类型</param>
        /// <param name="throwIfNotFound">在方块没有查找到时是否抛出异常</param>
        /// <param name="mustBeInSameType">方块是否要求必须要和目标类型完全一致</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">没有找到对应的方块</exception>
        /// <exception cref="InvalidCastException">有名称相同的方块，但类型不相容</exception>
        public static Block GetBlock(Type blockType, bool throwIfNotFound = false, bool mustBeInSameType = false) {
            int blockIndex = GetBlockIndex(blockType, throwIfNotFound, mustBeInSameType);
            if (blockIndex < 0) {
                return null;
            }
            Block block = Blocks[blockIndex];
            if (throwIfNotFound && block.GetType() == blockType) {
                throw new InvalidCastException(
                    $"Block <{blockType.AssemblyQualifiedName}> is modified into <{block.GetType().AssemblyQualifiedName}> thus not capable for type."
                );
            }
            return block;
        }

        public static void InitializeBlocks(SubsystemBlocksManager subsystemBlocksManager) {
            for (int i = 0; i < m_blocks.Length; i++) {
                m_blocks[i] = null;
            }
            BlocksAllocateData.Clear();
            foreach (ModEntity entity in ModsManager.ModList) {
                for (int i = 0; i < entity.BlockTypes.Count; i++) {
                    Type type = entity.BlockTypes[i];
#pragma warning disable IL2072
                    Block block = (Block)Activator.CreateInstance(type);
                    if (block == null) {
                        continue;
                    }
                    FieldInfo fieldInfo = type.GetRuntimeFields().FirstOrDefault(p => p.Name == "Index" && p.IsPublic && p.IsStatic);
#pragma warning restore IL2072
                    if (fieldInfo != null
                        && fieldInfo.FieldType == typeof(int)) {
                        int staticIndex = (int)fieldInfo.GetValue(null)!;
                        block.BlockIndex = staticIndex;
                    }
                    else {
                        block.BlockIndex = -1;
                    }
                    if (block.ShouldBeAddedToProject(subsystemBlocksManager)) {
                        bool staticBlockIndexBefore = false;
                        //对重名方块进行移除。复杂度n^2，如果严重影响性能可以考虑使用字典。
                        BlockAllocateData blockAllocateDataToRemove =
                            BlocksAllocateData.FirstOrDefault(b => b.Block.GetType().Name == block.GetType().Name);
                        if (blockAllocateDataToRemove != null) {
                            BlocksAllocateData.Remove(blockAllocateDataToRemove);
                            staticBlockIndexBefore = blockAllocateDataToRemove.StaticBlockIndex;
                            //Log.Information("Block覆盖：" + blockAllocateDataToRemove.Block.GetType().AssemblyQualifiedName + " => " + block.GetType().AssemblyQualifiedName);
                        }
                        BlocksAllocateData.Add(
                            new BlockAllocateData {
                                Block = block,
                                Index = 0,
                                Allocated = false,
                                StaticBlockIndex = !block.IsIndexDynamic
                                    || ReferenceEquals(entity, ModsManager.SurvivalCraftModEntity)
                                    || staticBlockIndexBefore,
                                ModEntity = entity
                            }
                        );
                    }
                }
            }
            //分配静态ID方块
            if (LoadBlocksStaticly) {
                Log.Information("[BlocksManager]Blocks Loaded Statically");
            }
            for (int i = 0; i < BlocksAllocateData.Count; i++) {
                try {
                    BlockAllocateData allocateData = BlocksAllocateData[i];
                    if (allocateData.Block.BlockIndex >= 0) {
                        BlockTypeToOriginalIndex.TryGetValue(allocateData.Block.GetType(), out int originalIndex);
                        if (originalIndex == 0) {
                            originalIndex = allocateData.Block.BlockIndex;
                        }
                        if (allocateData.StaticBlockIndex) {
                            AllocateBlock(allocateData, originalIndex);
                        }
                        else if (LoadBlocksStaticly && originalIndex >= 0) //负ID方块不进行分配
                        {
                            AllocateBlock(allocateData, originalIndex);
                            if (subsystemBlocksManager != null) {
                                subsystemBlocksManager.DynamicBlockNameToIndex[m_blocks[allocateData.Block.BlockIndex].GetType().Name] =
                                    originalIndex;
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    Log.Error(ex);
                }
            }
            //进行排序
            BlocksAllocateData.Sort(BlockAllocateDataComparer.Instance);
            //调用SubsystemBlocksManager，加载Project对于<动态Mod方块-方块ID>的匹配信息。
            if (subsystemBlocksManager != null) {
                subsystemBlocksManager.CallAllocate();
                //分配在SubsystemBlocksManager声明的动态方块
                for (int i = 0; i < BlocksAllocateData.Count; i++) {
                    BlockAllocateData allocateData = BlocksAllocateData[i];
                    if (!allocateData.Allocated) {
                        bool containsKey = subsystemBlocksManager.DynamicBlockNameToIndex.TryGetValue(
                            allocateData.Block.GetType().Name,
                            out int blockValue
                        );
                        if (containsKey) {
                            AllocateBlock(allocateData, blockValue);
                        }
                    }
                }
            }
            //分配剩余动态ID方块
            int allocateDataIndex = 0;
            for (int num = SurvivalCraftBlockCount + 1; allocateDataIndex < BlocksAllocateData.Count; num++) {
                if (num == 1024) {
                    throw new Exception("Too many blocks! Please reduce the mods count.");
                }
                if (m_blocks[num] == null) {
                    try {
                        while (allocateDataIndex < BlocksAllocateData.Count
                            && BlocksAllocateData[allocateDataIndex].Allocated) {
                            allocateDataIndex++;
                        }
                        if (allocateDataIndex == BlocksAllocateData.Count) {
                            break;
                        }
                        AllocateBlock(BlocksAllocateData[allocateDataIndex], num);
                        if (subsystemBlocksManager != null) {
                            subsystemBlocksManager.DynamicBlockNameToIndex[m_blocks[num].GetType().Name] = num;
                        }
                    }
                    catch {
                        // ignored
                    }
                }
            }
            //对未分配方块进行空置操作
            for (int num = 0; num < m_blocks.Length; num++) {
                m_blocks[num] ??= Blocks[0];
            }
        }

        public static void PostProcessBlocksLoad() {
            ResetBlocks();
            foreach (ModEntity modEntity in ModsManager.ModList) {
                modEntity.LoadBlocksData();
            }
            foreach (Block block in m_blocks) {
                try {
                    block.Initialize();
                }
                catch (Exception e) {
                    LoadingScreen.Warning($"Loading Block {block.GetType().Name} error.\n{e}");
                }
                foreach (int value in block.GetCreativeValues()) {
                    string category = block.GetCategory(value);
                    AddCategory(category);
                }
            }
            ModsManager.HookAction(
                "BlocksInitalized",
                modLoader => {
                    modLoader.BlocksInitalized();
                    return false;
                }
            );
        }

        public static void AddCategory(string category) {
            if (!m_categories.Contains(category)) {
                m_categories.Add(category);
            }
        }

        [Obsolete("Use BlocksManager.GetBlock() instead.")]
        public static Block FindBlockByTypeName(string typeName, bool throwIfNotFound) {
            Block block = Blocks.FirstOrDefault(b => b.GetType().Name == typeName);
            if (block == null && throwIfNotFound) {
                throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", 1), typeName));
            }
            return block;
        }

        public static Block[] FindBlocksByCraftingId(string craftingId) {
            List<Block> blocks = [];
            foreach (Block c in Blocks) {
                if (c.MatchCrafingId(craftingId)) {
                    blocks.Add(c);
                }
            }
            return blocks.ToArray();
        }

        public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Vector3 size,
            ref Matrix matrix,
            Color color,
            Color topColor,
            DrawBlockEnvironmentData environmentData) {
            DrawCubeBlock(
                primitivesRenderer,
                value,
                size,
                1f,
                ref matrix,
                color,
                topColor,
                environmentData,
                environmentData.SubsystemTerrain != null
                    ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture
                    : BlocksTexturesManager.DefaultBlocksTexture
            );
        }

        public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Vector3 size,
            float height,
            ref Matrix matrix,
            Color color,
            Color topColor,
            DrawBlockEnvironmentData environmentData) {
            DrawCubeBlock(
                primitivesRenderer,
                value,
                size,
                height,
                ref matrix,
                color,
                topColor,
                environmentData,
                environmentData.SubsystemTerrain != null
                    ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture
                    : BlocksTexturesManager.DefaultBlocksTexture
            );
        }

        public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Vector3 size,
            float height,
            ref Matrix matrix,
            Color color,
            Color topColor,
            DrawBlockEnvironmentData environmentData,
            Texture2D texture) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(
                texture,
                true,
                0,
                null,
                RasterizerState.CullCounterClockwiseScissor,
                null,
                SamplerState.PointClamp
            );
            float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
            color = Color.MultiplyColorOnlyNotSaturated(color, s);
            topColor = Color.MultiplyColorOnlyNotSaturated(topColor, s);
            Vector3 translation = matrix.Translation;
            Vector3 vector = matrix.Right * size.X;
            Vector3 v = matrix.Up * size.Y * height;
            Vector3 v2 = matrix.Forward * size.Z;
            Vector3 v3 = translation + 0.5f * (-vector - v - v2);
            Vector3 v4 = translation + 0.5f * (vector - v - v2);
            Vector3 v5 = translation + 0.5f * (-vector + v - v2);
            Vector3 v6 = translation + 0.5f * (vector + v - v2);
            Vector3 v7 = translation + 0.5f * (-vector - v + v2);
            Vector3 v8 = translation + 0.5f * (vector - v + v2);
            Vector3 v9 = translation + 0.5f * (-vector + v + v2);
            Vector3 v10 = translation + 0.5f * (vector + v + v2);
            if (environmentData.ViewProjectionMatrix.HasValue) {
                Matrix m = environmentData.ViewProjectionMatrix.Value;
                Vector3.Transform(ref v3, ref m, out v3);
                Vector3.Transform(ref v4, ref m, out v4);
                Vector3.Transform(ref v5, ref m, out v5);
                Vector3.Transform(ref v6, ref m, out v6);
                Vector3.Transform(ref v7, ref m, out v7);
                Vector3.Transform(ref v8, ref m, out v8);
                Vector3.Transform(ref v9, ref m, out v9);
                Vector3.Transform(ref v10, ref m, out v10);
            }
            int num = Terrain.ExtractContents(value);
            Block block = Blocks[num];
            Vector4 vector2 = Vector4.Zero;
            int textureSlotCount = block.GetTextureSlotCount(value);
            int textureSlot = block.GetFaceTextureSlot(0, value);
            vector2.X = (float)(textureSlot % textureSlotCount) / textureSlotCount;
            vector2.Y = (float)(textureSlot / textureSlotCount) / textureSlotCount;
            vector2.W = vector2.Y + 1f / textureSlotCount;
            vector2.Z = vector2.X + 1f / textureSlotCount;
            vector2.W = MathUtils.Lerp(vector2.Y, vector2.W, height);
            texturedBatch3D.QueueQuad(
                color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(-matrix.Forward)),
                p1: v3,
                p2: v5,
                p3: v6,
                p4: v4,
                texCoord1: new Vector2(vector2.X, vector2.W),
                texCoord2: new Vector2(vector2.X, vector2.Y),
                texCoord3: new Vector2(vector2.Z, vector2.Y),
                texCoord4: new Vector2(vector2.Z, vector2.W)
            );
            textureSlot = block.GetFaceTextureSlot(2, value);
            vector2.X = (float)(textureSlot % textureSlotCount) / textureSlotCount;
            vector2.Y = (float)(textureSlot / textureSlotCount) / textureSlotCount;
            vector2.W = vector2.Y + 1f / textureSlotCount;
            vector2.Z = vector2.X + 1f / textureSlotCount;
            vector2.W = MathUtils.Lerp(vector2.Y, vector2.W, height);
            texturedBatch3D.QueueQuad(
                color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(matrix.Forward)),
                p1: v7,
                p2: v8,
                p3: v10,
                p4: v9,
                texCoord1: new Vector2(vector2.Z, vector2.W),
                texCoord2: new Vector2(vector2.X, vector2.W),
                texCoord3: new Vector2(vector2.X, vector2.Y),
                texCoord4: new Vector2(vector2.Z, vector2.Y)
            );
            textureSlot = block.GetFaceTextureSlot(5, value);
            vector2.X = (float)(textureSlot % textureSlotCount) / textureSlotCount;
            vector2.Y = (float)(textureSlot / textureSlotCount) / textureSlotCount;
            vector2.W = vector2.Y + 1f / textureSlotCount;
            vector2.Z = vector2.X + 1f / textureSlotCount;
            texturedBatch3D.QueueQuad(
                color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(-matrix.Up)),
                p1: v3,
                p2: v4,
                p3: v8,
                p4: v7,
                texCoord1: new Vector2(vector2.X, vector2.Y),
                texCoord2: new Vector2(vector2.Z, vector2.Y),
                texCoord3: new Vector2(vector2.Z, vector2.W),
                texCoord4: new Vector2(vector2.X, vector2.W)
            );
            textureSlot = block.GetFaceTextureSlot(4, value);
            vector2.X = (float)(textureSlot % textureSlotCount) / textureSlotCount;
            vector2.Y = (float)(textureSlot / textureSlotCount) / textureSlotCount;
            vector2.W = vector2.Y + 1f / textureSlotCount;
            vector2.Z = vector2.X + 1f / textureSlotCount;
            texturedBatch3D.QueueQuad(
                color: Color.MultiplyColorOnlyNotSaturated(topColor, LightingManager.CalculateLighting(matrix.Up)),
                p1: v5,
                p2: v9,
                p3: v10,
                p4: v6,
                texCoord1: new Vector2(vector2.X, vector2.W),
                texCoord2: new Vector2(vector2.X, vector2.Y),
                texCoord3: new Vector2(vector2.Z, vector2.Y),
                texCoord4: new Vector2(vector2.Z, vector2.W)
            );
            textureSlot = block.GetFaceTextureSlot(1, value);
            vector2.X = (float)(textureSlot % textureSlotCount) / textureSlotCount;
            vector2.Y = (float)(textureSlot / textureSlotCount) / textureSlotCount;
            vector2.W = vector2.Y + 1f / textureSlotCount;
            vector2.Z = vector2.X + 1f / textureSlotCount;
            vector2.W = MathUtils.Lerp(vector2.Y, vector2.W, height);
            texturedBatch3D.QueueQuad(
                color: Color.MultiplyColorOnlyNotSaturated(color, LightingManager.CalculateLighting(-matrix.Right)),
                p1: v3,
                p2: v7,
                p3: v9,
                p4: v5,
                texCoord1: new Vector2(vector2.Z, vector2.W),
                texCoord2: new Vector2(vector2.X, vector2.W),
                texCoord3: new Vector2(vector2.X, vector2.Y),
                texCoord4: new Vector2(vector2.Z, vector2.Y)
            );
            textureSlot = block.GetFaceTextureSlot(3, value);
            vector2.X = (float)(textureSlot % textureSlotCount) / textureSlotCount;
            vector2.Y = (float)(textureSlot / textureSlotCount) / textureSlotCount;
            vector2.W = vector2.Y + 1f / textureSlotCount;
            vector2.Z = vector2.X + 1f / textureSlotCount;
            vector2.W = MathUtils.Lerp(vector2.Y, vector2.W, height);
            texturedBatch3D.QueueQuad(
                color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(matrix.Right)),
                p1: v4,
                p2: v6,
                p3: v10,
                p4: v8,
                texCoord1: new Vector2(vector2.X, vector2.W),
                texCoord2: new Vector2(vector2.X, vector2.Y),
                texCoord3: new Vector2(vector2.Z, vector2.Y),
                texCoord4: new Vector2(vector2.Z, vector2.W)
            );
        }

        public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Vector3 size,
            ref Matrix matrix,
            Color color,
            Color topColor,
            DrawBlockEnvironmentData environmentData,
            Texture2D texture) {
            DrawCubeBlock(
                primitivesRenderer,
                value,
                size,
                1f,
                ref matrix,
                color,
                topColor,
                environmentData,
                texture
            );
        }

        public static void DrawFlatOrImageExtrusionBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            float size,
            ref Matrix matrix,
            Texture2D texture,
            Color color,
            bool isEmissive,
            DrawBlockEnvironmentData environmentData) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            if (DrawImageExtrusionEnabled
                && texture == null
                && !isEmissive
                && (environmentData.DrawBlockMode == DrawBlockMode.FirstPerson || environmentData.DrawBlockMode == DrawBlockMode.ThirdPerson)) {
                DrawImageExtrusionBlock(primitivesRenderer, value, size, ref matrix, color, environmentData);
            }
            else {
                DrawFlatBlock(
                    primitivesRenderer,
                    value,
                    size,
                    ref matrix,
                    texture,
                    color,
                    isEmissive,
                    environmentData
                );
            }
        }

        public static void DrawFlatBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            float size,
            ref Matrix matrix,
            Texture2D texture,
            Color color,
            bool isEmissive,
            DrawBlockEnvironmentData environmentData) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            int num = Terrain.ExtractContents(value);
            Block block = Blocks[num];
            Vector4 vector;
            texture ??= environmentData.SubsystemTerrain != null
                ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture
                : BlocksTexturesManager.DefaultBlocksTexture;
            int textureSlotCount = block.GetTextureSlotCount(value);
            int textureSlot = block.GetFaceTextureSlot(-1, value);
            if (textureSlotCount == 16) {
                vector = m_slotTexCoords[textureSlot];
            }
            else {
                float tx = (float)(textureSlot % textureSlotCount) / textureSlotCount;
                float ty = (float)(textureSlot / textureSlotCount) / textureSlotCount;
                vector = new Vector4(tx, ty, tx + 1f / textureSlotCount, ty + 1f / textureSlotCount);
            }
            if (!isEmissive) {
                float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
                color = Color.MultiplyColorOnly(color, s);
            }
            Vector3 translation = matrix.Translation;
            Vector3 vector2;
            Vector3 vector3;
            if (environmentData.BillboardDirection.HasValue) {
                vector2 = Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, Vector3.UnitY));
                vector3 = -Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, vector2));
            }
            else {
                vector2 = matrix.Right;
                vector3 = matrix.Up;
            }
            Vector3 v = translation + 0.85f * size * (-vector2 - vector3);
            Vector3 v2 = translation + 0.85f * size * (vector2 - vector3);
            Vector3 v3 = translation + 0.85f * size * (-vector2 + vector3);
            Vector3 v4 = translation + 0.85f * size * (vector2 + vector3);
            if (environmentData.ViewProjectionMatrix.HasValue) {
                Matrix m = environmentData.ViewProjectionMatrix.Value;
                Vector3.Transform(ref v, ref m, out v);
                Vector3.Transform(ref v2, ref m, out v2);
                Vector3.Transform(ref v3, ref m, out v3);
                Vector3.Transform(ref v4, ref m, out v4);
            }
            TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(
                texture,
                true,
                0,
                null,
                RasterizerState.CullCounterClockwiseScissor,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            texturedBatch3D.QueueQuad(
                v,
                v3,
                v4,
                v2,
                new Vector2(vector.X, vector.W),
                new Vector2(vector.X, vector.Y),
                new Vector2(vector.Z, vector.Y),
                new Vector2(vector.Z, vector.W),
                color
            );
            if (!environmentData.BillboardDirection.HasValue) {
                texturedBatch3D.QueueQuad(
                    v,
                    v2,
                    v4,
                    v3,
                    new Vector2(vector.X, vector.W),
                    new Vector2(vector.Z, vector.W),
                    new Vector2(vector.Z, vector.Y),
                    new Vector2(vector.X, vector.Y),
                    color
                );
            }
        }

        public static void DrawImageExtrusionBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            float size,
            ref Matrix matrix,
            Color color,
            DrawBlockEnvironmentData environmentData) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            int num = Terrain.ExtractContents(value);
            Block block = Blocks[num];
            try {
                Image image = environmentData.SubsystemTerrain != null
                    ? (Image)environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture.Tag
                    : (Image)BlocksTexturesManager.DefaultBlocksTexture.Tag;
                BlockMesh imageExtrusionBlockMesh = GetImageExtrusionBlockMesh(image, block.GetFaceTextureSlot(-1, value));
                DrawMeshBlock(primitivesRenderer, imageExtrusionBlockMesh, color, 1.7f * size, ref matrix, environmentData);
            }
            catch (Exception) {
                // ignored
            }
        }

        public static BlockMesh GetImageExtrusionBlockMesh(Image image, int slot) {
            ImageExtrusionKey imageExtrusionKey = default;
            imageExtrusionKey.Image = image;
            imageExtrusionKey.Slot = slot;
            if (!m_imageExtrusionsCache.TryGetValue(imageExtrusionKey, out BlockMesh value)) {
                value = new BlockMesh();
                int num = (int)MathF.Round(m_slotTexCoords[slot].X * image.Width);
                int num2 = (int)MathF.Round(m_slotTexCoords[slot].Y * image.Height);
                int num3 = (int)MathF.Round(m_slotTexCoords[slot].Z * image.Width);
                int num4 = (int)MathF.Round(m_slotTexCoords[slot].W * image.Height);
                int num5 = MathUtils.Max(num3 - num, num4 - num2);
                value.AppendImageExtrusion(
                    image,
                    new Rectangle(num, num2, num3 - num, num4 - num2),
                    new Vector3(1f / num5, 1f / num5, 0.0833333358f),
                    Color.White,
                    0
                );
                m_imageExtrusionsCache.Add(imageExtrusionKey, value);
            }
            return value;
        }

        public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer,
            BlockMesh blockMesh,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            Texture2D texture = environmentData.SubsystemTerrain != null
                ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture
                : BlocksTexturesManager.DefaultBlocksTexture;
            DrawMeshBlock(
                primitivesRenderer,
                blockMesh,
                texture,
                Color.White,
                size,
                ref matrix,
                environmentData
            );
        }

        public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer,
            BlockMesh blockMesh,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            Texture2D texture = environmentData.SubsystemTerrain != null
                ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture
                : BlocksTexturesManager.DefaultBlocksTexture;
            DrawMeshBlock(
                primitivesRenderer,
                blockMesh,
                texture,
                color,
                size,
                ref matrix,
                environmentData
            );
        }

        public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer,
            BlockMesh blockMesh,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData,
            GeometrySubsetType subsetType) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            Texture2D texture = environmentData.SubsystemTerrain != null
                ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture
                : BlocksTexturesManager.DefaultBlocksTexture;
            DrawMeshBlock(
                primitivesRenderer,
                blockMesh,
                texture,
                color,
                size,
                ref matrix,
                environmentData,
                subsetType
            );
        }

        public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer,
            BlockMesh blockMesh,
            Texture2D texture,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            DrawMeshBlock(
                primitivesRenderer,
                blockMesh,
                texture,
                color,
                size,
                ref matrix,
                environmentData,
                GeometrySubsetType.AlphaTest
            );
        }

        public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer,
            BlockMesh blockMesh,
            Texture2D texture,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData,
            GeometrySubsetType subsetType) {
            environmentData = environmentData ?? m_defaultEnvironmentData;
            float num = LightingManager.LightIntensityByLightValue[environmentData.Light];
            Vector4 vector = new(color);
            Vector4 vector2 = new(new Vector3(vector.X, vector.Y, vector.Z) * num, vector.W);
            // AlphaTest：cutout 着色器，写深度（镂空边缘硬边）。Transparent：关闭 alpha-test 走 blend，
            // 且不写深度（DepthRead），避免半透明体素互相 z-reject 导致后绘背面不混合。Opaque：普通不透明。
            bool useAlphaTest = subsetType == GeometrySubsetType.AlphaTest;
            DepthStencilState depthStencilState = subsetType == GeometrySubsetType.Transparent
                ? DepthStencilState.DepthRead
                : DepthStencilState.Default;
            TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(
                texture,
                useAlphaTest,
                0,
                depthStencilState,
                RasterizerState.CullCounterClockwiseScissor,
                null,
                SamplerState.PointClamp
            );
            bool flag = false;
            Matrix m = !environmentData.ViewProjectionMatrix.HasValue ? matrix : matrix * environmentData.ViewProjectionMatrix.Value;
            if (size != 1f) {
                m = Matrix.CreateScale(size) * m;
            }
            if (m.M14 != 0f
                || m.M24 != 0f
                || m.M34 != 0f
                || m.M44 != 1f) {
                flag = true;
            }
            int count = blockMesh.Vertices.Count;
            BlockMeshVertex[] array = blockMesh.Vertices.Array;
            int count2 = blockMesh.Indices.Count;
            int[] array2 = blockMesh.Indices.Array;
            DynamicArray<VertexPositionColorTexture> triangleVertices = texturedBatch3D.TriangleVertices;
            int count3 = triangleVertices.Count;
            int count4 = triangleVertices.Count;
            triangleVertices.Count += count;
            for (int i = 0; i < count; i++) {
                BlockMeshVertex blockMeshVertex = array[i];
                if (flag) {
                    Vector4 v2 = new(blockMeshVertex.Position, 1f);
                    Vector4.Transform(ref v2, ref m, out v2);
                    float num2 = 1f / v2.W;
                    blockMeshVertex.Position = new Vector3(v2.X * num2, v2.Y * num2, v2.Z * num2);
                }
                else {
                    Vector3.Transform(ref blockMeshVertex.Position, ref m, out blockMeshVertex.Position);
                }
                Color color2 = !blockMeshVertex.IsEmissive
                    ? new Color(
                        (byte)(blockMeshVertex.Color.R * vector2.X),
                        (byte)(blockMeshVertex.Color.G * vector2.Y),
                        (byte)(blockMeshVertex.Color.B * vector2.Z),
                        (byte)(blockMeshVertex.Color.A * vector2.W)
                    )
                    : new Color(
                        (byte)(blockMeshVertex.Color.R * vector.X),
                        (byte)(blockMeshVertex.Color.G * vector.Y),
                        (byte)(blockMeshVertex.Color.B * vector.Z),
                        (byte)(blockMeshVertex.Color.A * vector.W)
                    );
                triangleVertices.Array[count4++] = new VertexPositionColorTexture(
                    blockMeshVertex.Position,
                    color2,
                    blockMeshVertex.TextureCoordinates
                );
            }
            DynamicArray<int> triangleIndices = texturedBatch3D.TriangleIndices;
            int count5 = triangleIndices.Count;
            triangleIndices.Count += count2;
            for (int j = 0; j < count2; j++) {
                triangleIndices.Array[count5++] = count3 + array2[j];
            }
        }

        public static int DamageItem(int value, int damageCount, Entity owner = null) {
            int num = Terrain.ExtractContents(value);
            Block block = Blocks[num];
            int result = 0;
            bool skipVanilla = false;
            ModsManager.HookAction(
                "DamageItem",
                modLoader => {
                    result = modLoader.DamageItem(block, value, damageCount, owner, out skipVanilla);
                    return false;
                }
            );
            if (skipVanilla) {
                return result;
            }
            int durability = block.GetDurability(value);
            if (durability >= 0) {
                int num2 = block.GetDamage(value) + damageCount;
                if (num2 <= durability) {
                    return block.SetDamage(value, num2);
                }
                return block.GetDamageDestructionValue(value);
            }
            return value;
        }

        public static void LoadBlocksData(string blocksDataString) {
            blocksDataString = blocksDataString.Replace("\r", string.Empty);
            string[] lines = blocksDataString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string[] firstLine = lines[0].Split(';');
            for (int i = 1; i < lines.Length; i++) {
                string line = lines[i];
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }
                string[] array = line.Split(';');
                if (array.Length != firstLine.Length) {
                    throw new InvalidOperationException(
                        $"{string.Format(LanguageControl.Get("BlocksManager", "2"), array.Length > 0 ? array[0] : LanguageControl.Unknown)}{string.Format(LanguageControl.Get("BlocksManager", "7"), firstLine.Length, array.Length)}"
                    );
                }
                string typeName = array[0];
                if (string.IsNullOrEmpty(typeName)) {
                    continue;
                }
                Block block = m_blocks.FirstOrDefault(v => v.GetType().Name == typeName);
                if (block == null) {
                    Log.Warning(string.Format(LanguageControl.Get("BlocksManager", 3), typeName));
                    continue;
                }
                Dictionary<string, FieldInfo> fieldInfos = new();
#pragma warning disable IL2072
                foreach (FieldInfo runtimeField in block.GetType().GetRuntimeFields()) {
#pragma warning disable IL2072
                    if (runtimeField.IsPublic
                        && !runtimeField.IsStatic) {
                        fieldInfos.Add(runtimeField.Name, runtimeField);
                    }
                }
                for (int j = 1; j < array.Length; j++) {
                    string fieldName = firstLine[j];
                    string data = array[j];
                    if (!string.IsNullOrEmpty(data)) {
                        if (!fieldInfos.TryGetValue(fieldName, out FieldInfo value)) {
                            throw new InvalidOperationException(string.Format(LanguageControl.Get("BlocksManager", "8"), fieldName, typeName));
                        }
                        object obj;
                        if (data.StartsWith('#')) {
                            string refTypeName = data.Substring(1);
                            obj = !string.IsNullOrEmpty(refTypeName)
                                ? (m_blocks.FirstOrDefault(v => v.GetType().Name == refTypeName)
                                    ?? throw new InvalidOperationException(
                                        string.Format(LanguageControl.Get("BlocksManager", "9"), refTypeName, typeName, fieldName)
                                    )).BlockIndex
                                : (object)block.BlockIndex;
                        }
                        else {
                            obj = HumanReadableConverter.ConvertFromString(value.FieldType, data);
                        }
                        value.SetValue(block, obj);
                    }
                }
            }
        }

        public static void CalculateSlotTexCoordTables() {
            for (int i = 0; i < 256; i++) {
                m_slotTexCoords[i] = TextureSlotToTextureCoords(i);
            }
        }

        public static Vector4 TextureSlotToTextureCoords(int slot) {
            int num = slot % 16;
            int num2 = slot / 16;
            float x = (num + 0.001f) / 16f;
            float y = (num2 + 0.001f) / 16f;
            float z = (num + 1 - 0.001f) / 16f;
            float w = (num2 + 1 - 0.001f) / 16f;
            return new Vector4(x, y, z, w);
        }

        public static Vector4[] GetslotTexCoords(int textureSlotCount) {
            int totalCount = textureSlotCount * textureSlotCount;
            Vector4[] slotTexCoords = new Vector4[totalCount];
            for (int i = 0; i < totalCount; i++) {
                int num = i % textureSlotCount;
                int num2 = i / textureSlotCount;
                float x = (num + 0.001f) / textureSlotCount;
                float y = (num2 + 0.001f) / textureSlotCount;
                float z = (num + 1 - 0.001f) / textureSlotCount;
                float w = (num2 + 1 - 0.001f) / textureSlotCount;
                slotTexCoords[i] = new Vector4(x, y, z, w);
            }
            return slotTexCoords;
        }

        [Obsolete("Use BlocksManager.GetBlock() instead.")]
        public static Block GetBlockInMod(string ModSpace, string TypeFullName) {
            if (ModsManager.GetModEntity(ModSpace, out ModEntity modEntity)) {
                Type type = modEntity.BlockTypes.Find(p => p.Name == TypeFullName);
                Block block = GetBlock(type, false, true);
                if (block != null) {
                    return block;
                }
            }
            return null;
        }
    }
}