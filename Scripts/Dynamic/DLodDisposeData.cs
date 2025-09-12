using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Spacats.LOD
{
    public class DLodDisposeData
    {
        private bool _isCreated = false;

        public Dictionary<DLodUnit, RequestTypes> RequestsDict;
        public List<DLodUnit> Units;

        public TransformAccessArray UnitsTransform;
        public NativeArray<DLodUnitData> UnitsData;
        public NativeList<int2> ChangedLods;

        public DLodJob LodJob;
        public JobHandle LodJobHandle;

        public void Create(DLodSettings lodSettings)
        {
            if (Units == null) Units = new List<DLodUnit>();
            if (RequestsDict == null) RequestsDict = new Dictionary<DLodUnit, RequestTypes>();

            UnitsTransform = new TransformAccessArray(lodSettings.MaxUnitCount, -1);
            UnitsData = new NativeArray<DLodUnitData>(lodSettings.MaxUnitCount, Allocator.Persistent);
            ChangedLods = new NativeList<int2>(lodSettings.MaxUnitCount, Allocator.Persistent);

            _isCreated = true;
        }

        public void Dispose()
        {
            Units?.Clear();
            RequestsDict?.Clear();
            if (UnitsTransform.isCreated) UnitsTransform.Dispose();
            if (UnitsData.IsCreated) UnitsData.Dispose();
            if (ChangedLods.IsCreated) ChangedLods.Dispose();

            _isCreated = false;
        }

        public void ScheduleJob(DLodRuntimeData runtimeData)
        {
            if (!_isCreated) return;

            CreateJob(runtimeData);

            runtimeData.JobScheduled = true;
            LodJobHandle = LodJob.ScheduleReadOnlyByRef(UnitsTransform, 64);
        }
        public void InstantJob(DLodRuntimeData runtimeData)
        {
            if (!_isCreated) return;

            CreateJob(runtimeData);

            LodJobHandle = LodJob.ScheduleReadOnlyByRef(UnitsTransform, 64);
            LodJobHandle.Complete();
        }

        private void CreateJob(DLodRuntimeData runtimeData)
        {
            NativeList<int2>.ParallelWriter changedLodsWriter = ChangedLods.AsParallelWriter();

            LodJob = new DLodJob();
            LodJob.TargetPosition = runtimeData.TargetPosition;
            LodJob.UnitsData = UnitsData;
            LodJob.ChangedLodsWriter = changedLodsWriter;
        }
    }
}
