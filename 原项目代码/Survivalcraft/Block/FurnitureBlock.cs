using Engine;
using Engine.Graphics;

namespace Game {
    public class FurnitureBlock : Block, IPaintableBlock, IElectricElementBlock {
        public static int Index = 227;
        public Matrix[] m_matrices = new Matrix[4];
        public int[][] m_facesMaps = [[0, 1, 2, 3, 4, 5], [1, 2, 3, 0, 4, 5], [2, 3, 0, 1, 4, 5], [3, 0, 1, 2, 4, 5]];
        public int[][] m_reverseFacesMaps = [[0, 1, 2, 3, 4, 5], [3, 0, 1, 2, 4, 5], [2, 3, 0, 1, 4, 5], [1, 2, 3, 0, 4, 5]];
        public const string fName = "FurnitureBlock";

        public override void Initialize() {
            for (int i = 0; i < 4; i++) {
                m_matrices[i] = Matrix.CreateTranslation(new Vector3(-0.5f, 0f, -0.5f))
                    * Matrix.CreateRotationY(i * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(new Vector3(0.5f, 0f, 0.5f));
            }
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            if (generator.SubsystemFurnitureBlockBehavior == null) {
                return;
            }
            int data = Terrain.ExtractData(value);
            int designIndex = GetDesignIndex(data);
            int rotation = GetRotation(data);
            FurnitureDesign design = generator.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
            if (design == null) {
                return;
            }
            FurnitureGeometry geometry2 = design.Geometry;
            int mountingFacesMask = design.MountingFacesMask;
            for (int i = 0; i < 6; i++) {
                int num = CellFace.OppositeFace(i < 4 ? (i + rotation) % 4 : i);
                byte b = (byte)(LightingManager.LightIntensityByLightValueAndFace[15 + 16 * num] * 255f);
                Color color = new(b, b, b);
                if (geometry2.SubsetOpaqueByFace[i] != null) {
                    generator.GenerateShadedMeshVertices(
                        this,
                        x,
                        y,
                        z,
                        geometry2.SubsetOpaqueByFace[i],
                        color,
                        m_matrices[rotation],
                        m_facesMaps[rotation],
                        geometry.OpaqueSubsetsByFace[num]
                    );
                }
                if (geometry2.SubsetAlphaTestByFace[i] != null) {
                    generator.GenerateShadedMeshVertices(
                        this,
                        x,
                        y,
                        z,
                        geometry2.SubsetAlphaTestByFace[i],
                        color,
                        m_matrices[rotation],
                        m_facesMaps[rotation],
                        geometry.AlphaTestSubsetsByFace[num]
                    );
                }
                if (geometry2.SubsetTransparentByFace[i] != null) {
                    generator.GenerateShadedMeshVertices(
                        this,
                        x,
                        y,
                        z,
                        geometry2.SubsetTransparentByFace[i],
                        color,
                        m_matrices[rotation],
                        m_facesMaps[rotation],
                        geometry.TransparentSubsetsByFace[num]
                    );
                }
                int num2 = CellFace.OppositeFace(i < 4 ? (i - rotation + 4) % 4 : i);
                if ((mountingFacesMask & (1 << num2)) != 0) {
                    generator.GenerateWireVertices(
                        value,
                        x,
                        y,
                        z,
                        i,
                        0f,
                        Vector2.Zero,
                        geometry.SubsetOpaque
                    );
                }
            }
            if (geometry2.Draws != null) {
                foreach (var kv in geometry2.Draws) {
                    TerrainGeometry targetGeo = geometry.GetGeometry(kv.Key);
                    FurnitureGeometry subGeo = kv.Value;
                    for (int i = 0; i < 6; i++) {
                        int num = CellFace.OppositeFace(i < 4 ? (i + rotation) % 4 : i);
                        byte b = (byte)(LightingManager.LightIntensityByLightValueAndFace[15 + 16 * num] * 255f);
                        Color color = new(b, b, b);
                        if (subGeo.SubsetOpaqueByFace[i] != null) {
                            generator.GenerateShadedMeshVertices(
                                this,
                                x,
                                y,
                                z,
                                subGeo.SubsetOpaqueByFace[i],
                                color,
                                m_matrices[rotation],
                                m_facesMaps[rotation],
                                targetGeo.OpaqueSubsetsByFace[num]
                            );
                        }
                        if (subGeo.SubsetAlphaTestByFace[i] != null) {
                            generator.GenerateShadedMeshVertices(
                                this,
                                x,
                                y,
                                z,
                                subGeo.SubsetAlphaTestByFace[i],
                                color,
                                m_matrices[rotation],
                                m_facesMaps[rotation],
                                targetGeo.AlphaTestSubsetsByFace[num]
                            );
                        }
                        if (subGeo.SubsetTransparentByFace[i] != null) {
                            generator.GenerateShadedMeshVertices(
                                this,
                                x,
                                y,
                                z,
                                subGeo.SubsetTransparentByFace[i],
                                color,
                                m_matrices[rotation],
                                m_facesMaps[rotation],
                                targetGeo.TransparentSubsetsByFace[num]
                            );
                        }
                    }
                }
            }
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            if (environmentData.SubsystemTerrain == null) {
                return;
            }
            int designIndex = GetDesignIndex(Terrain.ExtractData(value));
            FurnitureDesign design = environmentData.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
            if (design == null) {
                return;
            }
            Vector3 v = default;
            v.X = -0.5f * (design.Box.Left + design.Box.Right) / design.Resolution;
            v.Y = -0.5f * (design.Box.Top + design.Box.Bottom) / design.Resolution;
            v.Z = -0.5f * (design.Box.Near + design.Box.Far) / design.Resolution;
            Matrix matrix2 = Matrix.CreateTranslation(v * size) * matrix;
            FurnitureGeometry geometry = design.Geometry;
            for (int i = 0; i < 6; i++) {
                float s = LightingManager.LightIntensityByLightValueAndFace[environmentData.Light + 16 * CellFace.OppositeFace(i)];
                Color color2 = Color.MultiplyColorOnly(color, s);
                if (geometry.SubsetOpaqueByFace[i] != null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetOpaqueByFace[i], color2, size, ref matrix2, environmentData);
                }
                if (geometry.SubsetAlphaTestByFace[i] != null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetAlphaTestByFace[i], color2, size, ref matrix2, environmentData);
                }
                if (geometry.SubsetTransparentByFace[i] != null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, geometry.SubsetTransparentByFace[i], color2, size, ref matrix2, environmentData, GeometrySubsetType.Transparent);
                }
            }
            if (geometry.Draws != null) {
                foreach (var kv in geometry.Draws) {
                    Texture2D tex = kv.Key;
                    FurnitureGeometry subGeo = kv.Value;
                    for (int i = 0; i < 6; i++) {
                        float s = LightingManager.LightIntensityByLightValueAndFace[environmentData.Light + 16 * CellFace.OppositeFace(i)];
                        Color color2 = Color.MultiplyColorOnly(color, s);
                        if (subGeo.SubsetOpaqueByFace[i] != null) {
                            BlocksManager.DrawMeshBlock(primitivesRenderer, subGeo.SubsetOpaqueByFace[i], tex, color2, size, ref matrix2, environmentData);
                        }
                        if (subGeo.SubsetAlphaTestByFace[i] != null) {
                            BlocksManager.DrawMeshBlock(primitivesRenderer, subGeo.SubsetAlphaTestByFace[i], tex, color2, size, ref matrix2, environmentData);
                        }
                        if (subGeo.SubsetTransparentByFace[i] != null) {
                            BlocksManager.DrawMeshBlock(primitivesRenderer, subGeo.SubsetTransparentByFace[i], tex, color2, size, ref matrix2, environmentData, GeometrySubsetType.Transparent);
                        }
                    }
                }
            }
        }

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) {
            if (subsystemTerrain != null) {
                int data = Terrain.ExtractData(value);
                int rotation = GetRotation(data);
                int designIndex = GetDesignIndex(data);
                FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    return ((1 << m_reverseFacesMaps[rotation][face]) & design.TransparentFacesMask) != 0;
                }
            }
            return false;
        }

        public override int GetShadowStrength(int value) {
            int data = Terrain.ExtractData(value);
            if (GetIsLightEmitter(data)) {
                return -99;
            }
            return GetShadowStrengthFactor(data) * 3 + 1;
        }

        public override int GetEmittedLightAmount(int value) {
            if (!GetIsLightEmitter(Terrain.ExtractData(value))) {
                return 0;
            }
            return 15;
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            if (subsystemTerrain != null) {
                int designIndex = GetDesignIndex(Terrain.ExtractData(value));
                FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    if (!string.IsNullOrEmpty(design.Name)) {
                        return design.Name;
                    }
                    return design.GetDefaultName();
                }
            }
            return "Furniture";
        }

        public override bool IsInteractive(SubsystemTerrain subsystemTerrain, int value) {
            if (subsystemTerrain != null) {
                int designIndex = GetDesignIndex(Terrain.ExtractData(value));
                FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    if (design.InteractionMode != FurnitureInteractionMode.Multistate
                        && design.InteractionMode != FurnitureInteractionMode.ElectricButton
                        && design.InteractionMode != FurnitureInteractionMode.ElectricSwitch) {
                        return design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate;
                    }
                    return true;
                }
            }
            return base.IsInteractive(subsystemTerrain, value);
        }

        public override string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value) {
            if (subsystemTerrain != null) {
                int designIndex = GetDesignIndex(Terrain.ExtractData(value));
                FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    int mainValue = design.MainValue;
                    int num = Terrain.ExtractContents(mainValue);
                    return BlocksManager.Blocks[num].GetSoundMaterialName(subsystemTerrain, mainValue);
                }
            }
            return base.GetSoundMaterialName(subsystemTerrain, value);
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain subsystemTerrain, int value) {
            if (subsystemTerrain != null) {
                int data = Terrain.ExtractData(value);
                int designIndex = GetDesignIndex(data);
                int rotation = GetRotation(data);
                FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    return design.GetCollisionBoxes(rotation);
                }
            }
            return base.GetCustomCollisionBoxes(subsystemTerrain, value);
        }

        public override BoundingBox[] GetCustomInteractionBoxes(SubsystemTerrain subsystemTerrain, int value) {
            if (subsystemTerrain != null) {
                int data = Terrain.ExtractData(value);
                int designIndex = GetDesignIndex(data);
                int rotation = GetRotation(data);
                FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    return design.GetInteractionBoxes(rotation);
                }
            }
            return base.GetCustomInteractionBoxes(subsystemTerrain, value);
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            int faceTextureSlot = GetFaceTextureSlot(4, value);
            int designIndex = GetDesignIndex(Terrain.ExtractData(value));
            FurnitureDesign design = subsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
            if (design != null) {
                int mainValue = design.MainValue;
                int num = Terrain.ExtractContents(mainValue);
                return BlocksManager.Blocks[num].CreateDebrisParticleSystem(subsystemTerrain, position, mainValue, strength);
            }
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, faceTextureSlot);
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            int rotation = 0;
            if (raycastResult.CellFace.Face < 4) {
                rotation = CellFace.OppositeFace(raycastResult.CellFace.Face);
            }
            else {
                Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
                float num = Vector3.Dot(forward, Vector3.UnitZ);
                float num2 = Vector3.Dot(forward, Vector3.UnitX);
                float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
                float num4 = Vector3.Dot(forward, -Vector3.UnitX);
                if (num == MathUtils.Max(num, num2, num3, num4)) {
                    rotation = 0;
                }
                else if (num2 == MathUtils.Max(num, num2, num3, num4)) {
                    rotation = 1;
                }
                else if (num3 == MathUtils.Max(num, num2, num3, num4)) {
                    rotation = 2;
                }
                else if (num4 == MathUtils.Max(num, num2, num3, num4)) {
                    rotation = 3;
                }
            }
            int data = SetRotation(Terrain.ExtractData(value), rotation);
            BlockPlacementData result = default;
            result.CellFace = raycastResult.CellFace;
            result.Value = Terrain.ReplaceData(value, data);
            return result;
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
            int data = Terrain.ExtractData(oldValue);
            data = SetRotation(data, 0);
            dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(227, 0, data), Count = 1 });
        }

        public override float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData) {
            if (environmentData.SubsystemTerrain != null) {
                int designIndex = GetDesignIndex(Terrain.ExtractData(value));
                FurnitureDesign design = environmentData.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                if (design != null) {
                    float num = design.Resolution / (float)MathUtils.Max(design.Box.Width, design.Box.Height, design.Box.Depth);
                    return DefaultIconViewScale * num;
                }
            }
            return base.GetIconViewScale(value, environmentData);
        }

        public int? GetPaintColor(int value) => null;

        public int Paint(SubsystemTerrain terrain, int value, int? color) {
            int data = Terrain.ExtractData(value);
            int designIndex = GetDesignIndex(data);
            FurnitureDesign design = terrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
            if (design != null) {
                List<FurnitureDesign> list = design.CloneChain();
                foreach (FurnitureDesign item in list) {
                    item.Paint(color);
                }
                FurnitureDesign furnitureDesign = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(list[0], true);
                if (furnitureDesign != null) {
                    int data2 = SetDesignIndex(data, furnitureDesign.Index, furnitureDesign.ShadowStrengthFactor, furnitureDesign.IsLightEmitter);
                    return Terrain.ReplaceData(value, data2);
                }
                DisplayError();
            }
            return value;
        }

        public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, float playerLevel) {
            if (heatLevel != 0f) {
                return null;
            }
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            List<FurnitureDesign> list = new();
            for (int i = 0; i < ingredients.Length; i++) {
                if (string.IsNullOrEmpty(ingredients[i])) {
                    continue;
                }
                CraftingRecipesManager.DecodeIngredient(ingredients[i], out string craftingId, out int? data);
                if (craftingId == BlocksManager.Blocks[227].CraftingId) {
                    FurnitureDesign design = terrain.SubsystemFurnitureBlockBehavior.GetDesign(GetDesignIndex(data.GetValueOrDefault()));
                    if (design == null) {
                        return null;
                    }
                    list.Add(design);
                }
                else if (craftingId == BlocksManager.Blocks[142].CraftingId) {
                    num++;
                }
                else if (craftingId == BlocksManager.Blocks[141].CraftingId) {
                    num2++;
                }
                else {
                    if (craftingId != BlocksManager.Blocks[133].CraftingId) {
                        return null;
                    }
                    num3++;
                }
            }
            if (list.Count == 1
                && num == 1
                && num2 == 0
                && num3 == 0) {
                FurnitureDesign furnitureDesign = list[0].Clone();
                furnitureDesign.InteractionMode = FurnitureInteractionMode.ElectricButton;
                FurnitureDesign furnitureDesign2 = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(furnitureDesign, true);
                if (furnitureDesign2 == null) {
                    DisplayError();
                    return null;
                }
                return new CraftingRecipe {
                    ResultValue = Terrain.MakeBlockValue(
                        227,
                        0,
                        SetDesignIndex(0, furnitureDesign2.Index, furnitureDesign2.ShadowStrengthFactor, furnitureDesign2.IsLightEmitter)
                    ),
                    ResultCount = 1,
                    Description = LanguageControl.Get(fName, 0),
                    Ingredients = (string[])ingredients.Clone()
                };
            }
            if (list.Count == 2
                && num == 0
                && num2 == 1
                && num3 == 0) {
                List<FurnitureDesign> list2 = list.Select(d => d.Clone()).ToList();
                for (int j = 0; j < list2.Count; j++) {
                    list2[j].InteractionMode = FurnitureInteractionMode.ElectricSwitch;
                    list2[j].LinkedDesign = list2[(j + 1) % list2.Count];
                }
                FurnitureDesign furnitureDesign3 = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(list2[0], true);
                if (furnitureDesign3 == null) {
                    DisplayError();
                    return null;
                }
                return new CraftingRecipe {
                    ResultValue = Terrain.MakeBlockValue(
                        227,
                        0,
                        SetDesignIndex(0, furnitureDesign3.Index, furnitureDesign3.ShadowStrengthFactor, furnitureDesign3.IsLightEmitter)
                    ),
                    ResultCount = 1,
                    Description = LanguageControl.Get(fName, 0),
                    Ingredients = (string[])ingredients.Clone()
                };
            }
            if (list.Count >= 2
                && num == 0
                && num2 == 0
                && num3 <= 1) {
                List<FurnitureDesign> list3 = list.Select(d => d.Clone()).ToList();
                for (int k = 0; k < list3.Count; k++) {
                    list3[k].InteractionMode = num3 == 0 ? FurnitureInteractionMode.Multistate : FurnitureInteractionMode.ConnectedMultistate;
                    list3[k].LinkedDesign = list3[(k + 1) % list3.Count];
                }
                FurnitureDesign furnitureDesign4 = terrain.SubsystemFurnitureBlockBehavior.TryAddDesignChain(list3[0], true);
                if (furnitureDesign4 == null) {
                    DisplayError();
                    return null;
                }
                return new CraftingRecipe {
                    ResultValue = Terrain.MakeBlockValue(
                        227,
                        0,
                        SetDesignIndex(0, furnitureDesign4.Index, furnitureDesign4.ShadowStrengthFactor, furnitureDesign4.IsLightEmitter)
                    ),
                    ResultCount = 1,
                    Description = LanguageControl.Get(fName, 0),
                    Ingredients = (string[])ingredients.Clone()
                };
            }
            return null;
        }

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) {
            int designIndex = GetDesignIndex(Terrain.ExtractData(value));
            FurnitureDesign design = subsystemElectricity.SubsystemTerrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
            if (design != null) {
                if (design.InteractionMode == FurnitureInteractionMode.Multistate
                    || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate) {
                    return new MultistateFurnitureElectricElement(subsystemElectricity, new Point3(x, y, z));
                }
                if (design.InteractionMode == FurnitureInteractionMode.ElectricButton) {
                    return new ButtonFurnitureElectricElement(subsystemElectricity, new Point3(x, y, z));
                }
                if (design.InteractionMode == FurnitureInteractionMode.ElectricSwitch) {
                    return new SwitchFurnitureElectricElement(subsystemElectricity, new Point3(x, y, z), value);
                }
            }
            return null;
        }

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) {
            int data = Terrain.ExtractData(value);
            int rotation = GetRotation(data);
            int designIndex = GetDesignIndex(data);
            FurnitureDesign design = terrain.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
            if (design != null) {
                int num = CellFace.OppositeFace(face < 4 ? (face - rotation + 4) % 4 : face);
                if ((design.MountingFacesMask & (1 << num)) != 0
                    && SubsystemElectricity.GetConnectorDirection(face, 0, connectorFace).HasValue) {
                    Point3 point = CellFace.FaceToPoint3(face);
                    int cellValue = terrain.Terrain.GetCellValue(x - point.X, y - point.Y, z - point.Z);
                    if (!BlocksManager.Blocks[Terrain.ExtractContents(cellValue)]
                        .IsFaceNonAttachable(terrain, CellFace.OppositeFace(num), cellValue, value)) {
                        if (design.InteractionMode == FurnitureInteractionMode.Multistate
                            || design.InteractionMode == FurnitureInteractionMode.ConnectedMultistate) {
                            return ElectricConnectorType.Input;
                        }
                        if (design.InteractionMode == FurnitureInteractionMode.ElectricButton
                            || design.InteractionMode == FurnitureInteractionMode.ElectricSwitch) {
                            return ElectricConnectorType.Output;
                        }
                    }
                }
            }
            return null;
        }

        public int GetConnectionMask(int value) => int.MaxValue;

        public void DisplayError() {
            DialogsManager.ShowDialog(
                null,
                new MessageDialog(LanguageControl.Error, LanguageControl.Get(fName, 1), LanguageControl.Ok, null, null)
            );
        }

        public static int GetRotation(int data) => data & 3;

        public static int SetRotation(int data, int rotation) => (data & -4) | (rotation & 3);

        public static int GetDesignIndex(int data) => (((data >> 15) & 7) << 10) | ((data >> 2) & 1023);

        public static int SetDesignIndex(int data, int designIndex, int shadowStrengthFactor, bool isLightEmitter) {
            int IndexI3 = designIndex >> 10;
            int IndexI10 = designIndex & 1023;
            data = (data & 3) | ((IndexI10 & 1023) << 2); //设置旋转，和方块索引前10位
            data = (data & 4095) | ((shadowStrengthFactor & 3) << 12); //设置光强
            data = (data & 16383) | ((isLightEmitter ? 1 : 0) << 14); //设置是否发光
            data = (data & 32767) | (IndexI3 << 15); //设置索引后3位
            return data;
        }

        public static FurnitureDesign GetDesign(SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior, int value) {
            int designIndex = GetDesignIndex(Terrain.ExtractData(value));
            return subsystemFurnitureBlockBehavior.GetDesign(designIndex);
        }

        public static int GetShadowStrengthFactor(int data) => (data >> 12) & 3;

        public static bool GetIsLightEmitter(int data) => ((data >> 14) & 1) != 0;

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            isEnd = false;
            return true;
        }
    }
}