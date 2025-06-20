namespace ET
{
    public class ConfigProcessAttribute: BaseAttribute
    {
        public int ConfigType { get; }

        public ConfigProcessAttribute(int configType = 0)
        {
            this.ConfigType = configType;
        }
    }
}