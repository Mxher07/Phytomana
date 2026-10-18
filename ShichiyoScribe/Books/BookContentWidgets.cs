using System;
using System.Collections.Generic;
using Engine;

namespace Game {
    /// <summary>
    ///     内容页专属控件：通过方块"类名"显示方块图标，与方块索引无关（模组间兼容）。
    ///     方块名可附带数据值，例如 "WoolBlock:+3"（彩色方块按数据值显示对应颜色）；
    ///     未填写数据值缺省为 0。图标显示尺寸自动缩小 <see cref="DisplayScaleFactor"/>（19%）。
    ///     用法（页面 XML）：
    ///     <code>&lt;BookBlockIconWidget BlockName="IronOreBlock" Size="56, 56" Scale="1.6"/&gt;</code>
    ///     <code>&lt;BookBlockIconWidget BlockName="WoolBlock:+3" Size="56, 56" Scale="1.6"/&gt;</code>
    /// </summary>
    public class BookBlockIconWidget : BlockIconWidget {
        /// <summary>图标显示尺寸的统一缩小系数（缩小 19% ⇒ 0.81）。</summary>
        public const float DisplayScaleFactor = 0.81f;

        /// <summary>方块类名，如 "IronOreBlock" 或 "WoolBlock:+3"。</summary>
        public string BlockName {
            set => SetBlock(value);
        }

        /// <summary>
        ///     重写 Scale：对外保留"原始比例"，实际绘制按 <see cref="DisplayScaleFactor"/> 缩小。
        ///     XML 里的 Scale="1.6" 依旧写 1.6，显示时自动乘 0.81。
        /// </summary>
        public new float Scale {
            get => base.Scale / DisplayScaleFactor;
            set => base.Scale = value * DisplayScaleFactor;
        }

        public BookBlockIconWidget() {
            Scale = 1f;
        }

        /// <summary>设置方块。支持 "类名"、"类名:N"、"类名:+N"（N 为方块数据值）。</summary>
        public void SetBlock(string blockName) {
            if (string.IsNullOrEmpty(blockName)) {
                OlderBookLog.Warning($"BookBlockIconWidget: empty block name.");
                return;
            }
            string name = blockName.Trim();
            int data = 0;
            int separator = name.LastIndexOfAny([':', '：']);
            if (separator >= 0 && separator + 1 < name.Length) {
                string dataPart = name[(separator + 1)..].Trim();
                if (int.TryParse(dataPart.TrimStart('+', '＋'), out int parsed)) {
                    data = parsed;
                    name = name[..separator].Trim();
                }
            }
            int index = BlocksManager.GetBlockIndex(name, false);
            if (index < 0) {
                OlderBookLog.Warning($"Block '{name}' not found for BookBlockIconWidget.");
                return;
            }
            Contents = index;
            if (data != 0) {
                Data = data;
            }
        }
    }

    /// <summary>
    ///     内容页专属控件：把若干方块图标排成一个原版 GridPanelWidget 网格。
    ///     用法（页面 XML）：
    ///     <code>&lt;BookIconGridWidget BlockNames="DirtBlock,GrassBlock,SandBlock;IronOreBlock,CoalBlock,IronBlock" IconSize="56" /&gt;</code>
    ///     分号 ";" 分隔行，逗号 "," 分隔列，网格行列数自动计算。单个方块名同样支持数据值（如 "WoolBlock:+3"）。
    ///     图标显示自动缩小 19%。
    /// </summary>
    public class BookIconGridWidget : CanvasWidget {
        public GridPanelWidget m_grid = new();

        public string BlockNames {
            set {
                char[] rowSeparators = [';', '；'];
                char[] cellSeparators = [',', '，'];
                List<string> names = [];
                int columns = 1;
                int rows = 0;
                foreach (string row in value.Split(rowSeparators, StringSplitOptions.RemoveEmptyEntries)) {
                    string[] cells = row.Split(cellSeparators, StringSplitOptions.RemoveEmptyEntries);
                    columns = Math.Max(columns, cells.Length);
                    rows++;
                    names.AddRange(cells);
                }
                SetBlockNames(names, Math.Max(1, columns), Math.Max(1, rows));
            }
        }

        public float IconSize { get; set; } = 56f;

        public BookIconGridWidget() {
            IsHitTestVisible = false;
            Children.Add(m_grid);
            m_grid.IsHitTestVisible = false;
        }

        public void SetBlockNames(IEnumerable<string> names, int columns, int rows) {
            m_grid.Children.Clear();
            columns = Math.Max(1, columns);
            rows = Math.Max(1, rows);
            int index = 0;
            foreach (string name in names) {
                BookBlockIconWidget icon = new() {
                    Size = new Vector2(IconSize),
                    Scale = 1.4f
                };
                icon.SetBlock(name);
                m_grid.Children.Add(icon);
                m_grid.SetWidgetCell(icon, new Point2(index % columns, index / columns));
                index++;
                if (index >= columns * rows) {
                    break;
                }
            }
            // 余位补空占位，保证网格对齐
            while (index < columns * rows) {
                CanvasWidget empty = new() {
                    Size = new Vector2(IconSize),
                    IsHitTestVisible = false
                };
                m_grid.Children.Add(empty);
                m_grid.SetWidgetCell(empty, new Point2(index % columns, index / columns));
                index++;
            }
            m_grid.ColumnsCount = columns;
            m_grid.RowsCount = rows;
        }
    }
}