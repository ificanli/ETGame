using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ET
{
    public static class NodeFieldHelper
    {
        public static List<string> GetInputs(NodeView node, System.Type attribute)
        {
            List<string> inputs = new List<string>();
            FieldInfo[] fieldInfos = node.Node.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var btInputFields = fieldInfos.Where(field =>
                    field.GetCustomAttributes(attribute, false).Any());
            foreach (FieldInfo field in btInputFields)
            {
                string v = field.GetValue(node.Node) as string;
                inputs.Add(v);
            }
            return inputs;
        }
    }
}