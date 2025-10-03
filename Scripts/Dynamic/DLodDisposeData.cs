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
        public NativeList<float> GroupMultipliers;
        public NativeParallelMultiHashMap<int3, int> Cells;

        public DLodJob LodJob;
        public JobHandle LodJobHandle;

        public void Create(DLodSettings lodSettings)
        {
            if (Units == null) Units = new List<DLodUnit>();
            if (RequestsDict == null) RequestsDict = new Dictionary<DLodUnit, RequestTypes>();

            UnitsTransform = new TransformAccessArray(lodSettings.MaxUnitCount, -1);
            UnitsData = new NativeArray<DLodUnitData>(lodSettings.MaxUnitCount, Allocator.Persistent);
            ChangedLods = new NativeList<int2>(lodSettings.MaxUnitCount, Allocator.Persistent);
            GroupMultipliers = new NativeList<float>(100, Allocator.Persistent);
            Cells = new NativeParallelMultiHashMap<int3, int>(lodSettings.MaxUnitCount, Allocator.Persistent);

            RefreshGroupMultipliers(lodSettings);

            _isCreated = true;
        }

        public void Dispose()
        {
            if (!_isCreated) return;
            
            Units?.Clear();
            RequestsDict?.Clear();
            if (UnitsTransform.isCreated) UnitsTransform.Dispose();
            if (UnitsData.IsCreated) UnitsData.Dispose();
            if (ChangedLods.IsCreated) ChangedLods.Dispose();
            if (GroupMultipliers.IsCreated) GroupMultipliers.Dispose();
            if (Cells.IsCreated) Cells.Dispose();

            _isCreated = false;
        }

        public void RefreshGroupMultipliers(DLodSettings lodSettings)
        {
            GroupMultipliers.Clear();
            for (int i = 0; i < lodSettings.GroupMultipliers.Count; i++)
            {
                GroupMultipliers.Add(lodSettings.GroupMultipliers[i]);
            }
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
            Cells.Clear();
            NativeList<int2>.ParallelWriter changedLodsWriter = ChangedLods.AsParallelWriter();
            NativeParallelMultiHashMap<int3, int>.ParallelWriter cellsWriter = Cells.AsParallelWriter();
            
            LodJob = new DLodJob();
            LodJob.TargetPosition = runtimeData.TargetPosition;
            LodJob.UnitsData = UnitsData;
            LodJob.ChangedLodsWriter = changedLodsWriter;
            LodJob.GroupMultipliers = GroupMultipliers;
            LodJob.CellsWriter = cellsWriter;
            LodJob.CellSize = runtimeData.CellSize;
            LodJob.AOTCalculations = runtimeData.AOTCalculations;
        }
    }
}
