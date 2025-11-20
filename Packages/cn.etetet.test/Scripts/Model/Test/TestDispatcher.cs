using System;
using System.Collections.Generic;

namespace ET.Test
{
    public class TestDispatcher: Singleton<TestDispatcher>, ISingletonAwake
    {
        public SortedDictionary<string, SortedDictionary<string, ITestHandler>> dispatcher = new(); 
        
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (TestAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(TestAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                TestAttribute testAttribute = (TestAttribute)attrs[0];
                
                object obj = Activator.CreateInstance(type);

                ITestHandler iTestHandler = obj as ITestHandler;
                if (iTestHandler == null)
                {
                    throw new Exception($"handler not inherit ITestHandler class: {obj.GetType().FullName}");
                }

                string packageName = PackageEnum.Instance.GetStringByValue(testAttribute.Package);
                if (!this.dispatcher.TryGetValue(packageName, out var dict))
                {
                    dict = new SortedDictionary<string, ITestHandler>();
                    this.dispatcher.Add(packageName, dict);
                }
                dict.Add(type.Name, iTestHandler);
            }
        }

        /// <summary>
        /// 获取匹配的测试处理器列表
        /// </summary>
        /// <param name="package">包名正则表达式，null或空表示匹配所有包</param>
        /// <param name="name">处理器名正则表达式，null或空表示匹配所有处理器</param>
        /// <returns>匹配的测试处理器列表</returns>
        public List<ITestHandler> Get(string package, string name)
        {
            List<ITestHandler> result = new List<ITestHandler>();
            
            // 创建正则表达式
            System.Text.RegularExpressions.Regex packageRegex = null;
            System.Text.RegularExpressions.Regex nameRegex = null;
            
            try
            {
                if (!string.IsNullOrEmpty(package))
                {
                    packageRegex = new System.Text.RegularExpressions.Regex(package);
                }
                
                if (!string.IsNullOrEmpty(name))
                {
                    nameRegex = new System.Text.RegularExpressions.Regex(name);
                }
            }
            catch (Exception e)
            {
                Log.Error($"invalid regex pattern, package: {package}, name: {name}, error: {e}");
                return result;
            }
            
            // 遍历所有包
            foreach (var kvp in this.dispatcher)
            {
                string packageName = kvp.Key;
                
                // 如果指定了package正则，检查是否匹配
                if (packageRegex != null && !packageRegex.IsMatch(packageName))
                {
                    continue;
                }
                
                // 遍历包内的所有handler
                foreach (var handlerKvp in kvp.Value)
                {
                    string handlerName = handlerKvp.Key;
                    
                    // 如果指定了name正则，检查是否匹配
                    if (nameRegex != null && !nameRegex.IsMatch(handlerName))
                    {
                        continue;
                    }
                    
                    result.Add(handlerKvp.Value);
                }
            }
            
            return result;
        }
    }
}