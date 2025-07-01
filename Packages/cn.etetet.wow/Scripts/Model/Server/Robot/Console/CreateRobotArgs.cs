using CommandLine;

namespace ET.Server
{
    public class CreateRobotArgs: Object
    {
        [Option("Num", Required = false, Default = 1)]
        public int Num { get; set; }

        [Option("IsSubFiber", Required = false, Default = false)]
        public bool IsSubFiber { get; set; }
    }
}