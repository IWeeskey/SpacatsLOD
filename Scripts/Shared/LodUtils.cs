using Unity.Burst;
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


        public static int LevelForDistance(float distance, in LodDistances distances, float mult)
        {
            if (distance <= distances.Lod0 * mult) return 0;
            else if (distance <= distances.Lod1 * mult) return 1;
            else if (distance <= distances.Lod2 * mult) return 2;
            else if (distance <= distances.Lod3 * mult) return 3;
            else if (distance <= distances.Lod4 * mult) return 4;
            else return 5;
        }
    }
}
