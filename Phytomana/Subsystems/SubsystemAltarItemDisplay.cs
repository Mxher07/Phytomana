using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 祭坛物品悬浮显示：以 IDrawable 通道每帧绘制花药台与符文台上放置的材料。
    /// 类似掉落物：绕竖轴持续自转（高度不变），多件材料绕祭坛中心呈圆形排布旋转；
    /// 尺寸为掉落物的 0.35 倍（0.3f × 0.35 ≈ 0.105f）。
    /// </summary>
    public class SubsystemAltarItemDisplay : Subsystem, IDrawable {
        public static readonly int[] DrawOrderValues = [10];

        /// <summary>掉落物默认绘制尺寸 0.3f × 0.35。</summary>
        public const float ItemDrawSize = 0.105f;

        /// <summary>自转/公转角速度基准（与掉落物一致：2π 个时间单位一圈）。</summary>
        public const float SpinPeriod = 6.2831855f;

        /// <summary>材料绕祭坛中心的公转半径。</summary>
        public const float OrbitRadius = 0.3f;

        public PrimitivesRenderer3D m_primitivesRenderer = new();

        public DrawBlockEnvironmentData m_environmentData = new();

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemFlowerTableBehavior m_subsystemFlowerTable;

        public SubsystemRunesTableBehavior m_subsystemRunesTable;

        public int[] DrawOrders => DrawOrderValues;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemFlowerTable = Project.FindSubsystem<SubsystemFlowerTableBehavior>(false);
            m_subsystemRunesTable = Project.FindSubsystem<SubsystemRunesTableBehavior>(false);
        }

        public void Draw(Camera camera, int drawOrder) {
            if (drawOrder != 10) {
                return;
            }
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            float spinAngle = (float)MathUtils.Remainder(time, SpinPeriod);
            m_environmentData.SubsystemTerrain = m_subsystemTerrain;
            if (m_subsystemFlowerTable != null) {
                foreach (KeyValuePair<Point3, FlowerTable> pair in m_subsystemFlowerTable.m_tables) {
                    DrawTableItems(camera, pair.Key, pair.Value.Ingredients, spinAngle, 0.75f);
                }
            }
            if (m_subsystemRunesTable != null) {
                foreach (KeyValuePair<Point3, RunesTable> pair in m_subsystemRunesTable.m_tables) {
                    DrawTableItems(camera, pair.Key, pair.Value.Items, spinAngle, 0.95f);
                }
            }
            m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
        }

        /// <summary>绘制一张祭坛上悬浮的材料：绕中心圆形排布，各自自转，高度固定。</summary>
        public void DrawTableItems(Camera camera, Point3 cell, Dictionary<int, int> items, float spinAngle, float height) {
            List<int> values = [];
            foreach (KeyValuePair<int, int> item in items) {
                if (item.Value > 0) {
                    values.Add(item.Key);
                }
            }
            int count = values.Count;
            if (count == 0) {
                return;
            }
            if (m_subsystemTerrain.Terrain.GetChunkAtCell(cell.X, cell.Z) == null) {
                return;
            }
            Vector3 center = new(cell.X + 0.5f, cell.Y + height, cell.Z + 0.5f);
            // 简单距离剔除（64 格外的祭坛不绘制）
            Vector3 toCenter = center - camera.ViewPosition;
            if (toCenter.LengthSquared() > 64f * 64f) {
                return;
            }
            m_environmentData.Light = m_subsystemTerrain.Terrain.GetCellLightFast(cell.X, cell.Y + 1, cell.Z);
            for (int i = 0; i < count; i++) {
                // 绕祭坛中心的圆形排布：第 i 件材料错开 2π/n 相位
                float orbitAngle = spinAngle + i * (SpinPeriod / count);
                Vector3 anchor = center + new Vector3(
                    MathF.Sin(orbitAngle) * OrbitRadius,
                    0f,
                    MathF.Cos(orbitAngle) * OrbitRadius
                );
                Matrix drawMatrix = Matrix.CreateRotationY(spinAngle);
                drawMatrix.Translation = anchor;
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(values[i])];
                block?.DrawBlock(m_primitivesRenderer, values[i], Color.White, ItemDrawSize, ref drawMatrix, m_environmentData);
            }
        }
    }
}