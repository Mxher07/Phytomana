using System.Collections.Generic;

namespace Game {
    /// <summary>
    ///     一本书的定义。结构：
    ///     起始页(StartPages，每个元素 = 一个"双页"跨页) → 目录页(TocText/TocLeft) → 目录分类(Section) → 子目录(SubSection) → 子目录条目(Entry)。
    ///     页内容为引索字符串，指向 Assets/ 下的 .xml（如 "ShichiyoScribe/Pages/MyBook/Page1"），
    ///     运行时通过 ContentManager.Get&lt;XElement&gt;(path) 加载为一个完整控件树。
    /// </summary>
    public class BookDefinition {
        public const string ContentRoot = "ShichiyoScribe/Pages/";

        /// <summary>书的唯一标识。</summary>
        public string Id;

        /// <summary>书名（显示在书页顶端与目录中）。</summary>
        public string Title;

        /// <summary>目录页左页文字（"起"/目录说明）。若同时设置了 <see cref="TocLeft"/> 则优先加载 XML。</summary>
        public string TocText;

        /// <summary>目录页左页 XML 内容引索（可选，优先于 TocText）。</summary>
        public string TocLeft;

        /// <summary>绑定到哪本书方块（方块类名）。持有该书方块使用时会打开这本书。</summary>
        public string BlockName;

        /// <summary>该书的起始页（一页 = 左右双页）。</summary>
        public List<BookSpread> StartPages = [];

        /// <summary>目录分类（3 个）。</summary>
        public List<BookSection> Sections = [];

        /// <summary>该书的代码注册序号（用于日志）。</summary>
        public int CodeRegisteredSequence;

        public static string NormalizeContentPath(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            path = path.Replace('\\', '/').Trim('/');
            return path.StartsWith(ContentRoot, System.StringComparison.Ordinal) ? path : ContentRoot + path;
        }
    }

    /// <summary>一个起始双页（跨页）。</summary>
    public class BookSpread {
        /// <summary>跨页标题。</summary>
        public string Title;

        /// <summary>左页兜底文字。</summary>
        public string LeftText;

        /// <summary>左页 XML 内容引索。</summary>
        public string Left;

        /// <summary>右页兜底文字。</summary>
        public string RightText;

        /// <summary>右页 XML 内容引索。</summary>
        public string Right;
    }

    /// <summary>目录分类（书内的大类，如"基础入门"）。</summary>
    public class BookSection {
        /// <summary>分类名。</summary>
        public string Title;

        /// <summary>分类页左页文字。</summary>
        public string Text;

        /// <summary>分类页左页 XML 内容引索（可选）。</summary>
        public string Left;

        /// <summary>该分类下的子目录（2 个）。</summary>
        public List<BookSubSection> SubSections = [];
    }

    /// <summary>子目录。</summary>
    public class BookSubSection {
        /// <summary>子目录名。</summary>
        public string Title;

        /// <summary>子目录页左页文字。</summary>
        public string Text;

        /// <summary>子目录页左页 XML 内容引索（可选）。</summary>
        public string Left;

        /// <summary>该子目录下的条目。</summary>
        public List<BookEntry> Entries = [];
    }

    /// <summary>子目录条目（实际内容页）。</summary>
    public class BookEntry {
        /// <summary>条目标题。</summary>
        public string Title;

        /// <summary>条目左页兜底文字。</summary>
        public string Text;

        /// <summary>条目左页 XML 内容引索。</summary>
        public string Left;

        /// <summary>条目右页 XML 内容引索（可选，省略则右页显示返回提示）。</summary>
        public string Right;
    }
}