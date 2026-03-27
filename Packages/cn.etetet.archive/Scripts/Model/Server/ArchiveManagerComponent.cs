using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class ArchiveManagerComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<string, long> AccountToArchiveId { get; set; } = new();
    }
}
