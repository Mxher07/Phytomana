using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class Camera {
        public GameWidget GameWidget { get; set; }

        public VrEye? Eye { get; set; }

        public abstract Vector3 ViewPosition { get; }

        public abstract Vector3 ViewDirection { get; }

        public abstract Vector3 ViewUp { get; }

        public abstract Vector3 ViewRight { get; }

        public abstract Matrix ViewMatrix { get; }

        public abstract Matrix InvertedViewMatrix { get; }

        public abstract Matrix ProjectionMatrix { get; }

        public abstract Matrix ScreenProjectionMatrix { get; }

        public abstract Matrix InvertedProjectionMatrix { get; }

        public abstract Matrix ViewProjectionMatrix { get; }

        public abstract Vector2 ViewportSize { get; }

        public abstract Matrix ViewportMatrix { get; }

        public abstract BoundingFrustum ViewFrustum { get; }

        public abstract bool UsesMovementControls { get; }

        public abstract bool IsEntityControlEnabled { get; }

        public Camera(GameWidget gameWidget) => GameWidget = gameWidget;

        public Vector3 WorldToScreen(Vector3 worldPoint, Matrix worldMatrix) =>
            new Viewport(0, 0, Window.Size.X, Window.Size.Y).Project(worldPoint, ScreenProjectionMatrix, ViewMatrix, worldMatrix);

        public Vector3 ScreenToWorld(Vector3 screenPoint, Matrix worldMatrix) =>
            new Viewport(0, 0, Window.Size.X, Window.Size.Y).Unproject(screenPoint, ScreenProjectionMatrix, ViewMatrix, worldMatrix);

        public virtual void Activate(Camera previousCamera) { }

        public abstract void Update(float dt);
        public virtual void PrepareForDrawing() { }

        public virtual void PrepareForDrawing(VrEye? eye) {
            Eye = eye;
            PrepareForDrawing();
        }
    }
}
