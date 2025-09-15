using System;

namespace Spacats.LOD
{
    [Serializable]
    public struct LodDistances
    {
        public float Lod0;
        public float Lod1;
        public float Lod2;
        public float Lod3;
        public float Lod4;

        public float GetByIndex(int index)
        {
            switch (index)
            {
                case 0: return Lod0;
                case 1: return Lod1;
                case 2: return Lod2;
                case 3: return Lod3;
                case 4: return Lod4;
            }

            if (index >= 5) return Lod4;
            return Lod0;
        }
    }
}
