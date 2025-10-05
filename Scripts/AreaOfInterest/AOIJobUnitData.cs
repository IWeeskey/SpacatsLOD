
using Unity.Mathematics;

namespace Spacats.LOD
{
    public struct AOIJobUnitData
    {
        public int3 CenterCell;
        public int Radius;
        public bool IsWholeDynamic;
        public bool IsSelfDynamic;
        public int LodUnitIndex;
        
        public int AOIUnitIndex;
        public int NeighboursIndex;
    }
}
