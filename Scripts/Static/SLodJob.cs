using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Spacats.LOD
{
    [BurstCompile]
    public struct SLodJob : IJobParallelFor
    {
        public float3 TargetPosition;
        public NativeArray<SLodUnitData> UnitsData;
        public NativeList<int2>.ParallelWriter ChangedLodsWriter;
        [ReadOnly] public NativeList<float> GroupMultipliers;

        public void Execute(int index)
        {
            SLodUnitData unit = UnitsData[index];
            float distance = 0;
            float mult = LodUtils.GetMultiplierFromList(unit.GroupIndex, ref GroupMultipliers);

            if (unit.CuboidCalculations) distance = LodUtils.DistanceToOBB(TargetPosition, unit.Position, unit.CuboidData, unit.Rotation);
            else distance = math.distance(TargetPosition, unit.Position);

            int lod = LodUtils.LevelForDistance(distance, in unit.Distances, unit.Scale, mult);

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

