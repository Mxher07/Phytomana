using System;
using System.Collections.Generic;
using Engine;

namespace Phytomana.Api {
    /// <summary>
    /// PhytoMana 事件标记接口。所有对外公开事件均实现本接口。
    /// </summary>
    public interface IPhytoEvent { }

    /// <summary>
    /// 轻量事件总线：第三方 Mod 订阅/取消订阅公开事件，框架在对应时机触发。
    /// 处理器内抛出的异常会被捕获并记录日志，不会中断游戏或其他处理器。
    /// </summary>
    public static class PhytoEventBus {
        static readonly Dictionary<Type, List<Delegate>> m_handlers = [];

        public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IPhytoEvent {
            if (handler == null) {
                return;
            }
            if (!m_handlers.TryGetValue(typeof(TEvent), out List<Delegate> list)) {
                list = [];
                m_handlers[typeof(TEvent)] = list;
            }
            list.Add(handler);
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IPhytoEvent {
            if (handler == null) {
                return;
            }
            if (m_handlers.TryGetValue(typeof(TEvent), out List<Delegate> list)) {
                list.Remove(handler);
            }
        }

        /// <summary>
        /// 触发事件。事件对象为引用传递，处理器可修改其可写字段以影响后续逻辑。
        /// </summary>
        public static void Fire<TEvent>(TEvent evt) where TEvent : IPhytoEvent {
            if (!m_handlers.TryGetValue(typeof(TEvent), out List<Delegate> list) || list.Count == 0) {
                return;
            }
            foreach (Delegate handler in list.ToArray()) {
                try {
                    ((Action<TEvent>)handler).Invoke(evt);
                }
                catch (Exception e) {
                    Log.Error($"[PhytoMana]Event handler for {typeof(TEvent).Name} threw: {e}");
                }
            }
        }

        /// <summary>
        /// 清空全部订阅（Mod 卸载时调用）。
        /// </summary>
        public static void Clear() {
            m_handlers.Clear();
        }
    }
}
