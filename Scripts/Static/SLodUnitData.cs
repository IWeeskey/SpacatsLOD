using System;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public struct SLodUnitData
    {
        public int CurrentLod;
        [HideInInspector] public int UnitIndex;
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public float Scale;
        public LodData Data;
    }
}
