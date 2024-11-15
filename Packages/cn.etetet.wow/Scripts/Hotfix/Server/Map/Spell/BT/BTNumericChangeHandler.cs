namespace ET.Server
{
    public class BTNumericChangeHandler: ABTHandler<BTNumericChange>
    {
        protected override bool Run(BTNumericChange node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            long value = numericComponent.GetAsLong(node.NumericType);
            numericComponent.Set(node.NumericType, value + node.Value);
            return true;
        }
    }
}