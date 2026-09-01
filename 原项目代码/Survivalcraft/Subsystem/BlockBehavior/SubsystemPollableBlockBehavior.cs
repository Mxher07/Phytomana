namespace Game {
    public abstract class SubsystemPollableBlockBehavior : SubsystemBlockBehavior {
        public abstract void OnPoll(int value, int x, int y, int z, int pollPass);
    }
}