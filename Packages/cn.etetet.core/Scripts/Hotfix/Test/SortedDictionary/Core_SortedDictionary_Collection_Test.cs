using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary 集合操作测试
    /// 测试 Keys, Values 集合和 CopyTo 方法
    /// </summary>
    public class Core_SortedDictionary_Collection_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // ========== Keys 集合测试 ==========

            // 测试1: Keys.Count
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                var keys = dict.Keys;
                if (keys.Count != 3)
                {
                    Log.Console($"Keys.Count: 应为 3, 实际为 {keys.Count}");
                    return 1;
                }
                Log.Debug("测试1通过: Keys.Count");
            }

            // 测试2: Keys 遍历顺序
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
                        Log.Console($"Keys遍历顺序: 位置 {index} 期望 {expectedKeys[index]}, 实际 {key}");
                        return 2;
                    }
                    index++;
                }
                Log.Debug("测试2通过: Keys 遍历顺序");
            }

            // 测试3: Keys.Contains
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };
                var keys = dict.Keys;
                if (!keys.Contains(1) || !keys.Contains(2) || !keys.Contains(3))
                {
                    Log.Console("Keys.Contains: 应包含所有已添加的 key");
                    return 3;
                }
                if (keys.Contains(999))
                {
                    Log.Console("Keys.Contains: 不应包含未添加的 key");
                    return 4;
                }
                Log.Debug("测试3通过: Keys.Contains");
            }

            // 测试4: Keys 是只读的
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                ICollection<int> keysCollection = dict.Keys;
                if (!keysCollection.IsReadOnly)
                {
                    Log.Console("Keys.IsReadOnly: 应为 true");
                    return 5;
                }
                try
                {
                    keysCollection.Add(2);
                    Log.Console("Keys.Add: 应抛出 NotSupportedException");
                    return 6;
                }
                catch (NotSupportedException) { }
                try
                {
                    keysCollection.Remove(1);
                    Log.Console("Keys.Remove: 应抛出 NotSupportedException");
                    return 7;
                }
                catch (NotSupportedException) { }
                try
                {
                    keysCollection.Clear();
                    Log.Console("Keys.Clear: 应抛出 NotSupportedException");
                    return 8;
                }
                catch (NotSupportedException) { }
                Log.Debug("测试4通过: Keys 是只读的");
            }

            // 测试5: Keys.CopyTo
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                var keyArray = new int[5];
                dict.Keys.CopyTo(keyArray, 1);
                if (keyArray[0] != 0)
                {
                    Log.Console("Keys.CopyTo: 位置 0 应未填充");
                    return 9;
                }
                if (keyArray[1] != 1 || keyArray[2] != 2 || keyArray[3] != 3)
                {
                    Log.Console("Keys.CopyTo: 复制的数据不正确");
                    return 10;
                }
                Log.Debug("测试5通过: Keys.CopyTo");
            }

            // ========== Values 集合测试 ==========

            // 测试6: Values.Count
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                var values = dict.Values;
                if (values.Count != 3)
                {
                    Log.Console($"Values.Count: 应为 3, 实际为 {values.Count}");
                    return 11;
                }
                Log.Debug("测试6通过: Values.Count");
            }

            // 测试7: Values 遍历顺序（按 key 排序）
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
                        Log.Console($"Values遍历顺序: 位置 {index} 期望 '{expectedValues[index]}', 实际 '{value}'");
                        return 12;
                    }
                    index++;
                }
                Log.Debug("测试7通过: Values 遍历顺序");
            }

            // 测试8: Values.Contains (通过 ICollection<TValue>)
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                ICollection<string> valuesCollection = dict.Values;
                if (!valuesCollection.Contains("one") || !valuesCollection.Contains("two"))
                {
                    Log.Console("Values.Contains: 应包含所有已添加的 value");
                    return 13;
                }
                if (valuesCollection.Contains("notexist"))
                {
                    Log.Console("Values.Contains: 不应包含未添加的 value");
                    return 14;
                }
                Log.Debug("测试8通过: Values.Contains");
            }

            // 测试9: Values 是只读的
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                ICollection<string> valuesCollection = dict.Values;
                if (!valuesCollection.IsReadOnly)
                {
                    Log.Console("Values.IsReadOnly: 应为 true");
                    return 15;
                }
                try
                {
                    valuesCollection.Add("two");
                    Log.Console("Values.Add: 应抛出 NotSupportedException");
                    return 16;
                }
                catch (NotSupportedException) { }
                Log.Debug("测试9通过: Values 是只读的");
            }

            // 测试10: Values.CopyTo
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                var valueArray = new string[5];
                dict.Values.CopyTo(valueArray, 1);
                if (valueArray[0] != null)
                {
                    Log.Console("Values.CopyTo: 位置 0 应未填充");
                    return 17;
                }
                if (valueArray[1] != "one" || valueArray[2] != "two" || valueArray[3] != "three")
                {
                    Log.Console("Values.CopyTo: 复制的数据不正确");
                    return 18;
                }
                Log.Debug("测试10通过: Values.CopyTo");
            }

            // ========== CopyTo 测试 ==========

            // 测试11: 字典 CopyTo 基本功能
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 3, "three" },
                    { 1, "one" },
                    { 2, "two" }
                };
                var array = new KeyValuePair<int, string>[5];
                dict.CopyTo(array, 1);
                if (array[0].Key != 0 || array[0].Value != null)
                {
                    Log.Console("CopyTo: 位置 0 应未填充");
                    return 19;
                }
                if (array[1].Key != 1 || array[1].Value != "one")
                {
                    Log.Console("CopyTo: 位置 1 数据不正确");
                    return 20;
                }
                if (array[2].Key != 2 || array[2].Value != "two")
                {
                    Log.Console("CopyTo: 位置 2 数据不正确");
                    return 21;
                }
                if (array[3].Key != 3 || array[3].Value != "three")
                {
                    Log.Console("CopyTo: 位置 3 数据不正确");
                    return 22;
                }
                Log.Debug("测试11通过: 字典 CopyTo 基本功能");
            }

            // 测试12: CopyTo null 数组应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                try
                {
                    dict.CopyTo(null, 0);
                    Log.Console("CopyTo null数组: 应抛出 ArgumentNullException");
                    return 23;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试12通过: CopyTo null 数组抛出异常");
                }
            }

            // 测试13: CopyTo 负索引应抛出 ArgumentOutOfRangeException
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                var array = new KeyValuePair<int, string>[5];
                try
                {
                    dict.CopyTo(array, -1);
                    Log.Console("CopyTo 负索引: 应抛出 ArgumentOutOfRangeException");
                    return 24;
                }
                catch (ArgumentOutOfRangeException)
                {
                    Log.Debug("测试13通过: CopyTo 负索引抛出异常");
                }
            }

            // 测试14: CopyTo 空间不足应抛出 ArgumentException
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" },
                    { 3, "three" }
                };
                var array = new KeyValuePair<int, string>[2];
                try
                {
                    dict.CopyTo(array, 0);
                    Log.Console("CopyTo 空间不足: 应抛出 ArgumentException");
                    return 25;
                }
                catch (ArgumentException)
                {
                    Log.Debug("测试14通过: CopyTo 空间不足抛出异常");
                }
            }

            // 测试15: Keys 和 Values 与字典同步更新
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                var keys = dict.Keys;
                var values = dict.Values;
                dict.Add(2, "two");
                if (keys.Count != 2)
                {
                    Log.Console($"Keys同步更新: 添加后 Count 应为 2, 实际为 {keys.Count}");
                    return 26;
                }
                if (values.Count != 2)
                {
                    Log.Console($"Values同步更新: 添加后 Count 应为 2, 实际为 {values.Count}");
                    return 27;
                }
                dict.Remove(1);
                if (keys.Count != 1)
                {
                    Log.Console($"Keys同步更新: 删除后 Count 应为 1, 实际为 {keys.Count}");
                    return 28;
                }
                if (values.Count != 1)
                {
                    Log.Console($"Values同步更新: 删除后 Count 应为 1, 实际为 {values.Count}");
                    return 29;
                }
                Log.Debug("测试15通过: Keys 和 Values 与字典同步更新");
            }

            // 测试16: 空字典的 Keys 和 Values CopyTo
            {
                var dict = new SortedDictionary<int, string>();
                var keyArray = new int[1];
                dict.Keys.CopyTo(keyArray, 0);
                var valueArray = new string[1];
                dict.Values.CopyTo(valueArray, 0);
                Log.Debug("测试16通过: 空字典的 Keys 和 Values CopyTo");
            }

            // 测试17: ICollection.CopyTo (非泛型)
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                ICollection keysCollection = dict.Keys;
                var objArray = new object[3];
                keysCollection.CopyTo(objArray, 1);
                if ((int)objArray[1] != 1 || (int)objArray[2] != 2)
                {
                    Log.Console("ICollection.CopyTo: 数据不正确");
                    return 30;
                }
                Log.Debug("测试17通过: ICollection.CopyTo (非泛型)");
            }

            Log.Debug("SortedDictionary_Collection_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
