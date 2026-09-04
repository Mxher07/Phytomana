using System;

namespace Phytomana.Api {
    /// <summary>
    /// 独立魔力容器。全部魔力数据必须经由本类存取，禁止在方块/节点内另写魔力字段。
    /// </summary>
    public class ManaStorage {
        public float Max { get; }

        public float Current { get; private set; }

        public float Free => Max - Current;

        public bool IsEmpty => Current <= 0f;

        public bool IsFull => Current >= Max;

        public ManaStorage(float max) {
            Max = Math.Max(0f, max);
        }

        /// <summary>
        /// 尝试注入魔力，返回实际注入量；超出容量的溢出部分被丢弃（overflow = amount - 返回值）。
        /// </summary>
        public float TryAdd(float amount) {
            if (amount <= 0f || IsFull) {
                return 0f;
            }
            float accepted = Math.Min(amount, Free);
            Current += accepted;
            return accepted;
        }

        /// <summary>
        /// 尝试抽取魔力，返回实际抽取量；存量不足时只抽取现有量。
        /// </summary>
        public float Take(float amount) {
            if (amount <= 0f || IsEmpty) {
                return 0f;
            }
            float taken = Math.Min(amount, Current);
            Current -= taken;
            return taken;
        }

        public void SetCurrent(float amount) {
            Current = Math.Clamp(amount, 0f, Max);
        }

        public float SaveData() => Current;

        public void LoadData(float value) {
            SetCurrent(value);
        }
    }
}
