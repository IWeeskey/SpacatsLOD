using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Spacats.LOD
{
    public class SLodDisposeData
    {
        private bool _isCreated = false;

        public Dictionary<SLodUnit, RequestTypes> RequestsDict;
        public List<SLodUnit> Units;

        public NativeArray<SLodUnitData> UnitsData;
        public NativeList<int2> ChangedLods;
        public NativeList<float> GroupMultipliers;
        public NativeParallelMultiHashMap<int3, int> Cells;

        public SLodJob LodJob;
        public JobHandle LodJobHandle;

        public void Create(SLodSettings lodSettings)
        {
            if (Units == null) Units = new List<SLodUnit>();
            if (RequestsDict == null) RequestsDict = new Dictionary<SLodUnit, RequestTypes>();

            UnitsData = new NativeArray<SLodUnitData>(lodSettings.MaxUnitCount, Allocator.Persistent);
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
            if (UnitsData.IsCreated) UnitsData.Dispose();
            if (ChangedLods.IsCreated) ChangedLods.Dispose();
            if (GroupMultipliers.IsCreated) GroupMultipliers.Dispose();
            if (Cells.IsCreated) Cells.Dispose();
            
            _isCreated = false;
        }

        public void RefreshGroupMultipliers(SLodSettings lodSettings)
        {
            GroupMultipliers.Clear();
            for (int i = 0; i < lodSettings.GroupMultipliers.Count; i++)
            {
                GroupMultipliers.Add(lodSettings.GroupMultipliers[i]);
            }
        }

        public void ScheduleJob(SLodRuntimeData runtimeData)
        {
            if (!_isCreated) return;

            CreateJob(runtimeData);

            runtimeData.JobScheduled = true;
            LodJobHandle = LodJob.Schedule(Units.Count, 64);
        }
        public void InstantJob(SLodRuntimeData runtimeData)
        {
            if (!_isCreated) return;

            CreateJob(runtimeData);

            LodJobHandle = LodJob.Schedule(Units.Count, 64);
            LodJobHandle.Complete();
        }

        private void CreateJob(SLodRuntimeData runtimeData)
        {
            Cells.Clear();
            
            NativeList<int2>.ParallelWriter changedLodsWriter = ChangedLods.AsParallelWriter();
            NativeParallelMultiHashMap<int3, int>.ParallelWriter cellsWriter = Cells.AsParallelWriter();
            
            LodJob = new SLodJob();
            LodJob.TargetPosition = runtimeData.TargetPosition;
            LodJob.UnitsData = UnitsData;
            LodJob.ChangedLodsWriter = changedLodsWriter;
            LodJob.GroupMultipliers = GroupMultipliers;
            LodJob.CellsWriter = cellsWriter;
            LodJob.CellSize = runtimeData.CellSize;
            LodJob.PerformCellCalculations = runtimeData.PerformCellCalculations;
        }

    }
}
