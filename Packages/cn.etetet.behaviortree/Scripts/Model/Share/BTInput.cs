using System;

namespace ET
{
    [EnableClass]
    public class BTInput: Attribute
    {
        public Type Type { get; }

        /// <summary>
        /// 仅用于代码生成标记（例如方法参数标记为输入）。
        /// 行为树节点字段请使用带Type参数的构造函数。
        /// </summary>
        public BTInput()
        {
            this.Type = typeof(void);
        }

        public BTInput(Type type)
        {
            this.Type = type;
        }
    }
}
