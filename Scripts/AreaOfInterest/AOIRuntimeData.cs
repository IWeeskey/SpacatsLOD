using System;

namespace Spacats.LOD
{
    [Serializable]
    public class AOIRuntimeData
    {
        public (double, string) DynamicResult;
        public (double, string) StaticResult;
    }
}
