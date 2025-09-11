using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Spacats.LOD
{
    public class SLodDisposeData
    {
        public Dictionary<SLodUnit, RequestTypes> RequestsDict;
        public List<SLodUnit> Units;

        //public TransformAccessArray UnitsTransform;
        public NativeArray<SLodUnitData> UnitsData;
        public NativeList<int2> ChangedLods;

        public SLodJob LodJob;
        public JobHandle LodJobHandle;

        public void Create(SLodSettings lodSettings)
        {
            if (Units == null) Units = new List<SLodUnit>();
            if (RequestsDict == null) RequestsDict = new Dictionary<SLodUnit, RequestTypes>();

            //UnitsTransform = new TransformAccessArray(lodSettings.MaxUnitCount, -1);
            UnitsData = new NativeArray<SLodUnitData>(lodSettings.MaxUnitCount, Allocator.Persistent);
            ChangedLods = new NativeList<int2>(lodSettings.MaxUnitCount, Allocator.Persistent);
        }

        public void Dispose()
        {
            Units?.Clear();
            RequestsDict?.Clear();
            if (UnitsData.IsCreated) UnitsData.Dispose();
            if (ChangedLods.IsCreated) ChangedLods.Dispose();
        }

        public void ScheduleJob(SLodRuntimeData runtimeData)
        {
            CreateJob(runtimeData);

            runtimeData.JobScheduled = true;
            LodJobHandle = LodJob.Schedule(Units.Count, 64);
        }
        public void InstantJob(SLodRuntimeData runtimeData)
        {
            CreateJob(runtimeData);

            LodJobHandle = LodJob.Schedule(Units.Count, 64);
            LodJobHandle.Complete();
        }

        private void CreateJob(SLodRuntimeData runtimeData)
        {
            NativeList<int2>.ParallelWriter changedLodsWriter = ChangedLods.AsParallelWriter();

            LodJob = new SLodJob();
            LodJob.TargetPosition = runtimeData.TargetPosition;
            LodJob.UnitsData = UnitsData;
            LodJob.ChangedLodsWriter = changedLodsWriter;
        }
    }
}
