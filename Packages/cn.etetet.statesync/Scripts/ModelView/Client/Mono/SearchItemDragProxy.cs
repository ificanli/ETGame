using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class SearchItemDragProxy : MonoBehaviour
    {
        public EntityRef<SearchPanelComponent> PanelRef;
        public long ItemId;
        public bool IsBag;

        public SearchPanelComponent Panel => this.PanelRef;
    }
}
