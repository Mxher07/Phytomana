using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSimpleModel : ComponentModel {
        public SubsystemGameInfo m_subsystemGameInfo;

        public ComponentSpawn m_componentSpawn;

        public override void Animate() {
            base.Animate();

            // glTF 模型（有动画或有蒙皮）需要将实体变换应用到根骨骼
            // DAE 模型通过 SetBoneTransform 处理，保持原有行为
            bool isGltfModel = Model.HasSkin || Model.HasAnimations;

            if (Animated && isGltfModel) {
                // 获取实体的位置和旋转
                Vector3 entityPosition = m_componentFrame.Position;
                Quaternion entityRotation = m_componentFrame.Rotation;
                Matrix entityTransform = Matrix.CreateFromQuaternion(entityRotation) * Matrix.CreateTranslation(entityPosition);

                // 获取根骨骼变换（可能是动画采样的或原始的）
                Matrix rootTransform;
                if (m_boneTransforms[Model.RootBone.Index].HasValue) {
                    rootTransform = m_boneTransforms[Model.RootBone.Index].Value;
                } else {
                    rootTransform = Model.RootBone.Transform;
                }

                // 应用根骨骼变换修正（旋转 + 平移，已混合）
                // 顺序 R*T（行向量约定=先 R 后 T）：先绕骨骼原点旋转模型，再平移。
                // 平移在实体轴（不被 R 旋转），即 [0,1,0] 恒指实体上方。
                if (AnimationController != null) {
                    Matrix correction = Matrix.CreateFromQuaternion(AnimationController.EffectiveRootRotation)
                                      * Matrix.CreateTranslation(AnimationController.EffectiveRootTranslation);
                    rootTransform = correction * rootTransform;
                }

                // 叠加实体变换
                m_boneTransforms[Model.RootBone.Index] = rootTransform * entityTransform;
                return;
            }

            // DAE 模型或无动画的 glTF 模型
            if (m_componentSpawn != null) {
                Opacity = m_componentSpawn.SpawnDuration > 0f
                    ? (float)MathUtils.Saturate(
                        (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.SpawnTime) / m_componentSpawn.SpawnDuration
                    )
                    : 1f;
                if (m_componentSpawn.DespawnTime.HasValue) {
                    Opacity = MathUtils.Min(
                        Opacity.Value,
                        (float)MathUtils.Saturate(
                            1.0 - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.DespawnTime.Value) / m_componentSpawn.DespawnDuration
                        )
                    );
                }
            }
            SetBoneTransform(Model.RootBone.Index, m_componentFrame.Matrix);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentSpawn = Entity.FindComponent<ComponentSpawn>();
            base.Load(valuesDictionary, idToEntityMap);
        }
    }
}