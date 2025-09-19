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
        [ReadOnly] public NativeList<float> GroupMultipliers;

        public void Execute(int index, TransformAccess transform)
        {
            DLodUnitData unit = UnitsData[index];

            float distance = math.distance(TargetPosition, transform.position);
            float mult = LodUtils.GetMultiplierFromList(unit.GroupIndex, ref GroupMultipliers);

            int lod = LodUtils.LevelForDistance(distance, in unit.Distances, transform.localScale.x, mult);

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

