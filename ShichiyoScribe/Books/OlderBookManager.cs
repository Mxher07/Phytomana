using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game {
    /// <summary>
    ///     书籍注册表。
    ///     其他模组可以通过代码调用 <see cref="Register"/> 注册书，
    ///     或提供 .vlb 文件（XML），由 OlderBookLoader 在 BlocksInitalized 时自动扫描注册。
    /// </summary>
    public static class OlderBookManager {
        static int s_sequence;
        /// <summary>所有已注册的书，按 BookId。</summary>
        public static readonly Dictionary<string, BookDefinition> BooksById = [];

        /// <summary>方块索引 → 书。</summary>
        public static readonly Dictionary<int, BookDefinition> BooksByBlockIndex = [];

        public static IEnumerable<BookDefinition> Books => BooksById.Values;

        /// <summary>代码注册一本书。blockName 留空则仅注册目录、不绑任何方块（需由 .vlb 的 Block= 或调用方显式给出方块类名）。</summary>
        public static BookDefinition Register(BookDefinition book) {
            if (book == null) {
                return null;
            }
            if (string.IsNullOrEmpty(book.Id)) {
                book.Id = "book." + (++s_sequence);
            }
            book.Id = book.Id.Trim();
            if (BooksById.TryGetValue(book.Id, out BookDefinition existing)) {
                if (!ReferenceEquals(existing, book)
                    && existing.CodeRegisteredSequence > 0) {
                    OlderBookLog.Warning($"Book id '{book.Id}' is already registered, overwriting.");
                }
                BooksByBlockIndex.Remove(GetBlockIndex(existing.BlockName));
            }
            BooksById[book.Id] = book;
            if (string.IsNullOrWhiteSpace(book.BlockName)) {
                OlderBookLog.Warning($"Book '{book.Id}' has no bound block; it is registered in the directory but cannot be opened from any block.");
                return book;
            }
            int blockIndex = GetBlockIndex(book.BlockName);
            if (blockIndex >= 0) {
                BooksByBlockIndex[blockIndex] = book;
                OlderBookLog.Debug($"Book '{book.Id}' bound to block index {blockIndex} (\"{BlocksManager.Blocks[blockIndex]?.GetType().Name}\").");
            }
            else {
                OlderBookLog.Warning($"Book '{book.Id}' references block '{book.BlockName}' which is not registered, not bound to any block.");
            }
            return book;
        }

        public static bool Unregister(string id) {
            if (!BooksById.Remove(id, out BookDefinition book)) {
                return false;
            }
            BooksByBlockIndex.Remove(GetBlockIndex(book.BlockName));
            return true;
        }

        public static BookDefinition GetBook(string id) => BooksById.GetValueOrDefault(id);

        public static BookDefinition GetBookByBlock(int blockIndex) => BooksByBlockIndex.GetValueOrDefault(blockIndex);

        public static bool IsBookBlock(int blockIndex) => BooksByBlockIndex.ContainsKey(blockIndex);

        public static int GetBlockIndex(string blockName) {
            if (string.IsNullOrEmpty(blockName)) {
                return -1;
            }
            if (int.TryParse(blockName, out int index)) {
                return index;
            }
            return BlocksManager.GetBlockIndex(blockName, false);
        }
    }
}