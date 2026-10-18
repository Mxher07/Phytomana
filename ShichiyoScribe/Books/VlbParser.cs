using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Engine;

namespace Game {
    /// <summary>
    ///     .vlb 文件解析器。
    ///     .vlb 本质是一份 XML，描述一本书的结构（起始页/目录/分类/子目录/条目），
    ///     实际页内容通过引索指向 Assets/ 下的 .xml（见 <see cref="BookDefinition.ContentRoot"/>）。
    ///     注意：<b>必须显式填写 Block 属性</b>指明绑定哪个方块；缺失时该书不会被绑定到任何方块（仅注册目录）。
    ///     方块名支持 "类名" 或 "类名:+N"（N 为方块数据值）。
    ///     示例结构：
    ///     <code>
    ///     &lt;Book Id="..." Title="..." Block="MyModBookBlock"&gt;
    ///         &lt;StartPages&gt;
    ///             &lt;Spread Title="封面" RightText="..." Right="MyMod/Pages/..." /&gt;
    ///         &lt;/StartPages&gt;
    ///         &lt;Contents Text="目录说明..." /&gt;
    ///         &lt;Section Title="大类一" Text="..."&gt;
    ///             &lt;SubSection Title="子目录一"&gt;
    ///                 &lt;Entry Title="条目" Left="MyMod/Pages/..." Right="..." /&gt;
    ///             &lt;/SubSection&gt;
    ///         &lt;/Section&gt;
    ///     &lt;/Book&gt;
    ///     </code>
    /// </summary>
    public static class VlbParser {
        public const string Extension = ".vlb";

        /// <summary>从流解析 .vlb。失败时返回 null 并写日志。</summary>
        public static BookDefinition Parse(Stream stream, string filename) {
            if (stream == null) {
                return null;
            }
            try {
                return Parse(XElement.Load(stream), filename);
            }
            catch (Exception e) {
                OlderBookLog.Error($"Failed to parse .vlb file '{filename}'", e);
                return null;
            }
        }

        public static BookDefinition Parse(XElement root, string filename) {
            XElement bookNode = root.Name.LocalName == "Book" ? root : root.Element("Book") ?? root;
            string id = (string)bookNode.Attribute("Id") ?? filename;
            string blockName = ((string)bookNode.Attribute("Block"))?.Trim();
            if (string.IsNullOrEmpty(blockName)) {
                OlderBookLog.Warning($"Book '{id}' has no explicit 'Block' attribute; it will NOT be bound to any block.");
            }
            BookDefinition book = new() {
                Id = id.Trim(),
                Title = (string)bookNode.Attribute("Title") ?? (string)bookNode.Attribute("Name") ?? id,
                BlockName = blockName,
                TocText = (string)bookNode.Element("Contents")?.Attribute("Text"),
                TocLeft = Content(bookNode.Element("Contents")?.Attribute("Left"))
            };
            foreach (XElement spreadNode in bookNode.Elements("Spread")) {
                BookSpread spread = new() {
                    Title = (string)spreadNode.Attribute("Title"),
                    LeftText = (string)spreadNode.Attribute("LeftText") ?? (string)spreadNode.Attribute("Text"),
                    RightText = (string)spreadNode.Attribute("RightText"),
                    Left = Content(spreadNode.Attribute("Left")),
                    Right = Content(spreadNode.Attribute("Right"))
                };
                book.StartPages.Add(spread);
            }
            if (book.StartPages.Count == 0) {
                foreach (XElement startNode in bookNode.Elements("StartPage")) {
                    BookSpread spread = new() {
                        Title = (string)startNode.Attribute("Title"),
                        RightText = (string)startNode.Attribute("Text") ?? (string)startNode.Attribute("RightText"),
                        LeftText = (string)startNode.Attribute("LeftText"),
                        Left = Content(startNode.Attribute("Left")),
                        Right = Content(startNode.Attribute("Right"))
                    };
                    book.StartPages.Add(spread);
                }
            }
            foreach (XElement sectionNode in bookNode.Elements("Section")) {
                BookSection section = new() {
                    Title = (string)sectionNode.Attribute("Title") ?? (string)sectionNode.Attribute("Name"),
                    Text = (string)sectionNode.Attribute("Text"),
                    Left = Content(sectionNode.Attribute("Left"))
                };
                foreach (XElement subNode in sectionNode.Elements("SubSection")) {
                    BookSubSection subSection = new() {
                        Title = (string)subNode.Attribute("Title") ?? (string)subNode.Attribute("Name"),
                        Text = (string)subNode.Attribute("Text"),
                        Left = Content(subNode.Attribute("Left"))
                    };
                    foreach (XElement entryNode in subNode.Elements("Entry")) {
                        BookEntry entry = new() {
                            Title = (string)entryNode.Attribute("Title") ?? (string)entryNode.Attribute("Name"),
                            Text = (string)entryNode.Attribute("Text"),
                            Left = Content(entryNode.Attribute("Left")),
                            Right = Content(entryNode.Attribute("Right"))
                        };
                        subSection.Entries.Add(entry);
                    }
                    section.SubSections.Add(subSection);
                }
                if (section.SubSections.Count == 0) {
                    foreach (XElement subNode in sectionNode.Elements("Sub")) {
                        BookSubSection subSection = new() {
                            Title = (string)subNode.Attribute("Title"),
                            Text = (string)subNode.Attribute("Text"),
                            Left = Content(subNode.Attribute("Left"))
                        };
                        foreach (XElement entryNode in subNode.Elements("Entry")) {
                            subSection.Entries.Add(new BookEntry {
                                Title = (string)entryNode.Attribute("Title"),
                                Text = (string)entryNode.Attribute("Text"),
                                Left = Content(entryNode.Attribute("Left")),
                                Right = Content(entryNode.Attribute("Right"))
                            });
                        }
                        section.SubSections.Add(subSection);
                    }
                }
                book.Sections.Add(section);
            }
            OlderBookLog.Debug(
                $"Parsed .vlb '{book.Id}': {book.StartPages.Count} start spread(s), {book.Sections.Count} section(s), "
                + $"{book.Sections.Sum(s => s.SubSections.Count)} subsection(s), "
                + $"{book.Sections.Sum(s => s.SubSections.Sum(ss => ss.Entries.Count))} entr(y/ies)."
            );
            return book;
        }

        static string Content(XAttribute attribute) {
            if (attribute == null) {
                return null;
            }
            string value = ((string)attribute)?.Trim();
            return BookDefinition.NormalizeContentPath(value);
        }
    }
}