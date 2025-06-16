/*#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Luban;

namespace ET
{
    /// <summary>
    /// 编辑器下加载指定配置 用来判断配资是否存在 或一些简单的数据校验是否合理
    /// 注意这个没有调用REF 所有REF相关的配置会是null的
    /// 如果需要请先手动加载依赖配置 然后调用REF
    /// </summary>
    public static class LubanEditorConfigCategory
    {
        public static T Get<T>(string codeMode = "", string startConfig = "") where T : Singleton<T>, ILubanConfig
        {
            var type = typeof(T);

            T instance = Singleton<T>.Instance;

            if (instance != null)
            {
                return instance;
            }

            var codeModeString    = string.IsNullOrEmpty(codeMode) ? "ClientServer" : codeMode;
            var startConfigString = string.IsNullOrEmpty(startConfig) ? "StartConfig/Localhost" : startConfig;
            var configFilePath    = string.Empty;
            if (LubanHelper.StartConfigs.Contains(type.Name))
            {
                configFilePath = $"{LubanHelper.ConfigResPath}/Server/{startConfigString}/{type.Name}.bytes";
            }
            else
            {
                configFilePath = $"{LubanHelper.ConfigResPath}/{codeModeString}/{type.Name}.bytes";
            }

            try
            {
                instance = (T)Activator.CreateInstance(type, new ByteBuf(File.ReadAllBytes(configFilePath)));

                var instanceField = typeof(Singleton<T>).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceField != null)
                {
                    instanceField.SetValue(null, instance);
                }
                else
                {
                    throw new Exception($"无法找到 Singleton<{type.Name}>.instance 字段");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"加载配置失败: {configFilePath}, {e}");
            }

            return instance;
        }

        public static void ClearAll()
        {
            var assembly = Assembly.GetAssembly(typeof(ILubanConfig));
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(ILubanConfig).IsAssignableFrom(type) &&
                    type.BaseType != null &&
                    type.BaseType.IsGenericType &&
                    type.BaseType.GetGenericTypeDefinition() == typeof(Singleton<>))
                {
                    var genericType   = type.BaseType;
                    var instanceField = genericType.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
                    if (instanceField != null)
                    {
                        instanceField.SetValue(null, null);
                    }
                }
            }
        }
    }
}
#endif*/