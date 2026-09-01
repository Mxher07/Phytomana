using System.Text.Json;
using System.Text.Json.Nodes;

namespace Engine.Animation {
    /// <summary>
    /// JSON 继承辅助类
    /// 提供 JsonNode 的继承解析和合并功能
    /// </summary>
    public static class JsonInheritanceHelper {
        /// <summary>
        /// 解析继承链并返回合并后的 JsonNode
        /// </summary>
        /// <param name="jsonNode">当前 JSON 节点</param>
        /// <param name="parentLoader">
        /// 父配置加载回调，接收相对路径，返回父配置的 JsonNode。
        /// 如果父配置不存在，应返回 null。
        /// </param>
        /// <returns>合并后的 JsonNode</returns>
        /// <exception cref="JsonInheritanceException">
        /// 循环继承或父配置不存在
        /// </exception>
        public static JsonNode ResolveInheritance(JsonNode jsonNode, Func<string, JsonNode> parentLoader) {
            if (jsonNode == null) {
                return null;
            }

            // 检查是否有 extends 属性
            if (jsonNode is JsonObject obj
                && obj.TryGetPropertyValue("extends", out JsonNode extendsNode)
                && extendsNode != null) {
                string extendsPath = extendsNode.GetValue<string>();
                if (string.IsNullOrEmpty(extendsPath)) {
                    // extends 为空，移除属性并返回
                    obj.Remove("extends");
                    return jsonNode;
                }

                // 检测循环继承
                HashSet<string> inheritanceChain = new();
                inheritanceChain.Add(extendsPath);

                // 递归解析并合并
                return ResolveInheritanceInternal(jsonNode, parentLoader, inheritanceChain, extendsPath);
            }
            return jsonNode;
        }

        static JsonNode ResolveInheritanceInternal(JsonNode currentNode,
            Func<string, JsonNode> parentLoader,
            HashSet<string> inheritanceChain,
            string currentExtendsPath) {
            // 加载父配置
            JsonNode parentNode = parentLoader(currentExtendsPath);
            if (parentNode == null) {
                string chain = string.Join(" -> ", inheritanceChain);
                throw new JsonInheritanceException($"Parent config not found: {currentExtendsPath}", chain);
            }

            // 检查父配置是否也有继承
            if (parentNode is JsonObject parentObj
                && parentObj.TryGetPropertyValue("extends", out JsonNode parentExtendsNode)
                && parentExtendsNode != null) {
                string parentExtendsPath = parentExtendsNode.GetValue<string>();
                if (!string.IsNullOrEmpty(parentExtendsPath)) {
                    // 检测循环继承
                    if (inheritanceChain.Contains(parentExtendsPath)) {
                        inheritanceChain.Add(parentExtendsPath);
                        string chain = string.Join(" -> ", inheritanceChain);
                        throw new JsonInheritanceException($"Circular inheritance detected in chain: {chain}", chain);
                    }
                    inheritanceChain.Add(parentExtendsPath);

                    // 先递归解析父配置的继承
                    parentNode = ResolveInheritanceInternal(parentNode, parentLoader, inheritanceChain, parentExtendsPath);
                }
            }

            // 移除当前节点的 extends 属性
            if (currentNode is JsonObject currentObj) {
                currentObj.Remove("extends");
            }

            // 移除父节点的 extends 属性（如果还有的话）
            if (parentNode is JsonObject pObj) {
                pObj.Remove("extends");
            }

            // 合并：父配置作为 base，当前配置覆盖
            JsonNode result = parentNode.DeepClone();
            MergeInto(result, currentNode);
            return result;
        }

        /// <summary>
        /// 合并两个 JsonNode（深度合并）
        /// source 的值覆盖 target 的同名属性
        /// </summary>
        /// <param name="target">目标节点（会被修改）</param>
        /// <param name="source">源节点</param>
        public static void MergeInto(JsonNode target, JsonNode source) {
            if (target == null
                || source == null) {
                return;
            }

            // 特殊情况：当 target 是数组，source 是包含数组操作符的对象时，调用 MergeArray
            if (target.GetValueKind() == JsonValueKind.Array
                && source.GetValueKind() == JsonValueKind.Object) {
                JsonObject sourceObj = source.AsObject();
                bool hasArrayOperators = sourceObj.ContainsKey("$replace")
                    || sourceObj.ContainsKey("$remove")
                    || sourceObj.ContainsKey("$prepend")
                    || sourceObj.ContainsKey("$append");
                if (hasArrayOperators) {
                    MergeArray(target, source);
                    return;
                }
            }
            switch (source.GetValueKind()) {
                case JsonValueKind.Object: MergeObject(target, source); break;
                case JsonValueKind.Array: MergeArray(target, source); break;
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null: ReplaceNode(target, source.DeepClone()); break;
            }
        }

        static void MergeObject(JsonNode target, JsonNode source) {
            if (target.GetValueKind() != JsonValueKind.Object) {
                // 目标不是对象，直接替换
                ReplaceNode(target, source.DeepClone());
                return;
            }
            JsonObject targetObject = target.AsObject();
            JsonObject sourceObject = source.AsObject();
            foreach (KeyValuePair<string, JsonNode> sourceChild in sourceObject) {
                if (sourceChild.Value == null) {
                    continue;
                }
                if (targetObject.TryGetPropertyValue(sourceChild.Key, out JsonNode targetChild)) {
                    // 目标中存在同名属性，递归合并
                    MergeInto(targetChild, sourceChild.Value);
                }
                else {
                    // 目标中不存在，添加
                    targetObject.Add(sourceChild.Key, sourceChild.Value.DeepClone());
                }
            }
        }

