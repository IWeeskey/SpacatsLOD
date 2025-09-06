using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Collections;

namespace Spacats.LOD
{
    [BurstCompile]
    public struct DLodJob : IJobParallelForTransform
    {
        public float3 TargetPosition;
        public NativeArray<DLodUnitData> UnitsData;
        public NativeList<int2>.ParallelWriter ChangedLodsWriter;
        public void Execute(int index, TransformAccess transform)
        {
            DLodUnitData unit = UnitsData[index];

            float distance = math.distance(TargetPosition, transform.position);

            int lod = LodUtils.LevelForDistance(distance, in unit.Data, transform.localScale.x);

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

