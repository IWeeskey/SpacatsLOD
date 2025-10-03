using System;

namespace Spacats.LOD
{
    [Serializable]
    public class AOISettings
    {
        public readonly string StaticMeasureID = "AOT Static";
        public readonly string DynamicMeasureID = "AOT Dynamic";
        public bool PerformMeasurements = false;
    }
}
