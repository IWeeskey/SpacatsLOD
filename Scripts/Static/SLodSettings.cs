using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    [Serializable]
    public class SLodSettings
    {
        public readonly string JobMeasureID = "SLOD JOB";
        public readonly string TotalMeasureID = "SLOD TOTAL";

        public Transform Target;

        public bool PerformLogic = false;
        
        public bool UseEditorCamera = false;
        public bool AsyncLogic = false;
        public bool PerformMeasurements = false;

        public float UpdateRateMS = 16;
        public float CellSize = 1f;

        [SerializeField]
        private int _maxUnitCount = 1_000_000;
        public int MaxUnitCount => _maxUnitCount;

        public List<float> GroupMultipliers = new List<float>();

    }
}
