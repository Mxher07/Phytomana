using Engine;

namespace Game {
    public class FppCamera : BasePerspectiveCamera {
        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => true;

        public FppCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
        }

        public override void Update(float dt) {
            if (GameWidget.Target == null) return;

            if (!Eye.HasValue) {
                Matrix matrix = Matrix.CreateFromQuaternion(GameWidget.Target.ComponentCreatureModel.EyeRotation);
                matrix.Translation = GameWidget.Target.ComponentCreatureModel.EyePosition;
                SetupPerspectiveCamera(matrix.Translation, matrix.Forward, matrix.Up);
                return;
            }

            Vector3 translation = VrManager.HmdMatrix.Translation;
            Vector3 position = GameWidget.Target.ComponentBody.Position;
            float crouchScale = 1f - 0.5f * GameWidget.Target.ComponentBody.CrouchFactor;
            float maxHeight = GameWidget.Target.ComponentBody.BoxSize.Y * crouchScale;
            float num = position.Y + MathUtils.Clamp(translation.Y + SettingsManager.VrEyeHeightOffset, 0.2f, maxHeight - 0.1f);
            Vector3 hmdMatrixYpr = VrManager.HmdMatrixYpr;
            Vector3 vector = GameWidget.Target.ComponentCreatureModel.EyeRotation.ToYawPitchRoll();
            float num2 = vector.X - hmdMatrixYpr.X;
            Matrix identity = Matrix.Identity;
            identity.Translation = new Vector3(position.X, num, position.Z);
            identity.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(num2);
            SetupPerspectiveCamera(identity.Translation, identity.Forward, identity.Up);
        }
    }
}
