using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Spacats.LOD
{
    [BurstCompile]
    public struct SLodJob : IJobParallelFor
    {
        public float3 TargetPosition;
        public NativeArray<SLodUnitData> UnitsData;
        public NativeList<int2>.ParallelWriter ChangedLodsWriter;
        public void Execute(int index)
        {
            SLodUnitData unit = UnitsData[index];

            float distance = math.distance(TargetPosition, unit.Position);

            int lod = LodUtils.LevelForDistance(distance, in unit.Data, 1f);

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

