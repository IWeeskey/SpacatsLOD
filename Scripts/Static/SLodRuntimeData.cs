using System;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public class SLodRuntimeData
    {
        public bool InSceneLoading = false;
        public bool JobScheduled = false;
        public bool PerformCellCalculations = false;
        
        public int ChangedLodsCount = 0;
        public float LastUpdateTime = 0f;
        public float CellSize = 1f;
        
        public Vector3 TargetPosition;
        public (double, string) TotalResult;
        public (double, string) JobTimeResult;
        
        public int LastCellsCount = 0;
    }
}
