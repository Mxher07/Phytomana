namespace Game {
    public abstract class ParticleSystemBase {
        public SubsystemParticles SubsystemParticles;

        public abstract void Draw(Camera camera);

        public abstract bool Simulate(float dt);

        public virtual void OnAdded() { }

        public virtual void OnRemoved() { }
    }
}