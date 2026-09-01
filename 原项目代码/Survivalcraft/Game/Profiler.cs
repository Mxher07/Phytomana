using System.Diagnostics;
using System.Text;
using Engine;

namespace Game {
    public struct Profiler : IDisposable {
        public class Metric {
            public string Name;

            public int HitCount;

            public long TotalTicks;

            public long MaxTicks;

            public readonly RunningAverage AverageHitCount = new(5f);

            public readonly RunningAverage AverageTime = new(5f);
        }

        public static Dictionary<string, Metric> m_metrics = [];

        public static List<Metric> m_sortedMetrics = [];

        public static int m_maxNameLength;

        public static bool m_sortNeeded;

        public long m_startTicks;

        public Metric m_metric;

        public static bool Enabled = true;

        public static int MaxNameLength => m_maxNameLength;

        public static ReadOnlyList<Metric> Metrics {
            get {
                if (m_sortNeeded) {
                    m_sortedMetrics.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));
                    m_sortNeeded = false;
                }
                return new ReadOnlyList<Metric>(m_sortedMetrics);
            }
        }

        public Profiler(string name) {
            if (Enabled) {
                if (!m_metrics.TryGetValue(name, out m_metric)) {
                    m_metric = new Metric { Name = name };
                    m_maxNameLength = MathUtils.Max(m_maxNameLength, name.Length);
                    m_metrics.Add(name, m_metric);
                    m_sortedMetrics.Add(m_metric);
                    m_sortNeeded = true;
                }
                m_startTicks = Stopwatch.GetTimestamp();
            }
            else {
                m_startTicks = 0L;
                m_metric = null;
            }
        }

        public void Dispose() {
            if (m_metric != null) {
                long num = Stopwatch.GetTimestamp() - m_startTicks;
                m_metric.TotalTicks += num;
                m_metric.MaxTicks = Math.Max(m_metric.MaxTicks, num);
                m_metric.HitCount++;
                m_metric = null;
                return;
            }
            throw new InvalidOperationException("Profiler.Dispose called without a matching constructor.");
        }

        public static void Sample() {
            foreach (Metric metric in Metrics) {
                float sample = metric.TotalTicks / (float)Stopwatch.Frequency;
                metric.AverageHitCount.AddSample(metric.HitCount);
                metric.AverageTime.AddSample(sample);
                metric.HitCount = 0;
                metric.TotalTicks = 0L;
                metric.MaxTicks = 0L;
            }
        }

        public static void ReportAverage(Metric metric, StringBuilder text) {
            int num = m_maxNameLength + 2;
            int length = text.Length;
            text.Append(metric.Name);
            text.Append('.', Math.Max(1, num - text.Length + length));
            text.AppendNumber(metric.AverageHitCount.Value, 2);
            text.Append("x");
            text.Append('.', Math.Max(1, num + 9 - text.Length + length));
            FormatTimeSimple(text, metric.AverageTime.Value);
        }

        public static void ReportFrame(Metric metric, StringBuilder text) {
            int num = m_maxNameLength + 2;
            int length = text.Length;
            text.Append(metric.Name);
            text.Append('.', Math.Max(1, num - text.Length + length));
            FormatTimeSimple(text, metric.TotalTicks / (float)Stopwatch.Frequency);
        }

        public static void ReportAverage(StringBuilder text) {
            foreach (Metric metric in Metrics) {
                ReportAverage(metric, text);
                text.Append("\n");
            }
        }

        public static void ReportFrame(StringBuilder text) {
            foreach (Metric metric in Metrics) {
                ReportFrame(metric, text);
                text.Append("\n");
            }
        }

        public static void FormatTimeSimple(StringBuilder text, float time) {
            text.AppendNumber(time * 1000f, 3);
            text.Append("ms");
        }

        public static void FormatTime(StringBuilder text, float time) {
            if (time >= 1f) {
                text.AppendNumber(time, 2);
                text.Append("s");
            }
            else if (time >= 0.1f) {
                text.AppendNumber(time * 1000f, 0);
                text.Append("ms");
            }
            else if (time >= 0.01f) {
                text.AppendNumber(time * 1000f, 1);
                text.Append("ms");
            }
            else if (time >= 0.001f) {
                text.AppendNumber(time * 1000f, 2);
                text.Append("ms");
            }
            else if (time >= 0.0001f) {
                text.AppendNumber(time * 1000000f, 0);
                text.Append("us");
            }
            else if (time >= 1E-05f) {
                text.AppendNumber(time * 1000000f, 1);
                text.Append("us");
            }
            else if (time >= 1E-06f) {
                text.AppendNumber(time * 1000000f, 2);
                text.Append("us");
            }
            else if (time >= 1E-07f) {
                text.AppendNumber(time * 1E+09f, 0);
                text.Append("ns");
            }
            else if (time >= 1E-08f) {
                text.AppendNumber(time * 1E+09f, 1);
                text.Append("ns");
            }
            else {
                text.AppendNumber(time * 1E+09f, 2);
                text.Append("ns");
            }
        }
    }
}