using CommandLine;

namespace ET.Test
{
    public class RobotCaseArgs: Object
    {
        [Option("Id", Required = false, Default = 0)]
        public int Id { get; set; }
    }
}