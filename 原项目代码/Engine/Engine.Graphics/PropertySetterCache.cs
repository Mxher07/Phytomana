#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Engine.Graphics
{
    /// <summary>
    /// 属性设置器缓存
    /// 使用表达式树编译属性设置器委托，避免反射调用开销
    /// </summary>
    public static class PropertySetterCache
    {
        /// <summary>
        /// 缓存的属性设置器委托
        /// Key: (目标类型, 属性名)
        /// Value: 设置器委托 (object target, object value) => void
        /// </summary>
        private static readonly ConcurrentDictionary<(Type, string), Action<object, object>> s_setters
            = new ConcurrentDictionary<(Type, string), Action<object, object>>();

        /// <summary>
        /// 缓存的对象创建委托
        /// Key: 类型
        /// Value: 创建委托 () => object
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<object>> s_creators
            = new ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns>是否设置成功</returns>
        public static bool SetProperty(object target, string propertyName, object value)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return false;

            var type = target.GetType();
            var key = (type, propertyName);

            var setter = s_setters.GetOrAdd(key, k => CompileSetter(k.Item1, k.Item2));
            if (setter == null)
                return false;

            try
            {
                setter(target, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 批量设置属性值
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="properties">属性名和值的键值对</param>
        /// <returns>成功设置的属性数量</returns>
        public static int SetProperties(object target, Dictionary<string, object> properties)
        {
            if (target == null || properties == null)
                return 0;

            int count = 0;
            foreach (var kvp in properties)
            {
                if (SetProperty(target, kvp.Key, kvp.Value))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 创建对象并设置属性
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="properties">属性名和值的键值对</param>
        /// <returns>创建的对象实例</returns>
        public static object CreateAndSetProperties(Type type, Dictionary<string, object> properties)
        {
            if (type == null)
                return null;

            var creator = s_creators.GetOrAdd(type, CompileCreator);
            if (creator == null)
                return null;

            var obj = creator();
            if (obj == null)
                return null;

            if (properties != null)
            {
                SetProperties(obj, properties);
            }

            return obj;
        }

        /// <summary>
        /// 获取或编译属性设置器
        /// </summary>
        public static Action<object, object> GetOrCreateSetter(Type type, string propertyName)
        {
            if (type == null || string.IsNullOrEmpty(propertyName))
                return null;

            var key = (type, propertyName);
            return s_setters.GetOrAdd(key, k => CompileSetter(k.Item1, k.Item2));
        }

        /// <summary>
        /// 清除缓存（主要用于测试）
        /// </summary>
        public static void Clear()
        {
            s_setters.Clear();
            s_creators.Clear();
        }

        /// <summary>
        /// 编译属性设置器委托
        /// </summary>
        private static Action<object, object> CompileSetter(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return null;

            // 使用完整命名空间避免与 NCalc.Expression 冲突
            var targetParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "target");
            var valueParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");

            // (T target) => target
            var castTarget = System.Linq.Expressions.Expression.Convert(targetParam, type);

            // (object value) => (PropertyType)value
            var castValue = System.Linq.Expressions.Expression.Convert(valueParam, property.PropertyType);

            // target.Property = value
            var assign = System.Linq.Expressions.Expression.Call(castTarget, property.GetSetMethod(), castValue);

            var lambda = System.Linq.Expressions.Expression.Lambda<Action<object, object>>(assign, targetParam, valueParam);
            return lambda.Compile();
        }

        /// <summary>
        /// 编译对象创建委托
        /// </summary>
        private static Func<object> CompileCreator(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                return null;

            var newExpr = System.Linq.Expressions.Expression.New(constructor);
            var castExpr = System.Linq.Expressions.Expression.Convert(newExpr, typeof(object));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object>>(castExpr);
            return lambda.Compile();
        }
    }
}
