using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Spacats.LOD
{
    [BurstCompile]
    public struct AOIUnitJob: IJob
    {
        public int3 CenterCell;
        public int Radius;
        public bool IsWholeDynamic;
        public bool IsSelfDynamic;
        public int LodUnitIndex;
        
        [ReadOnly]public NativeParallelMultiHashMap<int3, int> Cells;
        public NativeList<int> Neighbours;
        
        public void Execute()
        {
            AOIBurstUtils.FillNeighboursList(Cells, Neighbours, CenterCell, Radius, IsWholeDynamic, IsSelfDynamic, LodUnitIndex);
        }
    }
}
