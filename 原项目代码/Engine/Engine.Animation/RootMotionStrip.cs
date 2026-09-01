using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 根骨骼位移剥离静态原语：从 SourceBone 走到模型根骨骼，每骨 translation := rest。
    /// </summary>
    public static class RootMotionStrip {
        /// <summary>
        /// 从 SourceBone 走到模型根骨骼，每骨 translation := rest（保留 rotation/scale）。
        /// NeedsStrip=false 直接返回。骨骼缺失回退 model.RootBone；cur==null 跳过（无 NPE）。幂等。
        /// </summary>
        public static void StripRootTranslation(Matrix?[] boneTransforms, Model model, RootStripInfo info) {
            if (!info.NeedsStrip || boneTransforms == null || model == null) {
                return;
            }
            ModelBone found = !string.IsNullOrEmpty(info.SourceBone)
                ? model.FindBone(info.SourceBone, throwIfNotFound: false)
                : null;
            ModelBone cur = found ?? model.RootBone;
            while (cur != null) {
                if (boneTransforms[cur.Index].HasValue) {
                    Matrix t = boneTransforms[cur.Index].Value;
                    t.Decompose(out _, out Quaternion rot, out _);
                    Vector3 restTrans = cur.Transform.Translation;
                    boneTransforms[cur.Index] = Matrix.CreateFromQuaternion(rot) * Matrix.CreateTranslation(restTrans);
                }
                if (cur == model.RootBone) {
                    break;
                }
                cur = cur.ParentBone;
            }
        }
    }
}
