using System;
using Unity.Mathematics;

namespace Spacats.LOD
{
    [Serializable]
    public struct Cuboid
    {
        public float3 Size;
        public float3 HalfSize => Size * 0.5f;

        public Cuboid(float width, float height, float depth)
        {
            Size = new float3(width, height, depth);
        }

        public Cuboid(float3 size)
        {
            Size = size;
        }
    }
}
