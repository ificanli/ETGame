using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class RogueChoiceQualityComponent : Entity, IAwake
    {
        public List<int> ChoiceQualities { get; set; } = new();
    }
}
