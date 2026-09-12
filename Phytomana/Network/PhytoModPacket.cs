using Engine;
using Game;
using Game.Network;

namespace Phytomana.Network {
    public enum PhytoCommand : byte {
        /// <summary>玩家向服务器发起法杖动作请求（客户端→服务器）。</summary>
        StaffUse = 1,
        /// <summary>服务器回写法杖状态查询结果（服务器→发起方）。</summary>
        StaffStatusReply = 2,
        /// <summary>玩家向服务器发起方块交互请求（花药台/符文台/石板，客户端→服务器）。</summary>
        TableAction = 3,
        /// <summary>服务器回写方块交互/状态文本（服务器→发起方）。</summary>
        TableStatusReply = 4,
    }

    /// <summary>
    /// Phytomana 联机包。复用联机模组预留的 ModPacket（ID=118）动态注册位，
    /// 装在模组自身程序集内，服务器在 Handle(isServer=true) 里落权威状态，
    /// 客户端 Handle(isServer=false) 只做显示应用。
    /// </summary>
    public class PhytoModPacket : Packet {
        public PhytoCommand Command;

        public int PlayerIndex;

        public bool BoolA;

        public byte ByteA;

        public byte ByteB;

        public float FloatA;

        public float FloatB;

        public float FloatC;

        public float FloatD;

        public bool HasA;

        public Point3 PointA;

        public bool HasV1;

        public Vector3 V1;

        public bool HasV2;

        public Vector3 V2;

        public bool HasText;

        public string Text;

        public override byte ID => (byte)PacketType.ModPacket;

        public override NetworkState MinNeedState => NetworkState.Playing;

        public override void Serialize(PacketSerializer archive) {
            archive.Enum(ref Command);
            archive.Value(ref PlayerIndex);
            archive.Value(ref BoolA);
            archive.Value(ref ByteA);
            archive.Value(ref ByteB);
            archive.Value(ref FloatA);
            archive.Value(ref FloatB);
            archive.Value(ref FloatC);
            archive.Value(ref FloatD);
            archive.Value(ref HasA);
            if (HasA) {
                archive.Value(ref PointA);
            }
            archive.Value(ref HasV1);
            if (HasV1) {
                archive.Value(ref V1);
            }
            archive.Value(ref HasV2);
            if (HasV2) {
                archive.Value(ref V2);
            }
            archive.Value(ref HasText);
            if (HasText) {
                archive.Value(ref Text);
            }
        }

        public override void Handle(bool isServer) => PhytoNet.HandlePacket(this, isServer);
    }
}