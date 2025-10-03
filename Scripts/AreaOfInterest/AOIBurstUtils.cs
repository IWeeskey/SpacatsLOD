using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Spacats.LOD
{
    [BurstCompile]
    public class AOIBurstUtils
    {
        public static void FillNeighboursList(NativeParallelMultiHashMap<int3, int> cells, NativeList<int> neighbours, 
            int3 centerCell, int radius, bool isWholeDynamic, bool isSelfDynamic, int lodUnitIndex)
        {
            int radiussq = radius * radius;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int3 offset = new int3(x, y, z);
                        int distSq = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;
                        if (distSq > radiussq) continue;
                        
                        int3 checkCell = centerCell + offset;
                        ProcessCell(cells, neighbours, checkCell, isWholeDynamic, isSelfDynamic, lodUnitIndex);
                    }
                }
            }
        }
        
        
        private static void ProcessCell(NativeParallelMultiHashMap<int3, int> cells, NativeList<int> neighbours, 
            int3 checkCell, bool isWholeDynamic, bool isSelfDynamic, int lodUnitIndex)
        {
            if (cells.TryGetFirstValue(checkCell, out int value, out var it))
            {
                do
                {
                    if (isSelfDynamic && isWholeDynamic && lodUnitIndex == value) continue;
                    if (!isSelfDynamic && !isWholeDynamic && lodUnitIndex == value) continue;
                    
                    neighbours.Add(value);
                }
                while (cells.TryGetNextValue(out value, ref it));
            }
        }
    }
}
