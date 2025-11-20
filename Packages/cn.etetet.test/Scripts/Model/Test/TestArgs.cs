using CommandLine;

namespace ET.Test
{
    public class TestArgs: Object
    {
        [Option("Package", Required = false, Default = ".*")]
        public string Package { get; set; }

        [Option("Name", Required = false, Default = ".*")]
        public string Name { get; set; }
    }
}