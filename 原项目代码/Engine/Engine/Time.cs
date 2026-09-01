using System.Diagnostics;

namespace Engine {
    public static class Time {
        struct DelayedExecutionRequest {
            public double Time;

            // ReSharper disable MemberHidesStaticFromOuterClass
            public int FrameIndex;
            // ReSharper restore MemberHidesStaticFromOuterClass

            public Action Action;
        }

        static long? m_startTicks;

        static List<DelayedExecutionRequest> m_delayedExecutionsRequests = [];

        static int m_fpsStartFrameIndex;

        static double m_fpsStartTime;

        static float m_fpsCpuTime;

        static float m_remainingDuration;
        public static int FrameIndex { get; private set; }

        public static long Ticks {
            get {
                long timestamp = Stopwatch.GetTimestamp();
                m_startTicks ??= timestamp;
                return timestamp - m_startTicks.Value;
            }
        }

        public static long TicksPerSecond => Stopwatch.Frequency;

        public static double RealTime => Ticks / (double)TicksPerSecond;

        public static double PreviousFrameStartTime { get; private set; }

        public static double FrameStartTime { get; private set; }

        public static float PreviousFrameDuration { get; private set; }

        public static float FrameDuration { get; private set; }

        public static float CpuFrameDuration { get; private set; }

        public static float AverageFrameDuration { get; private set; }

        public static float AverageCpuFrameDuration { get; private set; }

        public static float FrameDurationLimit { get; set; }

        public static bool SingleEvent(double time) {
            if (FrameStartTime >= time) {
                return PreviousFrameStartTime < time;
            }
            return false;
        }

        public static bool PeriodicEvent(double period, double offset) {
            double num = FrameStartTime - offset;
            double num2 = Math.Floor(num / period) * period;
            return num >= num2 && num - FrameDuration < num2;
        }

        public static void QueueTimeDelayedExecution(double time, Action action) {
            m_delayedExecutionsRequests.Add(new DelayedExecutionRequest { Time = time, FrameIndex = -1, Action = action });
        }

        public static void QueueFrameIndexDelayedExecution(int frameIndex, Action action) {
            m_delayedExecutionsRequests.Add(new DelayedExecutionRequest { Time = -1.0, FrameIndex = frameIndex, Action = action });
        }

        internal static void BeforeFrame() {
            double realTime = RealTime;
            PreviousFrameDuration = FrameDuration;
            FrameDuration = (float)(realTime - FrameStartTime);
            PreviousFrameStartTime = FrameStartTime;
            FrameStartTime = realTime;
            if (FrameStartTime >= m_fpsStartTime + 1.0) {
                int num = FrameIndex - m_fpsStartFrameIndex;
                AverageFrameDuration = (float)(FrameStartTime - m_fpsStartTime) / num;
                AverageCpuFrameDuration = m_fpsCpuTime / num;
                m_fpsStartTime = FrameStartTime;
                m_fpsCpuTime = 0f;
                m_fpsStartFrameIndex = FrameIndex;
            }
            int num2 = 0;
            while (num2 < m_delayedExecutionsRequests.Count) {
                DelayedExecutionRequest delayedExecutionRequest = m_delayedExecutionsRequests[num2];
                if ((delayedExecutionRequest.Time >= 0.0 && FrameStartTime >= delayedExecutionRequest.Time)
                    || (delayedExecutionRequest.FrameIndex >= 0 && FrameIndex >= delayedExecutionRequest.FrameIndex)) {
                    m_delayedExecutionsRequests.RemoveAt(num2);
                    delayedExecutionRequest.Action();
                }
                else {
                    num2++;
                }
            }
        }

        internal static void AfterFrame() {
            CpuFrameDuration = (float)(RealTime - FrameStartTime);
            m_fpsCpuTime += CpuFrameDuration;
            FrameIndex++;
            if (FrameDurationLimit > 0f) {
                float num = Math.Clamp(FrameDurationLimit, 0f, 1f);
                m_remainingDuration = Math.Clamp(m_remainingDuration + num - FrameDuration, 0f, 2f * num);
                float num2 = (float)(RealTime - FrameStartTime);
                Sleep(m_remainingDuration - num2);
            }
        }

        static void Sleep(double duration) {
            Task.Delay((int)Math.Clamp(Math.Round(duration * 1000.0), 0.0, 2147483647.0)).Wait();
        }
    }
}