using System;
using Unity.Mathematics;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public struct SLodUnitData
    {
        [HideInInspector] public int UnitIndex;
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public float Scale;
        [HideInInspector] public quaternion Rotation;

        public int CurrentLod;
        public bool CuboidCalculations;
        public LodDistances Distances;
        public Cuboid CuboidData;
    }
}
