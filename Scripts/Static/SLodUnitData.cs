using System;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public struct SLodUnitData
    {
        public int CurrentLod;
        public bool CuboidCalculations;
        [HideInInspector] public int UnitIndex;
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public float Scale;
        public LodDistances Distances;
        public Cuboid CuboidData;
    }
}
