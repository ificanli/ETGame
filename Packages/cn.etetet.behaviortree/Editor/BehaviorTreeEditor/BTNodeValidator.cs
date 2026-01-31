using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ET
{
    /// <summary>
    /// BTNode的BTInput/BTOutput校验工具类
    /// </summary>
    public static class BTNodeValidator
    {
        /// <summary>
        /// 校验BTNode及其子节点的BTInput是否合法
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="availableOutputs">当前可用的BTOutput集合（名称->类型）</param>
        /// <param name="errors">错误信息列表</param>
        public static void ValidateNodeRecursive(BTNode node, Dictionary<string, Type> availableOutputs, List<string> errors)
        {
            ValidateNodeRecursive(node, availableOutputs, errors, false);
        }

        /// <summary>
        /// 校验BTNode及其子节点的BTInput是否合法，可选择跳过当前节点输入校验
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="availableOutputs">当前可用的BTOutput集合（名称->类型）</param>
        /// <param name="errors">错误信息列表</param>
        /// <param name="skipInputOnCurrentNode">是否跳过当前节点的BTInput校验</param>
        public static void ValidateNodeRecursive(BTNode node, Dictionary<string, Type> availableOutputs, List<string> errors, bool skipInputOnCurrentNode)
        {
            // 获取当前节点的所有BTOutput，添加到可用输出集合
            Dictionary<string, Type> currentOutputs = new Dictionary<string, Type>(availableOutputs);
            FieldInfo[] fieldInfos = node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var btOutputFields = fieldInfos.Where(field => field.GetCustomAttributes(typeof(BTOutput), false).Any());
            foreach (FieldInfo field in btOutputFields)
            {
                string outputValue = field.GetValue(node) as string;
                if (!string.IsNullOrEmpty(outputValue))
                {
                    BTOutput btOutputAttr = field.GetCustomAttribute<BTOutput>();
                    currentOutputs[outputValue] = btOutputAttr.Type;
                }
            }

            if (!skipInputOnCurrentNode)
            {
                // 校验当前节点的BTInput
                var btInputFields = fieldInfos.Where(field => field.GetCustomAttributes(typeof(BTInput), false).Any());
                foreach (FieldInfo field in btInputFields)
                {
                    string inputValue = field.GetValue(node) as string;

                    // 如果BTInput字段为空，跳过校验
                    if (string.IsNullOrEmpty(inputValue))
                    {
                        continue;
                    }

                    // 检查输入值是否在可用的输出集合中
                    if (!availableOutputs.ContainsKey(inputValue))
                    {
                        string errorMsg = $"Node[{node.Id}] {node.GetType().Name}: Field '{field.Name}' requires input '{inputValue}' which is not provided by previous nodes";
                        errors.Add(errorMsg);
                    }
                    else
                    {
                        // 检查类型是否匹配
                        BTInput btInputAttr = field.GetCustomAttribute<BTInput>();
                        Type expectedType = btInputAttr.Type;
                        Type actualType = availableOutputs[inputValue];

                        if (expectedType != actualType)
                        {
                            string errorMsg = $"Node[{node.Id}] {node.GetType().Name}: Field '{field.Name}' expects type '{expectedType.Name}' but '{inputValue}' provides type '{actualType.Name}'";
                            errors.Add(errorMsg);
                        }
                    }
                }
            }

            // 递归处理子节点
            if (node.Children != null)
            {
                // 累积所有前面子节点的输出，供后续子节点使用
                Dictionary<string, Type> accumulatedOutputs = new Dictionary<string, Type>(currentOutputs);

                foreach (BTNode child in node.Children)
                {
                    // 跳过null节点（可能是编辑器中删除节点后留下的空引用）
                    if (child == null)
                    {
                        continue;
                    }

                    // 传递累积的输出给子节点
                    ValidateNodeRecursive(child, accumulatedOutputs, errors, false);

                    // 收集当前子节点的输出供后续子节点使用
                    var childOutputFields = child.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(field => field.GetCustomAttributes(typeof(BTOutput), false).Any());

                    foreach (FieldInfo field in childOutputFields)
                    {
                        string outputValue = field.GetValue(child) as string;
                        if (!string.IsNullOrEmpty(outputValue))
                        {
                            BTOutput btOutputAttr = field.GetCustomAttribute<BTOutput>();
                            accumulatedOutputs[outputValue] = btOutputAttr.Type;
                        }
                    }
                }
            }
        }

        public static bool IsTargetSelectorRoot(BTNode node)
        {
            if (node is not BTRoot)
            {
                return false;
            }

            Type currentType = node.GetType();
            while (currentType != null)
            {
                if (currentType.Name == "TargetSelector")
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }
    }
}
