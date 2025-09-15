
using Unity.Burst;

namespace Spacats.LOD
{
    [BurstCompile]
    public class LodUtils
    {
        public static int LevelForDistance(float d, in LodDistances distances, float mult)
        {
            if (d <= distances.Lod0 * mult) return 0;
            else if (d <= distances.Lod1 * mult) return 1;
            else if (d <= distances.Lod2 * mult) return 2;
            else if (d <= distances.Lod3 * mult) return 3;
            else if (d <= distances.Lod4 * mult) return 4;
            else return 5;
        }
    }
}
