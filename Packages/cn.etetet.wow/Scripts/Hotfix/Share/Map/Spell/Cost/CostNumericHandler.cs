namespace ET
{
    public class CostNumericHandler: CostHandler<CostNumeric>
    {
        protected override int Run(CostNumeric node, Unit unit, SpellConfig spellConfig)
        {
            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            if (numericComponent.Get(node.NumericType) < node.Value)
            {
                switch (node.NumericType)
                {
                    case NumericType.MP:
                        return TextConstDefine.SpellCast_MPNotEnought;
                    case NumericType.HP:
                        return TextConstDefine.SpellCast_HPNotEnought;
                }
            }
            return 0;
        }
    }
}