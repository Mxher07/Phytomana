using Engine.Graphics;
using Game;
using Game.Network;
using Game.Network.Enums;
using Game.Network.Packages;

namespace Phytomana {
    /// <summary>
    /// 状态/提示文本的统一出口。联机版的原生交互事件会同时在本机（预测）与服务端（权威）执行，
    /// 若两侧都直接显示会双显，且客户端本机魔力/花朵状态是近似值。
    /// 因此：单机（Local）直接本地显示；服务端（Server）把文本经 SmallMessagePackage
    /// 回执给远端玩家所属客户端（数值以服务端为准）；客户端（Client）的本地预测不显示，
    /// 等待服务端回执。服务端不存在对应客户端（主机内置服务器/单机）时回退为直接显示。
    /// </summary>
    public static class PhytoMessages {
        public static void Show(ComponentPlayer player, string text, Color color, bool blinking = false, bool playSound = false) {
            if (player?.PlayerData == null || string.IsNullOrEmpty(text)) {
                return;
            }
            if (CommonLib.WorkType == WorkType.Client) {
                return;
            }
            if (CommonLib.WorkType == WorkType.Server) {
                Client client = player.PlayerData.Client;
                if (client != null) {
                    CommonLib.Net.QueuePackage(new SmallMessagePackage(text, color, blinking, playSound) { To = client });
                    return;
                }
            }
            player.ComponentGui?.DisplaySmallMessage(text, color, blinking, playSound);
        }
    }
}
