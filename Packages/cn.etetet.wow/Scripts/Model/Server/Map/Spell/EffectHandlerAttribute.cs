namespace ET.Server
{
    public class EffectHandlerAttribute: BaseAttribute
    {
        public int Type { get; }

        public EffectHandlerAttribute(int type)
        {
            this.Type = type;
        }
    }
}