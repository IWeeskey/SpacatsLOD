using System;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public class DLodRuntimeData
    {
        public bool JobScheduled = false;

        public int ChangedLodsCount = 0;
        public float LastUpdateTime = 0f;

        public Vector3 TargetPosition;
        public (double, string) MeasurementsResult;
    }
}
