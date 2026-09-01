using System.Globalization;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game {
    public class SubsystemFurnitureBlockBehavior : SubsystemBlockBehavior {
        public const int MaxFurnitureSetNameLength = 64;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemSoundMaterials m_subsystemSoundMaterials;

        public SubsystemItemsScanner m_subsystemItemsScanner;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;
        public static string fName = "SubsystemFurnitureBlockBehavior";

        public List<FurnitureSet> m_furnitureSets = [];

        public FurnitureDesign[] m_furnitureDesigns = new FurnitureDesign[FurnitureDesign.maxDesign];

        public Dictionary<Point3, List<FireParticleSystem>> m_particleSystemsByCell = [];

        public override int[] HandledBlocks => [];

        public ReadOnlyList<FurnitureSet> FurnitureSets => new(m_furnitureSets);

        public FurnitureDesign GetDesign(int index) {
            if (index < 0
                || index >= m_furnitureDesigns.Length) {
                return null;
            }
            return m_furnitureDesigns[index];
        }

        public FurnitureDesign FindMatchingDesign(FurnitureDesign design) {
            for (int i = 0; i < m_furnitureDesigns.Length; i++) {
                if (m_furnitureDesigns[i] != null
                    && m_furnitureDesigns[i].Compare(design)) {
                    return m_furnitureDesigns[i];
                }
            }
            return null;
        }

        public FurnitureDesign FindMatchingDesignChain(FurnitureDesign design) {
            FurnitureDesign furnitureDesign = FindMatchingDesign(design);
            if (furnitureDesign != null
                && design.CompareChain(furnitureDesign)) {
                return furnitureDesign;
            }
            return null;
        }

        public FurnitureDesign TryAddDesign(FurnitureDesign design) {
            for (int i = 0; i < m_furnitureDesigns.Length; i++) {
                if (m_furnitureDesigns[i] != null
                    && m_furnitureDesigns[i].Compare(design)) {
                    return m_furnitureDesigns[i];
                }
            }
            for (int j = 0; j < m_furnitureDesigns.Length; j++) {
                if (m_furnitureDesigns[j] == null) {
                    AddDesign(j, design);
                    return design;
                }
            }
            GarbageCollectDesigns();
            for (int k = 0; k < m_furnitureDesigns.Length; k++) {
                if (m_furnitureDesigns[k] == null) {
                    AddDesign(k, design);
                    return design;
                }
            }
            return null;
        }

        public FurnitureDesign TryAddDesignChain(FurnitureDesign design, bool garbageCollectIfNeeded) {
            FurnitureDesign furnitureDesign = FindMatchingDesignChain(design);
            if (furnitureDesign != null) {
                return furnitureDesign;
            }
            List<FurnitureDesign> list = design.ListChain();
            if (garbageCollectIfNeeded && m_furnitureDesigns.Count(d => d == null) < list.Count) {
                GarbageCollectDesigns();
            }
            if (m_furnitureDesigns.Count(d => d == null) < list.Count) {
                return null;
            }
            int num = 0;
            for (int i = 0; i < m_furnitureDesigns.Length; i++) {
                if (num >= list.Count) {
                    break;
                }
                if (m_furnitureDesigns[i] == null) {
                    AddDesign(i, list[num]);
                    num++;
                }
            }
            if (num != list.Count) {
                throw new InvalidOperationException("public error.");
            }
            return design;
        }

        public void ScanDesign(CellFace start, Vector3 direction, ComponentMiner componentMiner) {
            FurnitureDesign design;
            FurnitureDesign furnitureDesign = null;
            Dictionary<Point3, int> valuesDictionary = new();
            Point3 point = start.Point;
            Point3 point2 = start.Point;
            int startValue = SubsystemTerrain.Terrain.GetCellValue(start.Point.X, start.Point.Y, start.Point.Z);
            int num = Terrain.ExtractContents(startValue);
            bool designedFromExistingFurniture = false;
            if (BlocksManager.Blocks[num] is FurnitureBlock) {
                int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(startValue));
                furnitureDesign = GetDesign(designIndex);
                if (furnitureDesign == null) {
                    componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 0), Color.White, true, false);
                    return;
                }
                design = furnitureDesign.Clone();
                design.LinkedDesign = null;
                design.InteractionMode = FurnitureInteractionMode.None;
                designedFromExistingFurniture = true;
                valuesDictionary.Add(start.Point, startValue);
            }
            else {
                Stack<Point3> val = new();
                val.Push(start.Point);
                HashSet<Point3> scannedPoints = [];
                while (val.Count > 0) {
                    Point3 key = val.Pop();
                    if (valuesDictionary.ContainsKey(key)
                        || !SubsystemTerrain.Terrain.IsCellValid(key.X, key.Y, key.Z)) {
                        continue;
                    }
                    int cellValue = SubsystemTerrain.Terrain.GetCellValueFast(key.X, key.Y, key.Z);
                    if (IsValueDisallowed(cellValue)) {
                        componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, false);
                        return;
                    }
                    TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(key.X, key.Z);
                    if (IsValueAllowed(cellValue)) {
                        if (chunkAtCell == null
                            || chunkAtCell.State <= TerrainChunkState.InvalidContents4
                            || (!chunkAtCell.IsLoaded && chunkAtCell.ModificationCounter == 0)) {
                            componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 4), Color.White, true, false);
                            return;
                        }
                        if (key.X < point.X) {
                            point.X = key.X;
                        }
                        if (key.Y < point.Y) {
                            point.Y = key.Y;
                        }
                        if (key.Z < point.Z) {
                            point.Z = key.Z;
                        }
                        if (key.X > point2.X) {
                            point2.X = key.X;
                        }
                        if (key.Y > point2.Y) {
                            point2.Y = key.Y;
                        }
                        if (key.Z > point2.Z) {
                            point2.Z = key.Z;
                        }
                        if (MathF.Abs(point.X - point2.X) >= FurnitureDesign.MaxResolution
                            || MathF.Abs(point.Y - point2.Y) >= FurnitureDesign.MaxResolution
                            || MathF.Abs(point.Z - point2.Z) >= FurnitureDesign.MaxResolution) {
                            componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, true, false);
                            return;
                        }
                        valuesDictionary[key] = cellValue;
                        Point3 p1 = new(key.X - 1, key.Y, key.Z);
                        if (scannedPoints.Add(p1)) {
                            val.Push(p1);
                        }
                        Point3 p2 = new(key.X + 1, key.Y, key.Z);
                        if (scannedPoints.Add(p2)) {
                            val.Push(p2);
                        }
                        Point3 p3 = new(key.X, key.Y - 1, key.Z);
                        if (scannedPoints.Add(p3)) {
                            val.Push(p3);
                        }
                        Point3 p4 = new(key.X, key.Y + 1, key.Z);
                        if (scannedPoints.Add(p4)) {
                            val.Push(p4);
                        }
                        Point3 p5 = new(key.X, key.Y, key.Z - 1);
                        if (scannedPoints.Add(p5)) {
                            val.Push(p5);
                        }
                        Point3 p6 = new(key.X, key.Y, key.Z + 1);
                        if (scannedPoints.Add(p6)) {
                            val.Push(p6);
                        }
                    }
                }
                if (valuesDictionary.Count == 0) {
                    componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "6"), Color.White, true, false);
                    return;
                }
                design = new FurnitureDesign(SubsystemTerrain);
                Point3 point3 = point2 - point;
                int num2 = MathUtils.Max(MathUtils.Max(point3.X, point3.Y, point3.Z) + 1, 2);
                int[] array = new int[num2 * num2 * num2];
                foreach (KeyValuePair<Point3, int> item in valuesDictionary) {
                    Point3 point4 = item.Key - point;
                    array[point4.X + point4.Y * num2 + point4.Z * num2 * num2] = item.Value;
                }
                design.SetValues(num2, array);
                int steps = start.Face > 3 ? CellFace.Vector3ToFace(direction, 3) : CellFace.OppositeFace(start.Face);
                design.Rotate(1, steps);
                Point3 location = design.Box.Location;
                Point3 point5 = new Point3(design.Resolution) - (design.Box.Location + design.Box.Size);
                Point3 delta = new((point5.X - location.X) / 2, -location.Y, (point5.Z - location.Z) / 2);
                design.Shift(delta);
            }
            BuildFurnitureDialog dialog = new(
                design,
                furnitureDesign,
                delegate(bool result) {
                    if (result) {
                        design = TryAddDesign(design);
                        if (design == null) {
                            componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, true, false);
                        }
                        else {
                            bool destroyDesignBlocks = m_subsystemGameInfo.WorldSettings.GameMode != 0;
                            int value = Terrain.MakeBlockValue(
                                227,
                                0,
                                FurnitureBlock.SetDesignIndex(0, design.Index, design.ShadowStrengthFactor, design.IsLightEmitter)
                            );
                            int num3 = Math.Clamp(design.Resolution, 4, 8);
                            Matrix matrix = componentMiner.ComponentCreature.ComponentBody.Matrix;
                            Vector3 position = matrix.Translation + 1f * matrix.Forward + 1f * Vector3.UnitY;
                            int toolDamageCount = 1;
                            ModsManager.HookAction(
                                "OnFurnitureDesigned",
                                modLoader => {
                                    modLoader.OnFurnitureDesigned(
                                        design,
                                        designedFromExistingFurniture,
                                        ref num3,
                                        ref destroyDesignBlocks,
                                        ref toolDamageCount
                                    );
                                    return false;
                                }
                            );
                            Block block = BlocksManager.Blocks[Terrain.ExtractContents(componentMiner.ActiveBlockValue)];
                            int durabilityRemains = block.GetDurability(componentMiner.ActiveBlockValue)
                                - block.GetDamage(componentMiner.ActiveBlockValue)
                                + 1;
                            if (durabilityRemains < toolDamageCount) {
                                componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                                    LanguageControl.Get(fName, 5),
                                    Color.White,
                                    true,
                                    false
                                );
                            }
                            else {
                                componentMiner.DamageActiveTool(toolDamageCount);
                                if (destroyDesignBlocks) {
                                    foreach (KeyValuePair<Point3, int> item2 in valuesDictionary) {
                                        SubsystemTerrain.DestroyCell(
                                            0,
                                            item2.Key.X,
                                            item2.Key.Y,
                                            item2.Key.Z,
                                            0,
                                            true,
                                            true
                                        );
                                    }
                                }
                                m_subsystemPickables.AddPickable(value, num3, position, null, null);
                                componentMiner.Poke(false);
                                for (int i = 0; i < 3; i++) {
                                    Time.QueueTimeDelayedExecution(
                                        Time.FrameStartTime + i * 0.25f,
                                        delegate { m_subsystemSoundMaterials.PlayImpactSound(startValue, new Vector3(start.Point), 1f); }
                                    );
                                }
                                if (componentMiner.ComponentCreature.PlayerStats != null) {
                                    componentMiner.ComponentCreature.PlayerStats.FurnitureItemsMade += num3;
                                }
                            }
                        }
                    }
                }
            );
            if (componentMiner.ComponentPlayer != null) {
                DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GuiWidget, dialog);
            }
        }

        public void SwitchToNextState(int x, int y, int z, bool playSound) {
            HashSet<Point3> hashSet = [];
            List<Point3> list = [new(x, y, z)];
            int num = 0;
            while (num < list.Count
                && num < 4096) {
                Point3 item = list[num++];
                if (!hashSet.Add(item)) {
                    continue;
                }
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(item.X, item.Y, item.Z);
                if (Terrain.ExtractContents(cellValue) != 227) {
                    continue;
                }
                int data = Terrain.ExtractData(cellValue);
                int designIndex = FurnitureBlock.GetDesignIndex(data);
                FurnitureDesign design = GetDesign(designIndex);
                if (design != null
                    && design.LinkedDesign != null
                    && design.LinkedDesign.Index >= 0
                    && (list.Count == 1 || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate)) {
                    int data2 = FurnitureBlock.SetDesignIndex(
                        data,
                        design.LinkedDesign.Index,
                        design.LinkedDesign.ShadowStrengthFactor,
                        design.LinkedDesign.IsLightEmitter
                    );
                    int value = Terrain.ReplaceData(cellValue, data2);
                    SubsystemTerrain.ChangeCell(item.X, item.Y, item.Z, value);
                    if (design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate) {
                        list.Add(new Point3(item.X - 1, item.Y, item.Z));
                        list.Add(new Point3(item.X + 1, item.Y, item.Z));
                        list.Add(new Point3(item.X, item.Y - 1, item.Z));
                        list.Add(new Point3(item.X, item.Y + 1, item.Z));
                        list.Add(new Point3(item.X, item.Y, item.Z - 1));
                        list.Add(new Point3(item.X, item.Y, item.Z + 1));
                    }
                }
            }
            if (playSound) {
                m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(x, y, z), 2f, true);
            }
        }

        public void GarbageCollectDesigns() {
            GarbageCollectDesigns(m_subsystemItemsScanner.ScanItems());
        }

        public FurnitureSet NewFurnitureSet(string name, string importedFrom) {
            if (name.Length > MaxFurnitureSetNameLength) {
                name = name.Substring(0, MaxFurnitureSetNameLength);
            }
            int num = 0;
            while (FurnitureSets.FirstOrDefault(fs => fs.Name == name) != null) {
                num++;
                name = num > 0 ? name + num.ToString(CultureInfo.InvariantCulture) : name;
            }
            FurnitureSet furnitureSet = new() { Name = name, ImportedFrom = importedFrom };
            m_furnitureSets.Add(furnitureSet);
            return furnitureSet;
        }

        public void DeleteFurnitureSet(FurnitureSet furnitureSet) {
            foreach (FurnitureDesign furnitureSetDesign in GetFurnitureSetDesigns(furnitureSet)) {
                furnitureSetDesign.FurnitureSet = null;
            }
            m_furnitureSets.Remove(furnitureSet);
        }

        public void MoveFurnitureSet(FurnitureSet furnitureSet, int move) {
            int num = m_furnitureSets.IndexOf(furnitureSet);
            if (num >= 0) {
                m_furnitureSets.RemoveAt(num);
                m_furnitureSets.Insert(Math.Clamp(num + move, 0, m_furnitureSets.Count), furnitureSet);
            }
        }

        public void AddToFurnitureSet(FurnitureDesign design, FurnitureSet furnitureSet) {
            foreach (FurnitureDesign item in design.ListChain()) {
                item.FurnitureSet = furnitureSet;
            }
        }

        public IEnumerable<FurnitureDesign> GetFurnitureSetDesigns(FurnitureSet furnitureSet) {
            return m_furnitureDesigns.Where(fd => fd != null && fd.FurnitureSet == furnitureSet);
        }

        public static List<FurnitureDesign> LoadFurnitureDesigns(SubsystemTerrain subsystemTerrain, ValuesDictionary valuesDictionary) {
            List<FurnitureDesign> list = [];
            foreach (KeyValuePair<string, object> item2 in valuesDictionary) {
                int index = int.Parse(item2.Key, CultureInfo.InvariantCulture);
                ValuesDictionary valuesDictionary2 = (ValuesDictionary)item2.Value;
                FurnitureDesign item = new(index, subsystemTerrain, valuesDictionary2);
                list.Add(item);
            }
            foreach (FurnitureDesign design in list) {
                if (design.m_loadTimeLinkedDesignIndex >= 0) {
                    design.LinkedDesign = list.FirstOrDefault(d => d.Index == design.m_loadTimeLinkedDesignIndex);
                }
            }
            return list;
        }

        public static void SaveFurnitureDesigns(ValuesDictionary valuesDictionary, ICollection<FurnitureDesign> designs) {
            foreach (FurnitureDesign design in designs) {
                valuesDictionary.SetValue(design.Index.ToString(CultureInfo.InvariantCulture), design.Save());
            }
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddTerrainFurniture(value);
            AddParticleSystems(value, x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveTerrainFurniture(value);
            RemoveParticleSystems(x, y, z);
        }

        public override void OnBlockModified(int value, int oldValue, int x, int y, int z) {
            RemoveTerrainFurniture(oldValue);
            RemoveParticleSystems(x, y, z);
            AddTerrainFurniture(value);
            AddParticleSystems(value, x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            if (!isLoaded) {
                AddTerrainFurniture(value);
            }
            AddParticleSystems(value, x, y, z);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 key in m_particleSystemsByCell.Keys) {
                if (key.X >= chunk.Origin.X
                    && key.X < chunk.Origin.X + 16
                    && key.Z >= chunk.Origin.Y
                    && key.Z < chunk.Origin.Y + 16) {
                    list.Add(key);
                }
            }
            foreach (Point3 item in list) {
                RemoveParticleSystems(item.X, item.Y, item.Z);
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
            if (Terrain.ExtractContents(cellValue) == 227) {
                int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(cellValue));
                FurnitureDesign design = GetDesign(designIndex);
                if (design != null
                    && (design.InteractionMode == FurnitureInteractionMode.Multistate
                        || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate)) {
                    SwitchToNextState(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z, true);
                    return true;
                }
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(true);
            m_subsystemItemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("FurnitureDesigns");
            foreach (FurnitureDesign item in LoadFurnitureDesigns(SubsystemTerrain, value)) {
                m_furnitureDesigns[item.Index] = item;
            }
            foreach (ValuesDictionary item2 in valuesDictionary.GetValue<ValuesDictionary>("FurnitureSets")
                .Values.Where(v => v is ValuesDictionary)) {
                string value2 = item2.GetValue<string>("Name");
                string value3 = item2.GetValue<string>("ImportedFrom", null);
                string value4 = item2.GetValue<string>("Indices");
                int[] array = HumanReadableConverter.ValuesListFromString<int>(';', value4);
                FurnitureSet furnitureSet = new() { Name = value2, ImportedFrom = value3 };
                m_furnitureSets.Add(furnitureSet);
                int[] array2 = array;
                foreach (int num in array2) {
                    if (num >= 0
                        && num < m_furnitureDesigns.Length
                        && m_furnitureDesigns[num] != null) {
                        m_furnitureDesigns[num].FurnitureSet = furnitureSet;
                    }
                }
            }
            m_subsystemItemsScanner.ItemsScanned += GarbageCollectDesigns;
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            GarbageCollectDesigns();
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("FurnitureDesigns", valuesDictionary2);
            SaveFurnitureDesigns(valuesDictionary2, m_furnitureDesigns.Where(d => d != null).ToArray());
            ValuesDictionary valuesDictionary3 = new();
            valuesDictionary.SetValue("FurnitureSets", valuesDictionary3);
            int num = 0;
            foreach (FurnitureSet furnitureSet in FurnitureSets) {
                ValuesDictionary valuesDictionary4 = new();
                valuesDictionary3.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary4);
                valuesDictionary4.SetValue("Name", furnitureSet.Name);
                if (furnitureSet.ImportedFrom != null) {
                    valuesDictionary4.SetValue("ImportedFrom", furnitureSet.ImportedFrom);
                }
                string value = HumanReadableConverter.ValuesListToString(
                    ';',
                    (from d in GetFurnitureSetDesigns(furnitureSet) select d.Index).ToArray()
                );
                valuesDictionary4.SetValue("Indices", value);
                num++;
            }
        }

        public void AddDesign(int index, FurnitureDesign design) {
            m_furnitureDesigns[index] = design;
            design.Index = index;
            design.m_terrainUseCount = 0;
        }

        public void AddTerrainFurniture(int value) {
            if (Terrain.ExtractContents(value) == 227) {
                int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(value));
                if (designIndex < m_furnitureDesigns.Length) {
                    m_furnitureDesigns[designIndex].m_terrainUseCount++;
                }
            }
        }

        public void RemoveTerrainFurniture(int value) {
            if (Terrain.ExtractContents(value) == 227) {
                int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(value));
                if (designIndex < m_furnitureDesigns.Length) {
                    m_furnitureDesigns[designIndex].m_terrainUseCount = MathUtils.Max(m_furnitureDesigns[designIndex].m_terrainUseCount - 1, 0);
                }
            }
        }

        public void GarbageCollectDesigns(ReadOnlyList<ScannedItemData> allExistingItems) {
            for (int i = 0; i < m_furnitureDesigns.Length; i++) {
                if (m_furnitureDesigns[i] != null) {
                    m_furnitureDesigns[i].m_gcUsed = m_furnitureDesigns[i].m_terrainUseCount > 0;
                }
            }
            foreach (ScannedItemData item in allExistingItems) {
                if (Terrain.ExtractContents(item.Value) == 227) {
                    int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(item.Value));
                    FurnitureDesign design = GetDesign(designIndex);
                    if (design != null) {
                        design.m_gcUsed = true;
                    }
                }
            }
            for (int j = 0; j < m_furnitureDesigns.Length; j++) {
                if (m_furnitureDesigns[j] != null
                    && m_furnitureDesigns[j].m_gcUsed) {
                    FurnitureDesign linkedDesign = m_furnitureDesigns[j].LinkedDesign;
                    while (linkedDesign != null
                        && !linkedDesign.m_gcUsed) {
                        linkedDesign.m_gcUsed = true;
                        linkedDesign = linkedDesign.LinkedDesign;
                    }
                }
            }
            for (int k = 0; k < m_furnitureDesigns.Length; k++) {
                if (m_furnitureDesigns[k] != null
                    && !m_furnitureDesigns[k].m_gcUsed
                    && m_furnitureDesigns[k].FurnitureSet == null) {
                    m_furnitureDesigns[k].Index = -1;
                    m_furnitureDesigns[k] = null;
                }
            }
        }

        public static bool IsValueAllowed(int value) {
            int contents = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[contents];
            return block.CanBlockBeBuiltIntoFurniture(value);
        }

        public static bool IsValueDisallowed(int value) {
            int num = Terrain.ExtractContents(value);
            int data = Terrain.ExtractData(value);
            if ((num == 18 || num == 92)
                && FluidBlock.GetLevel(data) != 0
                && FluidBlock.GetIsTop(data)) {
                return true;
            }
            return false;
        }

        public void AddParticleSystems(int value, int x, int y, int z) {
            if (Terrain.ExtractContents(value) != 227) {
                return;
            }
            int data = Terrain.ExtractData(value);
            int rotation = FurnitureBlock.GetRotation(data);
            int designIndex = FurnitureBlock.GetDesignIndex(data);
            FurnitureDesign design = GetDesign(designIndex);
            if (design == null) {
                return;
            }
            List<FireParticleSystem> list = [];
            BoundingBox[] torchPoints = design.GetTorchPoints(rotation);
            if (torchPoints.Length != 0) {
                BoundingBox[] array = torchPoints;
                for (int i = 0; i < array.Length; i++) {
                    BoundingBox boundingBox = array[i];
                    float num = (boundingBox.Size().X + boundingBox.Size().Y + boundingBox.Size().Z) / 3f;
                    float size = Math.Clamp(1.5f * num, 0.1f, 1f);
                    FireParticleSystem fireParticleSystem = new(new Vector3(x, y, z) + boundingBox.Center(), size, 32f);
                    m_subsystemParticles.AddParticleSystem(fireParticleSystem);
                    list.Add(fireParticleSystem);
                }
            }
            if (list.Count > 0) {
                m_particleSystemsByCell[new Point3(x, y, z)] = list;
            }
        }

        public void RemoveParticleSystems(int x, int y, int z) {
            if (m_particleSystemsByCell.TryGetValue(new Point3(x, y, z), out List<FireParticleSystem> value)) {
                foreach (FireParticleSystem item in value) {
                    item.IsStopped = true;
                }
                m_particleSystemsByCell.Remove(new Point3(x, y, z));
            }
        }
    }
}