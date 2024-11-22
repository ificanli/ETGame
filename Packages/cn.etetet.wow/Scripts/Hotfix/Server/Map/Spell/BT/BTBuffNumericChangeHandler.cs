namespace ET.Server
{
    public class BTBuffNumericChangeHandler: ABTHandler<BTBuffNumericChange>
    {
        protected override int Run(BTBuffNumericChange node, BTEnv env)
        {
            Buff buff = env.Get<Buff>(node.Buff);
            Unit unit = buff.Parent.GetParent<Unit>();
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            long value = numericComponent.GetAsLong(node.NumericType);
            numericComponent.Set(node.NumericType, value + node.Value);
            
            // buff删除的时候会还原数值
            BuffChangeNumericRecordComponent buffChangeNumericRecordComponent = 
                    buff.GetComponent<BuffChangeNumericRecordComponent>() ??
                    buff.AddComponent<BuffChangeNumericRecordComponent>();
            buffChangeNumericRecordComponent.Add(node.NumericType, node.Value);
            
            return 0;
        }
    }
}