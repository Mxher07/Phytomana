using Engine;
using TemplatesDatabase;

namespace Game {
    public struct PlayerInput {
        public PlayerInput() { }
        public Vector2 Look;

        public Vector3 Move;

        public Vector3 CrouchMove;

        public Vector3? VrMove;

        public Vector2? VrLook;

        public Vector2 CameraLook;

        public Vector3 CameraMove;

        public Vector3 CameraCrouchMove;

        public bool ToggleCreativeFly;

        public bool ToggleCrouch;

        public bool ToggleMount;

        public bool EditItem;

        public bool Jump;

        public int ScrollInventory;

        public bool ToggleInventory;

        public bool ToggleClothing;

        public bool TakeScreenshot;

        public bool SwitchCameraMode;

        public bool TimeOfDay;

        public bool Lighting;

        public bool Precipitation;

        public bool Fog;

        public bool KeyboardHelp;

        public bool GamepadHelp;

        public Ray3? Dig;

        public Ray3? Hit;

        public Ray3? Aim;

        public Ray3? Interact;

        public Ray3? PickBlockType;

        public bool Drop;

        public int? SelectInventorySlot;

        /// <summary>
        ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
        /// </summary>
        public ValuesDictionary ValuesDictionaryForMods = new();
    }
}