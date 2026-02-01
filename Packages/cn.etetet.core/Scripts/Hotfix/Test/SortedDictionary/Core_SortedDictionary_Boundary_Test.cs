using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 边界条件测试
    /// 测试空字典、Clear、单元素、大量数据等边界情况
    /// </summary>
    public class Core_SortedDictionary_Boundary_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // ========== 空字典测试 ==========

            // 测试1: 空字典的 Count
            {
                var dict = new SortedDictionary<int, string>();
                if (dict.Count != 0)
                {
                    Log.Console($"空字典Count: 应为 0, 实际为 {dict.Count}");
                    return 1;
                }
                Log.Debug("测试1通过: 空字典的 Count");
            }

            // 测试2: 空字典的遍历
            {
                var dict = new SortedDictionary<int, string>();
                int count = 0;
                foreach (var kvp in dict)
                {
                    count++;
                }
                if (count != 0)
                {
                    Log.Console($"空字典遍历: 不应有任何迭代, 实际迭代了 {count} 次");
                    return 2;
                }
                Log.Debug("测试2通过: 空字典的遍历");
            }

            // 测试3: 空字典的 Keys 和 Values
            {
                var dict = new SortedDictionary<int, string>();
                if (dict.Keys.Count != 0)
                {
                    Log.Console($"空字典Keys: Count 应为 0, 实际为 {dict.Keys.Count}");
                    return 3;
                }
                if (dict.Values.Count != 0)
                {
                    Log.Console($"空字典Values: Count 应为 0, 实际为 {dict.Values.Count}");
                    return 4;
                }
                Log.Debug("测试3通过: 空字典的 Keys 和 Values");
            }

            // 测试4: 空字典的 ContainsKey 和 ContainsValue
            {
                var dict = new SortedDictionary<int, string>();
                if (dict.ContainsKey(1))
                {
                    Log.Console("空字典ContainsKey: 不应包含任何 key");
                    return 5;
                }
                if (dict.ContainsValue("any"))
                {
                    Log.Console("空字典ContainsValue: 不应包含任何 value");
                    return 6;
                }
                Log.Debug("测试4通过: 空字典的 ContainsKey 和 ContainsValue");
            }

            // ========== Clear 测试 ==========

            // 测试5: Clear 基本功能
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };
                dict.Clear();
                if (dict.Count != 0)
                {
                    Log.Console($"Clear后: Count 应为 0, 实际为 {dict.Count}");
                    return 7;
                }
                Log.Debug("测试5通过: Clear 基本功能");
            }

            // 测试6: Clear 后不包含原有元素
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                dict.Clear();
                if (dict.ContainsKey(1) || dict.ContainsKey(2))
                {
                    Log.Console("Clear后: 不应包含原有 key");
                    return 8;
                }
                if (dict.ContainsValue("one") || dict.ContainsValue("two"))
                {
                    Log.Console("Clear后: 不应包含原有 value");
                    return 9;
                }
                Log.Debug("测试6通过: Clear 后不包含原有元素");
            }

            // 测试7: Clear 后可以重新添加
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                dict.Clear();
                dict.Add(1, "new_one");
                dict.Add(2, "new_two");
                if (dict.Count != 2)
                {
                    Log.Console($"Clear后重新添加: Count 应为 2, 实际为 {dict.Count}");
                    return 10;
                }
                if (dict[1] != "new_one" || dict[2] != "new_two")
                {
                    Log.Console("Clear后重新添加: 值不正确");
                    return 11;
                }
                Log.Debug("测试7通过: Clear 后可以重新添加");
            }

            // 测试8: 对空字典调用 Clear
            {
                var dict = new SortedDictionary<int, string>();
                dict.Clear(); // 不应抛出异常
                if (dict.Count != 0)
                {
                    Log.Console($"空字典Clear: Count 应为 0, 实际为 {dict.Count}");
                    return 12;
                }
                Log.Debug("测试8通过: 对空字典调用 Clear");
            }

            // 测试9: 多次 Clear
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                dict.Clear();
                dict.Add(2, "two");
                dict.Clear();
                dict.Add(3, "three");
                dict.Clear();
                if (dict.Count != 0)
                {
                    Log.Console($"多次Clear: Count 应为 0, 实际为 {dict.Count}");
                    return 13;
                }
                Log.Debug("测试9通过: 多次 Clear");
            }

            // ========== 单元素测试 ==========

            // 测试10: 单元素字典
            {
                var dict = new SortedDictionary<int, string> { { 42, "answer" } };
                if (dict.Count != 1)
                {
                    Log.Console($"单元素字典: Count 应为 1, 实际为 {dict.Count}");
                    return 14;
                }
                if (!dict.ContainsKey(42) || dict[42] != "answer")
                {
                    Log.Console("单元素字典: 元素不正确");
                    return 15;
                }
                Log.Debug("测试10通过: 单元素字典");
            }

            // 测试11: 单元素字典遍历
            {
                var dict = new SortedDictionary<int, string> { { 42, "answer" } };
                int count = 0;
                foreach (var kvp in dict)
                {
                    if (kvp.Key != 42 || kvp.Value != "answer")
                    {
                        Log.Console("单元素字典遍历: 元素不正确");
                        return 16;
                    }
                    count++;
                }
                if (count != 1)
                {
                    Log.Console($"单元素字典遍历: 应迭代 1 次, 实际 {count} 次");
                    return 17;
                }
                Log.Debug("测试11通过: 单元素字典遍历");
            }

            // 测试12: 单元素字典删除后变空
            {
                var dict = new SortedDictionary<int, string> { { 42, "answer" } };
                dict.Remove(42);
                if (dict.Count != 0)
                {
                    Log.Console($"单元素删除后: Count 应为 0, 实际为 {dict.Count}");
                    return 18;
                }
                Log.Debug("测试12通过: 单元素字典删除后变空");
            }

            // ========== 大量数据测试 ==========

            // 测试13: 大量数据插入
            {
                var dict = new SortedDictionary<int, int>();
                const int count = 1000;
                for (int i = 0; i < count; i++)
                {
                    dict.Add(i, i * 2);
                }
                if (dict.Count != count)
                {
                    Log.Console($"大量数据插入: Count 应为 {count}, 实际为 {dict.Count}");
                    return 19;
                }
                Log.Debug("测试13通过: 大量数据插入");
            }

            // 测试14: 大量数据乱序插入
            {
                var dict = new SortedDictionary<int, int>();
                const int count = 1000;
                var random = new Random(42);
                var numbers = Enumerable.Range(0, count).OrderBy(_ => random.Next()).ToList();
                foreach (int n in numbers)
                {
                    dict.Add(n, n * 2);
                }
                if (dict.Count != count)
                {
                    Log.Console($"大量数据乱序插入: Count 应为 {count}, 实际为 {dict.Count}");
                    return 20;
                }
                // 验证排序正确
                int expected = 0;
                foreach (var kvp in dict)
                {
                    if (kvp.Key != expected)
                    {
                        Log.Console($"大量数据乱序插入: 排序错误, 期望 {expected}, 实际 {kvp.Key}");
                        return 21;
                    }
                    expected++;
                }
                Log.Debug("测试14通过: 大量数据乱序插入");
            }

            // 测试15: 大量数据查找
            {
                var dict = new SortedDictionary<int, int>();
                const int count = 1000;
                for (int i = 0; i < count; i++)
                {
                    dict.Add(i, i * 2);
                }
                // 验证所有元素都能找到
                for (int i = 0; i < count; i++)
                {
                    if (!dict.ContainsKey(i))
                    {
                        Log.Console($"大量数据查找: 应包含 key {i}");
                        return 22;
                    }
                    if (dict[i] != i * 2)
                    {
                        Log.Console($"大量数据查找: key {i} 的值应为 {i * 2}, 实际为 {dict[i]}");
                        return 23;
                    }
                }
                Log.Debug("测试15通过: 大量数据查找");
            }

            // 测试16: 大量数据删除
            {
                var dict = new SortedDictionary<int, int>();
                const int count = 1000;
                for (int i = 0; i < count; i++)
                {
                    dict.Add(i, i * 2);
                }
                // 删除偶数
                for (int i = 0; i < count; i += 2)
                {
                    dict.Remove(i);
                }
                if (dict.Count != count / 2)
                {
                    Log.Console($"大量数据删除: Count 应为 {count / 2}, 实际为 {dict.Count}");
                    return 24;
                }
                // 验证只剩奇数
                foreach (var kvp in dict)
                {
                    if (kvp.Key % 2 == 0)
                    {
                        Log.Console($"大量数据删除: 不应包含偶数 key {kvp.Key}");
                        return 25;
                    }
                }
                Log.Debug("测试16通过: 大量数据删除");
            }

            // 测试17: 大量数据 Clear
            {
                var dict = new SortedDictionary<int, int>();
                const int count = 1000;
                for (int i = 0; i < count; i++)
                {
                    dict.Add(i, i * 2);
                }
                dict.Clear();
                if (dict.Count != 0)
                {
                    Log.Console($"大量数据Clear: Count 应为 0, 实际为 {dict.Count}");
                    return 26;
                }
                Log.Debug("测试17通过: 大量数据 Clear");
            }

            // 测试18: 极端 key 值
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(int.MinValue, "min");
                dict.Add(int.MaxValue, "max");
                dict.Add(0, "zero");
                if (dict.Count != 3)
                {
                    Log.Console($"极端key值: Count 应为 3, 实际为 {dict.Count}");
                    return 27;
                }
                // 验证排序
                int[] expectedOrder = { int.MinValue, 0, int.MaxValue };
                int index = 0;
                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"极端key值排序: 位置 {index} 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 28;
                    }
                    index++;
                }
                Log.Debug("测试18通过: 极端 key 值");
            }

            Log.Debug("SortedDictionary_Boundary_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
