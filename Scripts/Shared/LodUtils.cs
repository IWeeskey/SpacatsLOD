using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Spacats.LOD
{
    [BurstCompile]
    public class LodUtils
    {
        public static float DistanceToOBB(float3 target, float3 cuboidCenter, in Cuboid cuboid, quaternion rotation)
        {
            float3 localPoint = mul(conjugate(rotation), target - cuboidCenter);

            float3 halfSize = cuboid.HalfSize;

            float3 delta = max(abs(localPoint) - halfSize, 0);

            return length(delta);
        }

        public static int LevelForDistance(float distance, in LodDistances distances, float scale, float mult)
        {
            float totalMult = scale * mult;
            if (distance <= distances.Lod0 * totalMult) return 0;
            else if (distance <= distances.Lod1 * totalMult) return 1;
            else if (distance <= distances.Lod2 * totalMult) return 2;
            else if (distance <= distances.Lod3 * totalMult) return 3;
            else if (distance <= distances.Lod4 * totalMult) return 4;
            else return 5;
        }

        public static float GetMultiplierFromList(int index, ref NativeList<float> list)
        {
            float mult = 1f;

            if (index >= 0 && index < list.Length)
            {
                mult = list[index];
            }

            if (mult <= 0f) mult = 1f;

            return mult;
        }
    }
}
