using System.Linq;
using Engine;
using Engine.Graphics;
using Game.Network;
using Game.Network.Enums;

namespace Game {
    /// <summary>
    ///     书的子系统行为。只挂在"注册过的书方块索引"（<see cref="HandledBlocks"/>，由 .vlb 的 Block= 属性或代码 Register 决定）上：
    ///     - OnUse：手持书时"对着任意地方使用"即可翻开（GettingPriorityUse 由反射自动判定）。
    ///     - OnInteract：点击已放置的书方块时翻开。
    ///     订阅以外的方块永远不会弹书（非书方块不会误开界面）。
    ///     联机：书界面是纯本机界面，约定"每个人只能看到自己的页面" ——
    ///     只在书归属者自己的客户端弹界面（客户端限主玩家；服务器限主机内置玩家），
    ///     远端玩家在专用服务器上不弹界面，其客户端本地预测自行弹自己的书。
    /// </summary>
    public class SubsystemOlderBook : SubsystemBlockBehavior {
        public override int[] HandledBlocks => OlderBookManager.BooksByBlockIndex.Keys.ToArray();

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) => OpenBook(componentMiner);

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) => OpenBook(componentMiner);

        public override bool OnInteract(MovingBlocksRaycastResult raycastResult, ComponentMiner componentMiner) => OpenBook(componentMiner);

        bool OpenBook(ComponentMiner componentMiner) {
            ComponentPlayer player = componentMiner?.ComponentPlayer;
            if (player == null) {
                OlderBookLog.Debug("OpenBook skipped: player is null.");
                return false;
            }
            int blockIndex = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
            BookDefinition book = OlderBookManager.GetBookByBlock(blockIndex);
            if (book == null) {
                OlderBookLog.Debug($"OpenBook skipped: no book bound to held block index {blockIndex}.");
                return false;
            }
            // 只在"这个玩家自己的界面侧"弹书：单机直接弹；客户端仅主玩家；主机内置服务器仅主机玩家。
            bool isOwnUiSide = CommonLib.WorkType == WorkType.Local
                || (CommonLib.WorkType == WorkType.Client && CommonLib.MainPlayer == player)
                || (CommonLib.WorkType == WorkType.Server && player.PlayerData != null && player.PlayerData.IsMainPlayer);
            if (isOwnUiSide) {
                player.ComponentGui.ModalPanelWidget = new BookWidget(book, player);
                AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                OlderBookLog.Debug($"Opened book '{book.Id}' (\"{book.Title}\") for player.");
            }
            // 服务器侧对远端玩家同样消耗本次交互（与原生箱子行为一致），避免同一次点击被继续派发。
            return true;
        }
    }
}
