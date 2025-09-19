using System;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public struct DLodUnitData
    {
        [HideInInspector] public int UnitIndex;
        [HideInInspector] public int GroupIndex;

        public int CurrentLod;
        public LodDistances Distances;
    }
}
