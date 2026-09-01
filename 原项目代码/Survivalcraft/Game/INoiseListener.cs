using Engine;

namespace Game {
    public interface INoiseListener {
        void HearNoise(ComponentBody sourceBody, Vector3 sourcePosition, float loudness);
    }
}