namespace ET
{
    public enum ConfigType
    {
        Luban,
        Bson,
    }
    
    public class ConfigProcessAttribute: BaseAttribute
    {
        public ConfigType ConfigType { get; }

        public ConfigProcessAttribute(ConfigType configType = ConfigType.Luban)
        {
            this.ConfigType = configType;
        }
    }
}