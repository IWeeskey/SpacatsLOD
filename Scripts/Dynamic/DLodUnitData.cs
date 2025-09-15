using System;

namespace Spacats.LOD
{
    [Serializable]
    public struct DLodUnitData
    {
        public int UnitIndex;
        public int CurrentLod;
        public LodDistances Distances;
    }
}
