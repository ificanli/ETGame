using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 构造函数测试
    /// 测试所有构造函数重载和参数验证
    /// </summary>
    public class Core_SortedDictionary_Constructor_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // 测试1: 无参构造函数
            {
                var dict = new SortedDictionary<int, string>();
                if (dict.Count != 0)
                {
                    Log.Console($"无参构造: Count 应为 0, 实际为 {dict.Count}");
                    return 1;
                }

                if (dict.Comparer != Comparer<int>.Default)
                {
                    Log.Console("无参构造: Comparer 应为默认比较器");
                    return 2;
                }

                Log.Debug("测试1通过: 无参构造函数");
            }

            // 测试2: IComparer 构造函数
            {
                var reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
                var dict = new SortedDictionary<int, string>(reverseComparer);

                if (dict.Count != 0)
                {
                    Log.Console($"IComparer构造: Count 应为 0, 实际为 {dict.Count}");
                    return 3;
                }

                if (dict.Comparer != reverseComparer)
                {
                    Log.Console("IComparer构造: Comparer 未正确设置");
                    return 4;
                }

                Log.Debug("测试2通过: IComparer 构造函数");
            }

            // 测试3: IComparer 为 null 时使用默认比较器
            {
                var dict = new SortedDictionary<int, string>((IComparer<int>)null);

                if (dict.Comparer != Comparer<int>.Default)
                {
                    Log.Console("IComparer为null: 应使用默认比较器");
                    return 5;
                }

                Log.Debug("测试3通过: IComparer 为 null 时使用默认比较器");
            }

            // 测试4: IDictionary 构造函数
            {
                var source = new Dictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };

                var dict = new SortedDictionary<int, string>(source);

                if (dict.Count != 3)
                {
                    Log.Console($"IDictionary构造: Count 应为 3, 实际为 {dict.Count}");
                    return 6;
                }

                if (dict[1] != "one" || dict[2] != "two" || dict[3] != "three")
                {
                    Log.Console("IDictionary构造: 数据未正确复制");
                    return 7;
                }

                Log.Debug("测试4通过: IDictionary 构造函数");
            }

            // 测试5: IDictionary + IComparer 构造函数
            {
                var source = new Dictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                var reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));

                var dict = new SortedDictionary<int, string>(source, reverseComparer);

                if (dict.Count != 3)
                {
                    Log.Console($"IDictionary+IComparer构造: Count 应为 3, 实际为 {dict.Count}");
                    return 8;
                }

                if (dict.Comparer != reverseComparer)
                {
                    Log.Console("IDictionary+IComparer构造: Comparer 未正确设置");
                    return 9;
                }

                // 验证排序顺序（逆序）
                int[] expectedOrder = { 3, 2, 1 };
                int index = 0;
                foreach (var kvp in dict)
                {
                    if (kvp.Key != expectedOrder[index])
                    {
                        Log.Console($"IDictionary+IComparer构造: 排序顺序错误, 期望 {expectedOrder[index]}, 实际 {kvp.Key}");
                        return 10;
                    }

                    index++;
                }

                Log.Debug("测试5通过: IDictionary + IComparer 构造函数");
            }

            // 测试6: null dictionary 应抛出 ArgumentNullException
            {
                try
                {
                    var dict = new SortedDictionary<int, string>((IDictionary<int, string>)null);
                    Log.Console("null dictionary: 应抛出 ArgumentNullException");
                    return 11;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试6通过: null dictionary 正确抛出 ArgumentNullException");
                }
            }

            // 测试7: 从另一个 SortedDictionary 构造（优化路径）
            {
                var source = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };

                var dict = new SortedDictionary<int, string>(source);

                if (dict.Count != 3)
                {
                    Log.Console($"从SortedDictionary构造: Count 应为 3, 实际为 {dict.Count}");
                    return 12;
                }

                // 验证是独立副本
                source.Add(4, "four");
                if (dict.Count != 3)
                {
                    Log.Console("从SortedDictionary构造: 应为独立副本");
                    return 13;
                }

                Log.Debug("测试7通过: 从 SortedDictionary 构造");
            }

            // 测试8: 从带自定义 Comparer 的 SortedDictionary 构造
            {
                var reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
                var source = new SortedDictionary<int, string>(reverseComparer)
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };

                // 使用相同的 Comparer
                var dict = new SortedDictionary<int, string>(source, reverseComparer);

                if (dict.Comparer != reverseComparer)
                {
                    Log.Console("从带Comparer的SortedDictionary构造: Comparer 未正确设置");
                    return 14;
                }

                Log.Debug("测试8通过: 从带自定义 Comparer 的 SortedDictionary 构造");
            }

            Log.Debug("SortedDictionary_Constructor_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
