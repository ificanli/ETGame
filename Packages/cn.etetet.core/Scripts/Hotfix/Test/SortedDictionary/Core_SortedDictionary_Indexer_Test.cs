using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 索引器测试
    /// 测试 this[key] 的 get 和 set 操作
    /// </summary>
    public class Core_SortedDictionary_Indexer_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // 测试1: 通过索引器添加新元素
            {
                var dict = new SortedDictionary<int, string>();
                dict[1] = "one";
                dict[2] = "two";

                if (dict.Count != 2)
                {
                    Log.Console($"索引器添加: Count 应为 2, 实际为 {dict.Count}");
                    return 1;
                }

                Log.Debug("测试1通过: 通过索引器添加新元素");
            }

            // 测试2: 通过索引器获取元素
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };

                string val = dict[1];
                if (val != "one")
                {
                    Log.Console($"索引器获取: 应为 'one', 实际为 '{val}'");
                    return 2;
                }

                val = dict[2];
                if (val != "two")
                {
                    Log.Console($"索引器获取: 应为 'two', 实际为 '{val}'");
                    return 3;
                }

                Log.Debug("测试2通过: 通过索引器获取元素");
            }

            // 测试3: 通过索引器更新已存在的元素
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };

                dict[1] = "ONE";

                if (dict[1] != "ONE")
                {
                    Log.Console($"索引器更新: 应为 'ONE', 实际为 '{dict[1]}'");
                    return 4;
                }

                if (dict.Count != 2)
                {
                    Log.Console($"索引器更新: Count 应保持为 2, 实际为 {dict.Count}");
                    return 5;
                }

                Log.Debug("测试3通过: 通过索引器更新已存在的元素");
            }

            // 测试4: 获取不存在的 key 应抛出 KeyNotFoundException
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" }
                };

                try
                {
                    var _ = dict[999];
                    Log.Console("获取不存在的key: 应抛出 KeyNotFoundException");
                    return 6;
                }
                catch (KeyNotFoundException)
                {
                    Log.Debug("测试4通过: 获取不存在的 key 正确抛出异常");
                }
            }

            // 测试5: null key (get) 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>
                {
                    { "a", 1 }
                };

                try
                {
                    var _ = dict[null];
                    Log.Console("获取null key: 应抛出 ArgumentNullException");
                    return 7;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试5通过: 获取 null key 正确抛出异常");
                }
            }

            // 测试6: null key (set) 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>();

                try
                {
                    dict[null] = 1;
                    Log.Console("设置null key: 应抛出 ArgumentNullException");
                    return 8;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试6通过: 设置 null key 正确抛出异常");
                }
            }

            // 测试7: 设置 null value（允许）
            {
                var dict = new SortedDictionary<int, string>();
                dict[1] = null;

                if (dict.Count != 1)
                {
                    Log.Console($"设置null value: Count 应为 1, 实际为 {dict.Count}");
                    return 9;
                }

                if (dict[1] != null)
                {
                    Log.Console("设置null value: 值应为 null");
                    return 10;
                }

                Log.Debug("测试7通过: 设置 null value");
            }

            // 测试8: 更新为 null value
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" }
                };

                dict[1] = null;

                if (dict[1] != null)
                {
                    Log.Console("更新为null value: 值应为 null");
                    return 11;
                }

                Log.Debug("测试8通过: 更新为 null value");
            }

            // 测试9: 连续更新同一个 key
            {
                var dict = new SortedDictionary<int, string>();
                dict[1] = "first";
                dict[1] = "second";
                dict[1] = "third";

                if (dict[1] != "third")
                {
                    Log.Console($"连续更新: 应为 'third', 实际为 '{dict[1]}'");
                    return 12;
                }

                if (dict.Count != 1)
                {
                    Log.Console($"连续更新: Count 应为 1, 实际为 {dict.Count}");
                    return 13;
                }

                Log.Debug("测试9通过: 连续更新同一个 key");
            }

            // 测试10: 混合使用 Add 和索引器
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(1, "one");
                dict[2] = "two";
                dict.Add(3, "three");
                dict[1] = "ONE"; // 更新

                if (dict.Count != 3)
                {
                    Log.Console($"混合使用: Count 应为 3, 实际为 {dict.Count}");
                    return 14;
                }

                if (dict[1] != "ONE" || dict[2] != "two" || dict[3] != "three")
                {
                    Log.Console("混合使用: 值不正确");
                    return 15;
                }

                Log.Debug("测试10通过: 混合使用 Add 和索引器");
            }

            // 测试11: 值类型 value
            {
                var dict = new SortedDictionary<string, int>();
                dict["a"] = 1;
                dict["b"] = 2;
                dict["a"] = 10; // 更新

                if (dict["a"] != 10)
                {
                    Log.Console($"值类型value: 应为 10, 实际为 {dict["a"]}");
                    return 16;
                }

                Log.Debug("测试11通过: 值类型 value");
            }

            // 测试12: 空字典获取 key
            {
                var dict = new SortedDictionary<int, string>();

                try
                {
                    var _ = dict[1];
                    Log.Console("空字典获取key: 应抛出 KeyNotFoundException");
                    return 17;
                }
                catch (KeyNotFoundException)
                {
                    Log.Debug("测试12通过: 空字典获取 key 正确抛出异常");
                }
            }

            Log.Debug("SortedDictionary_Indexer_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
