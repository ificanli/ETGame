using System;
using System.Collections;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 枚举器测试
    /// 测试 foreach 遍历、Reset、遍历中修改等行为
    /// </summary>
    public class Core_SortedDictionary_Enumerator_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // 测试1: 基本遍历
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                int count = 0;
                foreach (var kvp in dict)
                {
                    count++;
                }
                if (count != 3)
                {
                    Log.Console($"基本遍历: 应迭代 3 次, 实际 {count} 次");
                    return 1;
                }
                Log.Debug("测试1通过: 基本遍历");
            }

            // 测试2: 遍历顺序
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                int[] expectedKeys = { 1, 2, 3 };
                string[] expectedValues = { "one", "two", "three" };
                int index = 0;
                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedKeys[index] || kvp.Value != expectedValues[index])
                    {
                        Log.Console($"遍历顺序: 位置 {index} 期望 ({expectedKeys[index]}, '{expectedValues[index]}'), 实际 ({kvp.Key}, '{kvp.Value}')");
                        return 2;
                    }
                    index++;
                }
                Log.Debug("测试2通过: 遍历顺序");
            }

            // 测试3: 使用 GetEnumerator 手动遍历
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                var enumerator = dict.GetEnumerator();
                int count = 0;
                while (enumerator.MoveNext())
                {
                    count++;
                }
                if (count != 2)
                {
                    Log.Console($"手动遍历: 应迭代 2 次, 实际 {count} 次");
                    return 3;
                }
                enumerator.Dispose();
                Log.Debug("测试3通过: 使用 GetEnumerator 手动遍历");
            }

            // 测试4: Reset 操作
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                var enumerator = dict.GetEnumerator();
                int count1 = 0;
                while (enumerator.MoveNext())
                {
                    count1++;
                }
                ((IEnumerator)enumerator).Reset();
                int count2 = 0;
                while (enumerator.MoveNext())
                {
                    count2++;
                }
                if (count1 != 2 || count2 != 2)
                {
                    Log.Console($"Reset操作: 两次遍历应各迭代 2 次, 实际 {count1} 和 {count2} 次");
                    return 4;
                }
                enumerator.Dispose();
                Log.Debug("测试4通过: Reset 操作");
            }

            // 测试5: 遍历中添加元素应抛出 InvalidOperationException
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                try
                {
                    foreach (var kvp in dict)
                    {
                        if (kvp.Key == 1)
                        {
                            dict.Add(3, "three");
                        }
                    }
                    Log.Console("遍历中添加: 应抛出 InvalidOperationException");
                    return 5;
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("测试5通过: 遍历中添加元素正确抛出异常");
                }
            }

            // 测试6: 遍历中删除元素应抛出 InvalidOperationException
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };
                try
                {
                    foreach (var kvp in dict)
                    {
                        if (kvp.Key == 2)
                        {
                            dict.Remove(1);
                        }
                    }
                    Log.Console("遍历中删除: 应抛出 InvalidOperationException");
                    return 6;
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("测试6通过: 遍历中删除元素正确抛出异常");
                }
            }

            // 测试7: 遍历中 Clear 应抛出 InvalidOperationException
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                try
                {
                    foreach (var kvp in dict)
                    {
                        dict.Clear();
                    }
                    Log.Console("遍历中Clear: 应抛出 InvalidOperationException");
                    return 7;
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("测试7通过: 遍历中 Clear 正确抛出异常");
                }
            }

            // 测试8: 遍历中更新值（通过索引器）应抛出 InvalidOperationException
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                try
                {
                    foreach (var kvp in dict)
                    {
                        dict[kvp.Key] = "updated";
                    }
                    Log.Console("遍历中更新值: 应抛出 InvalidOperationException");
                    return 8;
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("测试8通过: 遍历中更新值正确抛出异常");
                }
            }

            // 测试9: 未开始遍历时访问 Current (IEnumerator.Current)
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                var enumerator = dict.GetEnumerator();
                IEnumerator nonGenericEnumerator = enumerator;
                try
                {
                    var _ = nonGenericEnumerator.Current;
                    Log.Console("未开始遍历访问Current: 应抛出 InvalidOperationException");
                    return 9;
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("测试9通过: 未开始遍历时访问 Current 正确抛出异常");
                }
                enumerator.Dispose();
            }

            // 测试10: 遍历结束后访问 Current (IEnumerator.Current)
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                var enumerator = dict.GetEnumerator();
                while (enumerator.MoveNext()) { }
                IEnumerator nonGenericEnumerator = enumerator;
                try
                {
                    var _ = nonGenericEnumerator.Current;
                    Log.Console("遍历结束后访问Current: 应抛出 InvalidOperationException");
                    return 10;
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("测试10通过: 遍历结束后访问 Current 正确抛出异常");
                }
                enumerator.Dispose();
            }

            // 测试11: IDictionaryEnumerator 接口
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                IDictionary nonGenericDict = dict;
                IDictionaryEnumerator enumerator = nonGenericDict.GetEnumerator();
                int count = 0;
                int[] expectedKeys = { 1, 2 };
                while (enumerator.MoveNext())
                {
                    if ((int)enumerator.Key != expectedKeys[count])
                    {
                        Log.Console($"IDictionaryEnumerator: 位置 {count} 期望 key {expectedKeys[count]}, 实际 {enumerator.Key}");
                        return 11;
                    }
                    var entry = enumerator.Entry;
                    if ((int)entry.Key != expectedKeys[count])
                    {
                        Log.Console($"IDictionaryEnumerator.Entry: 位置 {count} 期望 key {expectedKeys[count]}, 实际 {entry.Key}");
                        return 12;
                    }
                    count++;
                }
                if (count != 2)
                {
                    Log.Console($"IDictionaryEnumerator: 应迭代 2 次, 实际 {count} 次");
                    return 13;
                }
                Log.Debug("测试11通过: IDictionaryEnumerator 接口");
            }

            // 测试12: Keys 枚举器
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                int[] expectedKeys = { 1, 2, 3 };
                int index = 0;
                foreach (int key in dict.Keys)
                {
                    if (key != expectedKeys[index])
                    {
                        Log.Console($"Keys枚举器: 位置 {index} 期望 {expectedKeys[index]}, 实际 {key}");
                        return 14;
                    }
                    index++;
                }
                Log.Debug("测试12通过: Keys 枚举器");
            }

            // 测试13: Values 枚举器
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                string[] expectedValues = { "one", "two", "three" };
                int index = 0;
                foreach (string value in dict.Values)
                {
                    if (value != expectedValues[index])
                    {
                        Log.Console($"Values枚举器: 位置 {index} 期望 '{expectedValues[index]}', 实际 '{value}'");
                        return 15;
                    }
                    index++;
                }
                Log.Debug("测试13通过: Values 枚举器");
            }

            // 测试14: 空字典枚举器
            {
                var dict = new SortedDictionary<int, string>();
                int count = 0;
                foreach (var kvp in dict)
                {
                    count++;
                }
                if (count != 0)
                {
                    Log.Console($"空字典枚举器: 不应有任何迭代, 实际 {count} 次");
                    return 16;
                }
                var enumerator = dict.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    Log.Console("空字典枚举器: MoveNext 应返回 false");
                    return 17;
                }
                enumerator.Dispose();
                Log.Debug("测试14通过: 空字典枚举器");
            }

            // 测试15: IEnumerable<KeyValuePair> 接口
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                IEnumerable<KeyValuePair<int, string>> enumerable = dict;
                int count = 0;
                foreach (var kvp in enumerable)
                {
                    count++;
                }
                if (count != 2)
                {
                    Log.Console($"IEnumerable接口: 应迭代 2 次, 实际 {count} 次");
                    return 18;
                }
                Log.Debug("测试15通过: IEnumerable<KeyValuePair> 接口");
            }

            Log.Debug("SortedDictionary_Enumerator_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
