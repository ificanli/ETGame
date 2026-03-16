using System.Collections.Generic;
using DotRecast.Detour;

namespace ET
{
    [EnableClass]
    public class PolyRefCollectQuery : IDtPolyQuery
    {
        public readonly HashSet<long> PolyRefs = new();

        public void Process(DtMeshTile tile, DtPoly poly, long refs)
        {
            this.PolyRefs.Add(refs);
        }
    }
}
