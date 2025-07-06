using CommandLine;

namespace ET.Server
{
    public class CreateRobotArgs: Object
    {
        [Option("Num", Required = false, Default = 1)]
        public int Num { get; set; }

        [Option("SchedulerType", Required = false, Default = SchedulerType.Parent)]
        public SchedulerType SchedulerType { get; set; }
    }
}