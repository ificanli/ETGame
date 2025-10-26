using System.Collections.Generic;

namespace ET
{
    public partial class QuestConfigCategory
    {
        private MultiMapSet<long, int> acceptQuest = new();
        private MultiMapSet<long, int> submitQuest = new();
        private MultiMapSet<long, int> allQuest = new();
        
        public override void EndInit()
        {
            foreach (var kv in this.GetAll())
            {
                QuestConfig questConfig = kv.Value;
                this.acceptQuest.Add(questConfig.AcceptNPC, questConfig.Id);
                this.allQuest.Add(questConfig.AcceptNPC, questConfig.Id);
                
                this.submitQuest.Add(questConfig.SubmitNPC, questConfig.Id);
                this.allQuest.Add(questConfig.SubmitNPC, questConfig.Id);
            }
        }
        
        public HashSet<int> GetAcceptQuestsById(long unitId)
        {
            HashSet<int> questIds;
            this.acceptQuest.TryGetValue(unitId, out questIds);
            return questIds;
        }
        
        public HashSet<int> GetSubmitQuestsById(long unitId)
        {
            HashSet<int> questIds;
            this.submitQuest.TryGetValue(unitId, out questIds);
            return questIds;
        }
        
        public HashSet<int> GetAllQuestsById(long unitId)
        {
            HashSet<int> questIds;
            this.allQuest.TryGetValue(unitId, out questIds);
            return questIds;
        }
    }
}