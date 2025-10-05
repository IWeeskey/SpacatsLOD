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
        // public int3 CenterCell;
        // public int Radius;
        // public bool IsWholeDynamic;
        // public bool IsSelfDynamic;
        // public int LodUnitIndex;

        public int MaxUnitsInJob;
        public NativeArray<AOIJobUnitData> UnitsData;
        [ReadOnly] public NativeParallelMultiHashMap<int3, int> Cells;
        public NativeList<int> Neighbours;
        
        public void Execute()
        {
            int prevNeighboursCount = 0;
            for (int i = 0; i < MaxUnitsInJob; i++)
            {
                
            }
            //AOIBurstUtils.FillNeighboursList(Cells, Neighbours, CenterCell, Radius, IsWholeDynamic, IsSelfDynamic, LodUnitIndex);
        }
    }
}
