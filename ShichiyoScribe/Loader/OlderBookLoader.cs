using System.Collections.Generic;
using Engine;

namespace Game {
    /// <summary>
    ///     OlderBook 的主要 ModLoader。
    ///     在 <see cref="BlocksInitalized"/>（所有方块完成初始化之后）扫描所有已加载模组中的 .vlb 文件并注册。
    ///     一本书必须通过 .vlb 的 Block="方块类名" 属性（或其它模组代码调用 <see cref="OlderBookManager.Register"/>）
    ///     显式声明绑定方块；本模组不再内置任何示例书。
    /// </summary>
    public class OlderBookLoader : ModLoader {
        public override int Priority => -1000;

        /// <summary>
        ///     关键步：C# ModLoader 必须显式把自己挂到 Hook 上，否则
        ///     <see cref="BlocksInitalized"/> 永远不会被调用（书籍注册也就不会执行）。
        /// </summary>
        public override void __ModInitialize() {
            ModsManager.RegisterHook("BlocksInitalized", this);
        }

        public override void BlocksInitalized() {
            try {
                ScanVlbBooks();
            }
            catch (System.Exception e) {
                OlderBookLog.Error("OlderBookLoader.BlocksInitalized failed", e);
            }
            int count = OlderBookManager.BooksById.Count;
            int blocks = OlderBookManager.BooksByBlockIndex.Count;
            OlderBookLog.Info($"Registered {count} book(s) bound to {blocks} block(s).");
        }

        /// <summary>扫描所有已加载模组，注册其中的 .vlb 书。</summary>
        public static void ScanVlbBooks() {
            OlderBookLog.Debug("Scanning all loaded mods for '.vlb' files...");
            foreach (ModEntity modEntity in ModsManager.ModList) {
                try {
                    modEntity.GetFiles(
                        VlbParser.Extension,
                        (filename, stream) => {
                            BookDefinition book = VlbParser.Parse(stream, filename);
                            if (book != null) {
                                OlderBookManager.Register(book);
                                OlderBookLog.Debug($"Registered book '{book.Id}' (title \"{book.Title}\", block '{book.BlockName}') from '{modEntity.modInfo?.Name ?? "?"}' ({filename}).");
                            }
                        }
                    );
                }
                catch (System.Exception e) {
                    OlderBookLog.Error($"Failed scanning .vlb in mod [{modEntity.modInfo?.Name}]", e);
                }
            }
        }
    }
}