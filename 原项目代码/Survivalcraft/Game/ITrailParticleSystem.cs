using Engine;

namespace Game {
    public interface ITrailParticleSystem {
        Vector3 Position { get; set; }

        bool IsStopped { get; set; }
    }
}