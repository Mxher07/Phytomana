using Engine.Graphics;

namespace Engine {
    public interface IVrBackend : IDisposable {
        // Lifecycle
        bool IsAvailable { get; }
        void Initialize();
        void StartVr();
        void StopVr();

        // HMD
        bool IsStarted { get; }
        VrControllerType ControllerType { get; }
        Matrix HmdMatrix { get; }
        Matrix HmdMatrixInverted { get; }
        Vector3 HmdMatrixYpr { get; }
        Matrix HmdLastMatrix { get; }
        Matrix HmdLastMatrixInverted { get; }
        Vector3 HmdLastMatrixYpr { get; }
        Vector2 HeadMove { get; }
        Vector2 WalkingVelocity { get; }

        // Frame loop
        bool BeginFrame();
        EyeFrame GetEyeFrame(VrEye eye);
        void ReleaseEye(VrEye eye);
        void EndFrame();
        void EndFrameEmpty();

        // Eye rendering
        Matrix GetEyeToHeadTransform(VrEye eye);
        Matrix GetProjectionMatrix(VrEye eye, float near, float far);
        int SwapchainWidth { get; }
        int SwapchainHeight { get; }

        // Controllers
        bool IsControllerPresent(VrController controller);
        Matrix GetControllerMatrix(VrController controller);
        Vector2 GetStickPosition(VrController controller, float deadZone = 0f);
        Vector2? GetTouchpadPosition(VrController controller, float deadZone = 0f);
        float GetTriggerPosition(VrController controller, float deadZone = 0f);
        bool IsButtonDown(VrController controller, VrControllerButton button);
        bool IsButtonDownOnce(VrController controller, VrControllerButton button);

        // Render target
        RenderTarget2D VrRenderTarget { get; }

        // Per-frame update
        void Update();
    }
}
