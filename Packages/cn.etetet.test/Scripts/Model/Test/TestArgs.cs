using CommandLine;

namespace ET.Test
{
    public class TestArgs: Object
    {
        [Option('n', "Name", Required = false, Default = ".*")]
        public string Name { get; set; }
    }
}