using System;
using System.Collections.Generic;

namespace ET.Test
{
    public class TestDispatcher: Singleton<TestDispatcher>, ISingletonAwake
    {
        private SortedDictionary<string, ITestHandler> dispatcher = new(); 
        
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (TestAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(TestAttribute), true);
                if (attrs.Length == 0)
                {
                    continue;
                }
                
                object obj = Activator.CreateInstance(type);

                ITestHandler iTestHandler = obj as ITestHandler;
                if (iTestHandler == null)
                {
                    throw new Exception($"handler not inherit ITestHandler class: {obj.GetType().FullName}");
                }

                this.dispatcher.Add(iTestHandler.GetType().Name, iTestHandler);
            }
        }

        /// <summary>
        /// 获取匹配的测试处理器列表
        /// </summary>
        /// <param name="name">处理器名正则表达式，null或空表示匹配所有处理器</param>
        /// <returns>匹配的测试处理器列表</returns>
        public List<ITestHandler> Get(string name)
        {
            List<ITestHandler> result = new();
            
            // 创建正则表达式
            System.Text.RegularExpressions.Regex nameRegex = new(name);
            
            // 遍历所有包
            foreach ((string testName, ITestHandler iTestHandler) in this.dispatcher)
            {
                // 如果指定了name正则，检查是否匹配
                if (!nameRegex.IsMatch(testName))
                {
                    continue;
                }
                
                result.Add(iTestHandler);
            }

            if (result.Count == 0)
            {
                return result;
            }
            
            return result;
        }
    }
}
