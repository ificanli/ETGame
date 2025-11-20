namespace ET.Test
{
    public class TestAttribute: BaseAttribute
    {
        public int Package { get; }
        
        public TestAttribute(int package)
        {
            this.Package = package;
        }
    }
}