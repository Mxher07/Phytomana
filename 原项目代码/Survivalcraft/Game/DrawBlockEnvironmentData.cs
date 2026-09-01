using Engine;
using GameEntitySystem;

namespace Game {
    public class DrawBlockEnvironmentData {
        public DrawBlockMode DrawBlockMode;

        public SubsystemTerrain SubsystemTerrain;

        public Matrix InWorldMatrix;

        public Matrix? ViewProjectionMatrix;

        public Vector3? BillboardDirection;

        public int Humidity;

        public int Temperature;

        public int Light;

        public Entity Owner; //在绘制的时候，可以读取该方块的拥有者。在渲染InventorySlotWidget, ComponentFirstPersonModel, ComponentHumanModel中用到

        public float? EnvironmentTemperature;

        public DrawBlockEnvironmentData() {
            InWorldMatrix = Matrix.Identity;
            Humidity = 15;
            Temperature = 8;
            Light = 15;
        }
    }
}