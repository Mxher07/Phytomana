using System;
using System.Collections.Generic;
using Engine;
using Game;
using Game.Network;
using GameEntitySystem;

namespace Phytomana.Network {
    /// <summary>方块交互动作编码（TableAction 的 ByteA）。</summary>
    public static class PhytoTableAction {
        public const byte FlowerTable = 1;

        public const byte RunesTable = 2;

        public const byte ManaTablet = 3;
    }

    /// <summary>
    /// Phytomana 联机通道门面：注册自定义 ModPacket、发送请求、服务端分派到各子系统处理器。
    /// 依赖联机模组（Survivalcraft.Multiplayer）注入的 NetworkManager 门面；
    /// 单机/主机进程下这些调用均为无害空操作（单机不注册、不发送）。
    /// </summary>
    public static class PhytoNet {
        private static bool s_registered;

        private static double s_lastRegisterAttempt;

        /// <summary>服务端处理客户端法杖动作请求（由 SubsystemGrownStaffBehavior 注册）。</summary>
        public static Action<PhytoModPacket> HandleStaffUseServer;

        /// <summary>客户端收到服务器状态回执并显示（由 SubsystemGrownStaffBehavior 注册）。</summary>
        public static Action<PhytoModPacket> HandleStaffStatusClient;

        /// <summary>服务端处理方块交互请求（花药台/符文台/石板各自 += 注册）。</summary>
        public static Action<PhytoModPacket> HandleTableActionServer;

        /// <summary>客户端收到方块交互状态文本并显示（由 SubsystemFlowerTableBehavior 注册）。</summary>
        public static Action<PhytoModPacket> HandleTableStatusClient;

        public static void AddTableHandler(Action<PhytoModPacket> handler) {
            HandleTableActionServer += handler;
        }

        /// <summary>
        /// 向 PacketManager 注册自定义包。联机层每次会话启动都会清空包工厂表，
        /// 所以低频兜底重注册（每秒尝试一次）以覆盖「先单机后联机」的时序。
        /// </summary>
        public static void EnsureRegistered() {
            if (!(NetworkManager.IsServerRunning || NetworkManager.IsClientRunning)) {
                return;
            }
            double now = Time.RealTime;
            if (s_registered && now - s_lastRegisterAttempt < 1.0) {
                return;
            }
            s_lastRegisterAttempt = now;
            try {
                PacketManager.RegisterPacket(new PhytoModPacket());
                s_registered = true;
            }
            catch {
                // 已注册（本会话残留）或 ID 冲突，忽略。
            }
        }

        public static void HandlePacket(PhytoModPacket packet, bool isServer) {
            switch (packet.Command) {
                case PhytoCommand.StaffUse when isServer:
                    EnsureRegistered();
                    HandleStaffUseServer?.Invoke(packet);
                    break;
                case PhytoCommand.StaffStatusReply when !isServer:
                    HandleStaffStatusClient?.Invoke(packet);
                    break;
                case PhytoCommand.TableAction when isServer:
                    EnsureRegistered();
                    HandleTableActionServer?.Invoke(packet);
                    break;
                case PhytoCommand.TableStatusReply when !isServer:
                    HandleTableStatusClient?.Invoke(packet);
                    break;
            }
        }

        /// <summary>构造一个方块交互请求包（客户端→服务器）。</summary>
        public static PhytoModPacket BuildTableAction(byte action, Point3 point) => new() {
            Command = PhytoCommand.TableAction,
            PlayerIndex = NetworkManager.PlayerIndex,
            ByteA = action,
            HasA = true,
            PointA = point,
        };

        /// <summary>构造一个状态/动作反馈文本包（服务器→发起方）。</summary>
        public static PhytoModPacket BuildTableStatusReply(PhytoModPacket request, string text) => new() {
            Command = PhytoCommand.TableStatusReply,
            PlayerIndex = request.PlayerIndex,
            HasText = true,
            Text = text,
        };

        public static void Send(Packet packet) {
            EnsureRegistered();
            NetworkManager.Queue(packet);
        }

        /// <summary>向某请求包的发起方回派一个状态包。</summary>
        public static void ReplyTo(Packet request, Packet reply) {
            reply.From = request.From;
            reply.To = request.From;
            Send(reply);
        }

        public static ComponentPlayer FindPlayer(int playerIndex) {
            Project project = GameManager.Project;
            if (project == null) {
                return null;
            }
            SubsystemPlayers players = project.FindSubsystem<SubsystemPlayers>(false);
            if (players == null) {
                return null;
            }
            foreach (ComponentPlayer player in players.ComponentPlayers) {
                if (player.PlayerData != null && player.PlayerData.PlayerIndex == playerIndex) {
                    return player;
                }
            }
            return null;
        }

        public static byte PackFlags(bool b0, bool b1, bool b2) {
            byte flags = 0;
            if (b0) {
                flags |= 1;
            }
            if (b1) {
                flags |= 2;
            }
            if (b2) {
                flags |= 4;
            }
            return flags;
        }

        public static bool HasFlag(byte flags, int bit) => (flags & (1 << bit)) != 0;
    }
}