using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Collections;
using UnityEngine;

namespace Spacats.LOD
{
    [BurstCompile]
    public struct DLodJob : IJobParallelForTransform
    {
        public bool AOTCalculations;
        public float CellSize;
        public float3 TargetPosition;
        public NativeArray<DLodUnitData> UnitsData;
        public NativeList<int2>.ParallelWriter ChangedLodsWriter;
        public NativeParallelMultiHashMap<int3, int>.ParallelWriter CellsWriter;
        [ReadOnly] public NativeList<float> GroupMultipliers;

        public void Execute(int index, TransformAccess transform)
        {
            DLodUnitData unit = UnitsData[index];

            float3 unitPosition = transform.position;
            
            float distance = math.distance(TargetPosition, unitPosition);
            float mult = LodUtils.GetMultiplierFromList(unit.GroupIndex, ref GroupMultipliers);

            int lod = LodUtils.LevelForDistance(distance, in unit.Distances, transform.localScale.x, mult);
            
            if (AOTCalculations)
            {
                int3 cellKey = LodUtils.GetCellKey(unitPosition, CellSize);
                CellsWriter.Add(cellKey, index);
            }
            
            if (lod != unit.CurrentLod)
            {
                unit.CurrentLod = lod;
                ChangedLodsWriter.AddNoResize(new int2(index, lod));
                UnitsData[index] = unit;
            }

            UnitsData[index] = unit;
        }
    }
}

