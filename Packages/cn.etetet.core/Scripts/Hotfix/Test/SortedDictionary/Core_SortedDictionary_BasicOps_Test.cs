using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 基础操作测试
    /// 测试 Add, Remove, ContainsKey, ContainsValue, TryGetValue 等方法
    /// </summary>
    public class Core_SortedDictionary_BasicOps_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // ========== Add 测试 ==========

            // 测试1: 基本添加
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(1, "one");
                dict.Add(2, "two");
                dict.Add(3, "three");

                if (dict.Count != 3)
                {
                    Log.Console($"基本添加: Count 应为 3, 实际为 {dict.Count}");
                    return 1;
                }

                Log.Debug("测试1通过: 基本添加");
            }

            // 测试2: 添加重复 key 应抛出 ArgumentException
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(1, "one");

                try
                {
                    dict.Add(1, "duplicate");
                    Log.Console("添加重复key: 应抛出 ArgumentException");
                    return 2;
                }
                catch (ArgumentException)
                {
                    Log.Debug("测试2通过: 添加重复 key 正确抛出异常");
                }
            }

            // 测试3: 添加 null key 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>();

                try
                {
                    dict.Add(null, 1);
                    Log.Console("添加null key: 应抛出 ArgumentNullException");
                    return 3;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试3通过: 添加 null key 正确抛出异常");
                }
            }

            // 测试4: 添加 null value（允许）
            {
                var dict = new SortedDictionary<int, string>();
                dict.Add(1, null);

                if (dict.Count != 1 || dict[1] != null)
                {
                    Log.Console("添加null value: 应允许 null 值");
                    return 4;
                }

                Log.Debug("测试4通过: 添加 null value");
            }

            // ========== Remove 测试 ==========

            // 测试5: 删除存在的 key
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };

                bool removed = dict.Remove(2);

                if (!removed)
                {
                    Log.Console("删除存在的key: 应返回 true");
                    return 5;
                }

                if (dict.Count != 2)
                {
                    Log.Console($"删除存在的key: Count 应为 2, 实际为 {dict.Count}");
                    return 6;
                }

                if (dict.ContainsKey(2))
                {
                    Log.Console("删除存在的key: 删除后不应包含该 key");
                    return 7;
                }

                Log.Debug("测试5通过: 删除存在的 key");
            }

            // 测试6: 删除不存在的 key
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };

                bool removed = dict.Remove(999);

                if (removed)
                {
                    Log.Console("删除不存在的key: 应返回 false");
                    return 8;
                }

                if (dict.Count != 2)
                {
                    Log.Console($"删除不存在的key: Count 应保持为 2, 实际为 {dict.Count}");
                    return 9;
                }

                Log.Debug("测试6通过: 删除不存在的 key");
            }

            // 测试7: 删除 null key 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>
                {
                    { "a", 1 }
                };

                try
                {
                    dict.Remove(null);
                    Log.Console("删除null key: 应抛出 ArgumentNullException");
                    return 10;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试7通过: 删除 null key 正确抛出异常");
                }
            }

            // ========== ContainsKey 测试 ==========

            // 测试8: ContainsKey 基本功能
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };

                if (!dict.ContainsKey(1))
                {
                    Log.Console("ContainsKey: 应包含 key 1");
                    return 11;
                }

                if (!dict.ContainsKey(2))
                {
                    Log.Console("ContainsKey: 应包含 key 2");
                    return 12;
                }

                if (dict.ContainsKey(999))
                {
                    Log.Console("ContainsKey: 不应包含 key 999");
                    return 13;
                }

                Log.Debug("测试8通过: ContainsKey 基本功能");
            }

            // 测试9: ContainsKey null 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>
                {
                    { "a", 1 }
                };

                try
                {
                    dict.ContainsKey(null);
                    Log.Console("ContainsKey null: 应抛出 ArgumentNullException");
                    return 14;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试9通过: ContainsKey null 正确抛出异常");
                }
            }

            // ========== ContainsValue 测试 ==========

            // 测试10: ContainsValue 基本功能
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };

                if (!dict.ContainsValue("one"))
                {
                    Log.Console("ContainsValue: 应包含 value 'one'");
                    return 15;
                }

                if (!dict.ContainsValue("two"))
                {
                    Log.Console("ContainsValue: 应包含 value 'two'");
                    return 16;
                }

                if (dict.ContainsValue("notexist"))
                {
                    Log.Console("ContainsValue: 不应包含 value 'notexist'");
                    return 17;
                }

                Log.Debug("测试10通过: ContainsValue 基本功能");
            }

            // 测试11: ContainsValue null
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, null },
                    { 3, "three" }
                };

                if (!dict.ContainsValue(null))
                {
                    Log.Console("ContainsValue null: 应包含 null 值");
                    return 18;
                }

                Log.Debug("测试11通过: ContainsValue null");
            }

            // 测试12: ContainsValue 空字典
            {
                var dict = new SortedDictionary<int, string>();

                if (dict.ContainsValue("any"))
                {
                    Log.Console("ContainsValue 空字典: 不应包含任何值");
                    return 19;
                }

                Log.Debug("测试12通过: ContainsValue 空字典");
            }

            // ========== TryGetValue 测试 ==========

            // 测试13: TryGetValue 成功
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };

                if (!dict.TryGetValue(1, out string value) || value != "one")
                {
                    Log.Console("TryGetValue 成功: 应返回 true 和正确的值");
                    return 20;
                }

                Log.Debug("测试13通过: TryGetValue 成功");
            }

            // 测试14: TryGetValue 失败
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" }
                };

                if (dict.TryGetValue(999, out string value))
                {
                    Log.Console("TryGetValue 失败: 应返回 false");
                    return 21;
                }

                if (value != null)
                {
                    Log.Console("TryGetValue 失败: out 参数应为 default");
                    return 22;
                }

                Log.Debug("测试14通过: TryGetValue 失败");
            }

            // 测试15: TryGetValue null key 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>
                {
                    { "a", 1 }
                };

                try
                {
                    dict.TryGetValue(null, out _);
                    Log.Console("TryGetValue null key: 应抛出 ArgumentNullException");
                    return 23;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试15通过: TryGetValue null key 正确抛出异常");
                }
            }

            // 测试16: TryGetValue 获取 null 值
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, null }
                };

                if (!dict.TryGetValue(1, out string value))
                {
                    Log.Console("TryGetValue null值: 应返回 true");
                    return 24;
                }

                if (value != null)
                {
                    Log.Console("TryGetValue null值: 应返回 null");
                    return 25;
                }

                Log.Debug("测试16通过: TryGetValue 获取 null 值");
            }

            Log.Debug("SortedDictionary_BasicOps_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
