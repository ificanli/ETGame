using CommandLine;

namespace ET.Server
{
    public class RemoveRobotArgs: Object
    {
        [Option("Account", Required = false, Default = 1)]
        public string Account { get; set; }
    }
}