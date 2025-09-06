
using Unity.Burst;

namespace Spacats.LOD
{
    [BurstCompile]
    public class LodUtils
    {
        public static int LevelForDistance(float d, in LodData dta, float mult)
        {
            if (d <= dta.Lod0 * mult) return 0;
            else if (d <= dta.Lod1 * mult) return 1;
            else if (d <= dta.Lod2 * mult) return 2;
            else if (d <= dta.Lod3 * mult) return 3;
            else if (d <= dta.Lod4 * mult) return 4;
            else return 5;
        }
    }
}
