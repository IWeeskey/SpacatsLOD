using System;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public class SLodSettings
    {
        public Transform Target;

        public bool PerformLogic = false;

        public bool UseEditorCamera = false;
        public bool AsyncLogic = false;
        public bool PerformMeasurements = false;

        public float UpdateRateMS = 16;

        [SerializeField]
        private int _maxUnitCount = 1_000_000;
        public int MaxUnitCount => _maxUnitCount;
        public readonly string JobMeasureID = "DLOD JOB";
        public readonly string TotalMeasureID = "DLOD TOTAL";
    }
}
