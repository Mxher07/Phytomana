using System.Diagnostics;

namespace Game {
    public class RunningAverage {
        public long m_startTicks;

        public long m_period;

        public float m_sumValues;

        public int m_countValues;

        public float m_value;

        public float Value => m_value;

        public RunningAverage(float period) => m_period = (long)(period * Stopwatch.Frequency);

        public void AddSample(float sample) {
            m_sumValues += sample;
            m_countValues++;
            long timestamp = Stopwatch.GetTimestamp();
            if (timestamp >= m_startTicks + m_period) {
                m_value = m_sumValues / m_countValues;
                m_sumValues = 0f;
                m_countValues = 0;
                m_startTicks = timestamp;
            }
        }
    }
}