        static void MergeArray(JsonNode target, JsonNode source) {
            // 检查 source 是否为数组操作符对象
            if (source.GetValueKind() == JsonValueKind.Object) {
                JsonObject sourceObj = source.AsObject();
                bool hasOperators = sourceObj.ContainsKey("$replace")
                    || sourceObj.ContainsKey("$remove")
                    || sourceObj.ContainsKey("$prepend")
                    || sourceObj.ContainsKey("$append");
                if (hasOperators) {
                    ApplyArrayOperators(target, sourceObj);
                    return;
                }
            }

            // source 是普通数组，直接替换 target
            if (source.GetValueKind() != JsonValueKind.Array) {
                ReplaceNode(target, source.DeepClone());
                return;
            }

            // 默认行为：直接替换
            ReplaceNode(target, source.DeepClone());
        }

        static void ApplyArrayOperators(JsonNode target, JsonObject operators) {
            JsonArray resultArray;

            // 1. $replace - 直接替换
            if (operators.TryGetPropertyValue("$replace", out JsonNode replaceNode)) {
                if (replaceNode is JsonArray replaceArray) {
                    resultArray = replaceArray.DeepClone().AsArray();
                }
                else {
                    // $replace 不是数组，创建空数组
                    resultArray = new JsonArray();
                }
            }
            else {
                // 没有 $replace，从 target 开始
                if (target.GetValueKind() == JsonValueKind.Array) {
                    resultArray = target.DeepClone().AsArray();
                }
                else {
                    resultArray = new JsonArray();
                }
            }

            // 2. $remove - 删除完全匹配的元素
            if (operators.TryGetPropertyValue("$remove", out JsonNode removeNode)) {
                ApplyRemove(resultArray, removeNode);
            }

            // 3. $prepend - 插入到开头
            if (operators.TryGetPropertyValue("$prepend", out JsonNode prependNode)) {
                ApplyPrepend(resultArray, prependNode);
            }

            // 4. $append - 追加到末尾
            if (operators.TryGetPropertyValue("$append", out JsonNode appendNode)) {
                ApplyAppend(resultArray, appendNode);
            }

            // 用结果替换 target
            ReplaceNode(target, resultArray);
        }

        static void ApplyRemove(JsonArray array, JsonNode removeNode) {
            if (removeNode is JsonArray removeArray) {
                // 删除多个元素
                for (int i = array.Count - 1; i >= 0; i--) {
                    foreach (JsonNode removeItem in removeArray) {
                        if (JsonEquals(array[i], removeItem)) {
                            array.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            else {
                // 删除单个元素
                for (int i = array.Count - 1; i >= 0; i--) {
                    if (JsonEquals(array[i], removeNode)) {
                        array.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        static void ApplyPrepend(JsonArray array, JsonNode prependNode) {
            if (prependNode is JsonArray prependArray) {
                // 插入多个元素到开头
                for (int i = prependArray.Count - 1; i >= 0; i--) {
                    array.Insert(0, prependArray[i]?.DeepClone());
                }
            }
            else {
                // 插入单个元素到开头
                array.Insert(0, prependNode.DeepClone());
            }
        }

        static void ApplyAppend(JsonArray array, JsonNode appendNode) {
            if (appendNode is JsonArray appendArray) {
                // 追加多个元素
                foreach (JsonNode item in appendArray) {
                    array.Add(item?.DeepClone());
                }
            }
            else {
                // 追加单个元素
                array.Add(appendNode.DeepClone());
            }
        }

        /// <summary>
        /// 比较两个 JsonNode 是否相等（值相等）
        /// </summary>
        static bool JsonEquals(JsonNode a, JsonNode b) {
            if (a == null
                && b == null) {
                return true;
            }
            if (a == null
                || b == null) {
                return false;
            }
            JsonValueKind kindA = a.GetValueKind();
            JsonValueKind kindB = b.GetValueKind();
            if (kindA != kindB) {
                return false;
            }
            switch (kindA) {
                case JsonValueKind.Object:
                    JsonObject objA = a.AsObject();
                    JsonObject objB = b.AsObject();
                    if (objA.Count != objB.Count) {
                        return false;
                    }
                    foreach (KeyValuePair<string, JsonNode> prop in objA) {
                        if (!objB.TryGetPropertyValue(prop.Key, out JsonNode propB)
                            || !JsonEquals(prop.Value, propB)) {
                            return false;
                        }
                    }
                    return true;
                case JsonValueKind.Array:
                    JsonArray arrA = a.AsArray();
                    JsonArray arrB = b.AsArray();
                    if (arrA.Count != arrB.Count) {
                        return false;
                    }
                    for (int i = 0; i < arrA.Count; i++) {
                        if (!JsonEquals(arrA[i], arrB[i])) {
                            return false;
                        }
                    }
                    return true;
                case JsonValueKind.String: return a.GetValue<string>() == b.GetValue<string>();
                case JsonValueKind.Number: return a.GetValue<double>() == b.GetValue<double>();
                case JsonValueKind.True:
                case JsonValueKind.False: return a.GetValue<bool>() == b.GetValue<bool>();
                case JsonValueKind.Null: return true;
                default: return false;
            }
        }

        /// <summary>
        /// 替换 JsonNode 的值（保持其在父节点中的位置）
        /// </summary>
        static void ReplaceNode(JsonNode oldNode, JsonNode newNode) {
            switch (oldNode.Parent) {
                case JsonObject parentObject: parentObject[oldNode.GetPropertyName()] = newNode; break;
                case JsonArray parentArray: parentArray[oldNode.GetElementIndex()] = newNode; break;
            }
        }
    }
}