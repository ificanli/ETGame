using CommandLine;

namespace ET.Server
{
    public class RobotCaseArgs: Object
    {
        [Option("Case", Required = false, Default = 1)]
        public int Case { get; set; }
        
        [Option("All", Required = false, Default = false)]
        public bool All { get; set; }
    }
}