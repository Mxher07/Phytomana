using Engine;

namespace Game {
    public class LoadingCamera : BasePerspectiveCamera {
        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => false;

        public LoadingCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
        }

        public override void Update(float dt) {
            SetupPerspectiveCamera(GameWidget.PlayerData.SpawnPosition, Vector3.UnitX, Vector3.UnitY);
        }
    }
}