namespace Game {
    public class HardwareManager {
        public void Vibrate(long ms) {
#if ANDROID
            Engine.Window.Activity.Vibrate(ms);
#endif
        }
    }
}