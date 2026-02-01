using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 排序功能测试
    /// 测试默认排序和自定义 Comparer 的排序行为
    /// </summary>
    public class Core_SortedDictionary_Order_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // 测试1: int 类型默认排序（升序）
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(5, "five");
                dict.Add(1, "one");
                dict.Add(3, "three");
                dict.Add(2, "two");
                dict.Add(4, "four");

                int[] expectedOrder = { 1, 2, 3, 4, 5 };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"int默认排序: 位置 {index} 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 1;
                    }
                    index++;
                }

                Log.Debug("测试1通过: int 类型默认排序");
            }

            // 测试2: 负数和正数混合排序
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(0, "zero");
                dict.Add(-2, "minus two");
                dict.Add(2, "two");
                dict.Add(-1, "minus one");
                dict.Add(1, "one");

                int[] expectedOrder = { -2, -1, 0, 1, 2 };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"负数正数混合: 位置 {index} 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 2;
                    }
                    index++;
                }

                Log.Debug("测试2通过: 负数和正数混合排序");
            }

            // 测试3: string 类型默认排序（字典序）
            {
                var dict = new SortedDictionary<string, int>();
                dict.Add("banana", 1);
                dict.Add("apple", 2);
                dict.Add("cherry", 3);
                dict.Add("date", 4);

                string[] expectedOrder = { "apple", "banana", "cherry", "date" };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"string默认排序: 位置 {index} 期望 '{expectedOrder[index]}', 实际 '{kvp.Key}'");
                        return 3;
                    }
                    index++;
                }

                Log.Debug("测试3通过: string 类型默认排序");
            }

            // 测试4: string 大小写排序 (使用 Ordinal 比较器确保 ASCII 顺序)
            {
                var dict = new SortedDictionary<string, int>(StringComparer.Ordinal);
                dict.Add("Apple", 1);
                dict.Add("apple", 2);
                dict.Add("APPLE", 3);
                dict.Add("Banana", 4);

                // Ordinal 排序: 按 ASCII 值排序，大写字母 (A=65) 在小写字母 (a=97) 之前
                string[] expectedOrder = { "APPLE", "Apple", "Banana", "apple" };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"string大小写排序: 位置 {index} 期望 '{expectedOrder[index]}', 实际 '{kvp.Key}'");
                        return 4;
                    }
                    index++;
                }

                Log.Debug("测试4通过: string 大小写排序");
            }

            // 测试5: 自定义 Comparer（逆序）
            {
                var reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
                var dict = new SortedDictionary<int, string>(reverseComparer);
                dict.Add(1, "one");
                dict.Add(3, "three");
                dict.Add(2, "two");
                dict.Add(5, "five");
                dict.Add(4, "four");

                int[] expectedOrder = { 5, 4, 3, 2, 1 }; // 逆序
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"逆序Comparer: 位置 {index} 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 5;
                    }
                    index++;
                }

                Log.Debug("测试5通过: 自定义 Comparer（逆序）");
            }

            // 测试6: 按字符串长度排序
            {
                var lengthComparer = Comparer<string>.Create((a, b) =>
                {
                    int cmp = a.Length.CompareTo(b.Length);
                    return cmp != 0 ? cmp : string.Compare(a, b, StringComparison.Ordinal);
                });

                var dict = new SortedDictionary<string, int>(lengthComparer);
                dict.Add("bb", 1);
                dict.Add("a", 2);
                dict.Add("ccc", 3);
                dict.Add("dddd", 4);
                dict.Add("ee", 5);

                string[] expectedOrder = { "a", "bb", "ee", "ccc", "dddd" }; // 按长度排序，同长度按字典序
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"按长度排序: 位置 {index} 期望 '{expectedOrder[index]}', 实际 '{kvp.Key}'");
                        return 6;
                    }
                    index++;
                }

                Log.Debug("测试6通过: 按字符串长度排序");
            }

            // 测试7: 忽略大小写的字符串排序
            {
                var caseInsensitiveComparer = StringComparer.OrdinalIgnoreCase;
                var dict = new SortedDictionary<string, int>(caseInsensitiveComparer);
                dict.Add("Apple", 1);
                dict.Add("banana", 2);
                dict.Add("CHERRY", 3);

                string[] expectedOrder = { "Apple", "banana", "CHERRY" };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"忽略大小写排序: 位置 {index} 期望 '{expectedOrder[index]}', 实际 '{kvp.Key}'");
                        return 7;
                    }
                    index++;
                }

                // 验证忽略大小写的查找
                if (!dict.ContainsKey("apple") || !dict.ContainsKey("APPLE"))
                {
                    Log.Console("忽略大小写排序: 应能用不同大小写查找");
                    return 8;
                }

                Log.Debug("测试7通过: 忽略大小写的字符串排序");
            }

            // 测试8: 验证 Keys 集合的排序顺序
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(5, "five");
                dict.Add(1, "one");
                dict.Add(3, "three");

                var keys = dict.Keys.ToList();
                int[] expectedKeys = { 1, 3, 5 };

                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys[i] != expectedKeys[i])
                    {
                        Log.Console($"Keys排序: 位置 {i} 期望 {expectedKeys[i]}, 实际 {keys[i]}");
                        return 9;
                    }
                }

                Log.Debug("测试8通过: Keys 集合的排序顺序");
            }

            // 测试9: 验证 Values 集合按 Key 排序顺序
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(3, "three");
                dict.Add(1, "one");
                dict.Add(2, "two");

                var values = dict.Values.ToList();
                string[] expectedValues = { "one", "two", "three" }; // 按 key 排序后的 value 顺序

                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] != expectedValues[i])
                    {
                        Log.Console($"Values排序: 位置 {i} 期望 '{expectedValues[i]}', 实际 '{values[i]}'");
                        return 10;
                    }
                }

                Log.Debug("测试9通过: Values 集合按 Key 排序顺序");
            }

            // 测试10: 大量数据乱序插入后验证排序
            {
                var dict = new SortedDictionary<int, int>();
                var random = new Random(42); // 固定种子保证可重复
                var numbers = Enumerable.Range(0, 100).OrderBy(_ => random.Next()).ToList();

                foreach (int n in numbers)
                {
                    dict.Add(n, n * 2);
                }

                int expected = 0;
                foreach (var kvp in dict)
                {
                    if (kvp.Key != expected)
                    {
                        Log.Console($"大量数据排序: 期望 key {expected}, 实际 {kvp.Key}");
                        return 11;
                    }

                    if (kvp.Value != expected * 2)
                    {
                        Log.Console($"大量数据排序: key {expected} 的 value 应为 {expected * 2}, 实际 {kvp.Value}");
                        return 12;
                    }
                    expected++;
                }

                Log.Debug("测试10通过: 大量数据乱序插入后验证排序");
            }

            // 测试11: 删除元素后排序保持正确
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(5, "five");
                dict.Add(1, "one");
                dict.Add(3, "three");
                dict.Add(2, "two");
                dict.Add(4, "four");

                dict.Remove(2);
                dict.Remove(4);

                int[] expectedOrder = { 1, 3, 5 };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"删除后排序: 位置 {index} 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 13;
                    }
                    index++;
                }

                Log.Debug("测试11通过: 删除元素后排序保持正确");
            }

            // 测试12: 添加删除混合操作后排序正确
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(3, "three");
                dict.Add(1, "one");
                dict.Remove(3);
                dict.Add(2, "two");
                dict.Add(4, "four");
                dict.Remove(1);
                dict.Add(5, "five");

                int[] expectedOrder = { 2, 4, 5 };
                int index = 0;

                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"混合操作后排序: 位置 {index} 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 14;
                    }
                    index++;
                }

                Log.Debug("测试12通过: 添加删除混合操作后排序正确");
            }

            Log.Debug("SortedDictionary_Order_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
