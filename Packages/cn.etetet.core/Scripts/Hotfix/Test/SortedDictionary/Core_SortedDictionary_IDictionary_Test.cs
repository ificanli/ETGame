using System;
using System.Collections;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// SortedDictionary IDictionary 接口测试
    /// 测试非泛型 IDictionary 接口的实现
    /// </summary>
    public class Core_SortedDictionary_IDictionary_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            // ========== IDictionary.Add 测试 ==========

            // 测试1: IDictionary.Add 基本功能
            {
                var dict = new SortedDictionary<int, string>();
                IDictionary nonGenericDict = dict;
                nonGenericDict.Add(1, "one");
                nonGenericDict.Add(2, "two");
                if (dict.Count != 2)
                {
                    Log.Console($"IDictionary.Add: Count 应为 2, 实际为 {dict.Count}");
                    return 1;
                }
                if (dict[1] != "one" || dict[2] != "two")
                {
                    Log.Console("IDictionary.Add: 值不正确");
                    return 2;
                }
                Log.Debug("测试1通过: IDictionary.Add 基本功能");
            }

            // 测试2: IDictionary.Add null key 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>();
                IDictionary nonGenericDict = dict;
                try
                {
                    nonGenericDict.Add(null, 1);
                    Log.Console("IDictionary.Add null key: 应抛出 ArgumentNullException");
                    return 3;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试2通过: IDictionary.Add null key 正确抛出异常");
                }
            }

            // 测试3: IDictionary.Add 类型不匹配的 key
            {
                var dict = new SortedDictionary<int, string>();
                IDictionary nonGenericDict = dict;
                try
                {
                    nonGenericDict.Add("string_key", "value");
                    Log.Console("IDictionary.Add 类型不匹配key: 应抛出 ArgumentException");
                    return 4;
                }
                catch (ArgumentException)
                {
                    Log.Debug("测试3通过: IDictionary.Add 类型不匹配 key 正确抛出异常");
                }
            }

            // 测试4: IDictionary.Add 类型不匹配的 value
            {
                var dict = new SortedDictionary<int, string>();
                IDictionary nonGenericDict = dict;
                try
                {
                    nonGenericDict.Add(1, 123);
                    Log.Console("IDictionary.Add 类型不匹配value: 应抛出 ArgumentException");
                    return 5;
                }
                catch (ArgumentException)
                {
                    Log.Debug("测试4通过: IDictionary.Add 类型不匹配 value 正确抛出异常");
                }
            }

            // ========== IDictionary.Contains 测试 ==========

            // 测试5: IDictionary.Contains 基本功能
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                IDictionary nonGenericDict = dict;
                if (!nonGenericDict.Contains(1))
                {
                    Log.Console("IDictionary.Contains: 应包含 key 1");
                    return 6;
                }
                if (nonGenericDict.Contains(999))
                {
                    Log.Console("IDictionary.Contains: 不应包含 key 999");
                    return 7;
                }
                Log.Debug("测试5通过: IDictionary.Contains 基本功能");
            }

            // 测试6: IDictionary.Contains 类型不匹配的 key
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                IDictionary nonGenericDict = dict;
                if (nonGenericDict.Contains("string_key"))
                {
                    Log.Console("IDictionary.Contains 类型不匹配: 应返回 false");
                    return 8;
                }
                Log.Debug("测试6通过: IDictionary.Contains 类型不匹配返回 false");
            }

            // 测试7: IDictionary.Contains null key 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int> { { "a", 1 } };
                IDictionary nonGenericDict = dict;
                try
                {
                    nonGenericDict.Contains(null);
                    Log.Console("IDictionary.Contains null key: 应抛出 ArgumentNullException");
                    return 9;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试7通过: IDictionary.Contains null key 正确抛出异常");
                }
            }

            // ========== IDictionary.Remove 测试 ==========

            // 测试8: IDictionary.Remove 基本功能
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                IDictionary nonGenericDict = dict;
                nonGenericDict.Remove(1);
                if (dict.Count != 1)
                {
                    Log.Console($"IDictionary.Remove: Count 应为 1, 实际为 {dict.Count}");
                    return 10;
                }
                if (dict.ContainsKey(1))
                {
                    Log.Console("IDictionary.Remove: 不应包含已删除的 key");
                    return 11;
                }
                Log.Debug("测试8通过: IDictionary.Remove 基本功能");
            }

            // 测试9: IDictionary.Remove 类型不匹配的 key（不抛异常，静默忽略）
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                IDictionary nonGenericDict = dict;
                nonGenericDict.Remove("string_key");
                if (dict.Count != 1)
                {
                    Log.Console($"IDictionary.Remove 类型不匹配: Count 应保持为 1, 实际为 {dict.Count}");
                    return 12;
                }
                Log.Debug("测试9通过: IDictionary.Remove 类型不匹配静默忽略");
            }

            // ========== IDictionary 索引器测试 ==========

            // 测试10: IDictionary 索引器 get
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                IDictionary nonGenericDict = dict;
                object value = nonGenericDict[1];
                if ((string)value != "one")
                {
                    Log.Console($"IDictionary索引器get: 应为 'one', 实际为 '{value}'");
                    return 13;
                }
                Log.Debug("测试10通过: IDictionary 索引器 get");
            }

            // 测试11: IDictionary 索引器 get 不存在的 key（返回 null）
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                IDictionary nonGenericDict = dict;
                object value = nonGenericDict[999];
                if (value != null)
                {
                    Log.Console($"IDictionary索引器get不存在: 应为 null, 实际为 '{value}'");
                    return 14;
                }
                Log.Debug("测试11通过: IDictionary 索引器 get 不存在的 key 返回 null");
            }

            // 测试12: IDictionary 索引器 get 类型不匹配的 key（返回 null）
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                IDictionary nonGenericDict = dict;
                object value = nonGenericDict["string_key"];
                if (value != null)
                {
                    Log.Console($"IDictionary索引器get类型不匹配: 应为 null, 实际为 '{value}'");
                    return 15;
                }
                Log.Debug("测试12通过: IDictionary 索引器 get 类型不匹配返回 null");
            }

            // 测试13: IDictionary 索引器 set
            {
                var dict = new SortedDictionary<int, string>();
                IDictionary nonGenericDict = dict;
                nonGenericDict[1] = "one";
                nonGenericDict[2] = "two";
                if (dict.Count != 2)
                {
                    Log.Console($"IDictionary索引器set: Count 应为 2, 实际为 {dict.Count}");
                    return 16;
                }
                if (dict[1] != "one" || dict[2] != "two")
                {
                    Log.Console("IDictionary索引器set: 值不正确");
                    return 17;
                }
                Log.Debug("测试13通过: IDictionary 索引器 set");
            }

            // 测试14: IDictionary 索引器 set 更新已存在的 key
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                IDictionary nonGenericDict = dict;
                nonGenericDict[1] = "ONE";
                if (dict[1] != "ONE")
                {
                    Log.Console($"IDictionary索引器set更新: 应为 'ONE', 实际为 '{dict[1]}'");
                    return 18;
                }
                Log.Debug("测试14通过: IDictionary 索引器 set 更新已存在的 key");
            }

            // 测试15: IDictionary 索引器 set null key 应抛出 ArgumentNullException
            {
                var dict = new SortedDictionary<string, int>();
                IDictionary nonGenericDict = dict;
                try
                {
                    nonGenericDict[null] = 1;
                    Log.Console("IDictionary索引器set null key: 应抛出 ArgumentNullException");
                    return 19;
                }
                catch (ArgumentNullException)
                {
                    Log.Debug("测试15通过: IDictionary 索引器 set null key 正确抛出异常");
                }
            }

            // 测试16: IDictionary 索引器 set 类型不匹配
            {
                var dict = new SortedDictionary<int, string>();
                IDictionary nonGenericDict = dict;
                try
                {
                    nonGenericDict["string_key"] = "value";
                    Log.Console("IDictionary索引器set类型不匹配key: 应抛出 ArgumentException");
                    return 20;
                }
                catch (ArgumentException) { }
                try
                {
                    nonGenericDict[1] = 123;
                    Log.Console("IDictionary索引器set类型不匹配value: 应抛出 ArgumentException");
                    return 21;
                }
                catch (ArgumentException) { }
                Log.Debug("测试16通过: IDictionary 索引器 set 类型不匹配正确抛出异常");
            }

            // ========== IDictionary 属性测试 ==========

            // 测试17: IsFixedSize 和 IsReadOnly
            {
                var dict = new SortedDictionary<int, string>();
                IDictionary nonGenericDict = dict;
                if (nonGenericDict.IsFixedSize)
                {
                    Log.Console("IDictionary.IsFixedSize: 应为 false");
                    return 22;
                }
                if (nonGenericDict.IsReadOnly)
                {
                    Log.Console("IDictionary.IsReadOnly: 应为 false");
                    return 23;
                }
                Log.Debug("测试17通过: IsFixedSize 和 IsReadOnly");
            }

            // 测试18: IDictionary.Keys 和 IDictionary.Values
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                IDictionary nonGenericDict = dict;
                ICollection keys = nonGenericDict.Keys;
                ICollection values = nonGenericDict.Values;
                if (keys.Count != 2)
                {
                    Log.Console($"IDictionary.Keys.Count: 应为 2, 实际为 {keys.Count}");
                    return 24;
                }
                if (values.Count != 2)
                {
                    Log.Console($"IDictionary.Values.Count: 应为 2, 实际为 {values.Count}");
                    return 25;
                }
                Log.Debug("测试18通过: IDictionary.Keys 和 IDictionary.Values");
            }

            // ========== ICollection 属性测试 ==========

            // 测试19: ICollection.IsSynchronized 和 SyncRoot
            {
                var dict = new SortedDictionary<int, string>();
                ICollection collection = dict;
                if (collection.IsSynchronized)
                {
                    Log.Console("ICollection.IsSynchronized: 应为 false");
                    return 26;
                }
                if (collection.SyncRoot == null)
                {
                    Log.Console("ICollection.SyncRoot: 不应为 null");
                    return 27;
                }
                Log.Debug("测试19通过: ICollection.IsSynchronized 和 SyncRoot");
            }

            // 测试20: ICollection.CopyTo
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 2, "two" },
                    { 1, "one" }
                };
                ICollection collection = dict;
                var array = new object[3];
                collection.CopyTo(array, 1);
                var kvp1 = (KeyValuePair<int, string>)array[1];
                var kvp2 = (KeyValuePair<int, string>)array[2];
                if (kvp1.Key != 1 || kvp1.Value != "one")
                {
                    Log.Console("ICollection.CopyTo: 位置 1 数据不正确");
                    return 28;
                }
                if (kvp2.Key != 2 || kvp2.Value != "two")
                {
                    Log.Console("ICollection.CopyTo: 位置 2 数据不正确");
                    return 29;
                }
                Log.Debug("测试20通过: ICollection.CopyTo");
            }

            // ========== ICollection<KeyValuePair> 接口测试 ==========

            // 测试21: ICollection<KeyValuePair>.Add
            {
                var dict = new SortedDictionary<int, string>();
                ICollection<KeyValuePair<int, string>> collection = dict;
                collection.Add(new KeyValuePair<int, string>(1, "one"));
                if (dict.Count != 1 || dict[1] != "one")
                {
                    Log.Console("ICollection<KVP>.Add: 添加失败");
                    return 30;
                }
                Log.Debug("测试21通过: ICollection<KeyValuePair>.Add");
            }

            // 测试22: ICollection<KeyValuePair>.Contains
            {
                var dict = new SortedDictionary<int, string> { { 1, "one" } };
                ICollection<KeyValuePair<int, string>> collection = dict;
                if (!collection.Contains(new KeyValuePair<int, string>(1, "one")))
                {
                    Log.Console("ICollection<KVP>.Contains: 应包含 (1, 'one')");
                    return 31;
                }
                if (collection.Contains(new KeyValuePair<int, string>(1, "wrong")))
                {
                    Log.Console("ICollection<KVP>.Contains: 不应包含 (1, 'wrong')");
                    return 32;
                }
                if (collection.Contains(new KeyValuePair<int, string>(999, "one")))
                {
                    Log.Console("ICollection<KVP>.Contains: 不应包含 (999, 'one')");
                    return 33;
                }
                Log.Debug("测试22通过: ICollection<KeyValuePair>.Contains");
            }

            // 测试23: ICollection<KeyValuePair>.Remove
            {
                var dict = new SortedDictionary<int, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                ICollection<KeyValuePair<int, string>> collection = dict;
                bool removed = collection.Remove(new KeyValuePair<int, string>(1, "one"));
                if (!removed || dict.Count != 1)
                {
                    Log.Console("ICollection<KVP>.Remove: 删除失败");
                    return 34;
                }
                removed = collection.Remove(new KeyValuePair<int, string>(2, "wrong"));
                if (removed || dict.Count != 1)
                {
                    Log.Console("ICollection<KVP>.Remove: value不匹配时不应删除");
                    return 35;
                }
                Log.Debug("测试23通过: ICollection<KeyValuePair>.Remove");
            }

            // 测试24: ICollection<KeyValuePair>.IsReadOnly
            {
                var dict = new SortedDictionary<int, string>();
                ICollection<KeyValuePair<int, string>> collection = dict;
                if (collection.IsReadOnly)
                {
                    Log.Console("ICollection<KVP>.IsReadOnly: 应为 false");
                    return 36;
                }
                Log.Debug("测试24通过: ICollection<KeyValuePair>.IsReadOnly");
            }

            Log.Debug("SortedDictionary_IDictionary_Test 全部通过");
            return ErrorCode.ERR_Success;
        }
    }
}
