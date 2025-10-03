using UnityEngine;
using Spacats.Utils;
using Unity.Mathematics;
using System.Globalization;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spacats.LOD
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(-10)]
    public class AreaOfInterestController: Controller
    {
        private static AreaOfInterestController _instance;
        public static AreaOfInterestController Instance { get { if (_instance == null) Debug.LogError("AreaOfInterestController is not registered yet!"); return _instance; } }
        public static bool HasInstance => _instance == null ? false : true;
        public bool IsControllerRegistered => _registered;
        
        private List<DLodUnit> _dUnits;
        private List<SLodUnit> _sUnits;
        
        private AOIRuntimeData _runtimeData = new AOIRuntimeData();
        public AOISettings AOISettings = new AOISettings();
        
        public (double, string) DynamicResult => _runtimeData.DynamicResult;
        public (double, string) StaticResult => _runtimeData.StaticResult;
        
        public int DUnitsCount => (_dUnits == null) ? 0 : _dUnits.Count;
        public int SUnitsCount => (_sUnits == null) ? 0 : _sUnits.Count;
        
        protected override void COnRegister()
        {
            base.COnRegister();
            _instance = this;
            Dispose();
            Create();
        }

        protected override void COnRegisteredEnable()
        {
            base.COnRegisteredEnable();
            Dispose();
            Create();
        }

        protected override void COnRegisteredDisable()
        {
            base.COnRegisteredDisable();
            Dispose();
        }

        public override void COnSceneUnloading(Scene scene)
        {
            base.COnSceneUnloading(scene);
        }

        public override void COnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            base.COnSceneLoaded(scene, mode);
        }

        private void Create()
        {
            _dUnits = new List<DLodUnit>();
            _sUnits = new List<SLodUnit>();
        }

        private void Dispose()
        {
            _dUnits?.Clear();
            _sUnits?.Clear();
            
        }

        public void ProcessDynamic(NativeParallelMultiHashMap<int3, int> dCells, float cellSize)
        {
            if (AOISettings.PerformMeasurements)
            {
                TimeTracker.Start(AOISettings.DynamicMeasureID);
            }
            
            int _dUnitsCount = _dUnits.Count;
            for (int i = 0; i < _dUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _dUnits[i].AOIData;
                aoiUnit.CellKey = LodUtils.GetCellKey(_dUnits[i].transform.position, cellSize);
                ProcessSingleUnit(aoiUnit, dCells, true);
            }

            int _sUnitsCount = _sUnits.Count;
            for (int i = 0; i < _sUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _sUnits[i].AOIData;
                aoiUnit.CellKey = LodUtils.GetCellKey(_sUnits[i].transform.position, cellSize);
                ProcessSingleUnit(aoiUnit, dCells, false);
            }


            if (AOISettings.PerformMeasurements)
            {
                _runtimeData.DynamicResult = TimeTracker.Finish(AOISettings.DynamicMeasureID, false);
                if (GUIPermanentMessage.Instance != null)
                {
                    GUIPermanentMessage.Instance.Message = "";
                    GUIPermanentMessage.Instance.Message += "\n" +
                                                            "D Total " +
                                                            _dUnitsCount.ToString("#,0", CultureInfo.InvariantCulture)
                                                                .Replace(",", " ") + "; " +
                                                            _runtimeData.DynamicResult.Item1.ToString() + "ms;";
                }
            }
        }
        
        public void ProcessStatic(NativeParallelMultiHashMap<int3, int> sCells, float cellSize)
        {
            if (AOISettings.PerformMeasurements)
            {
                TimeTracker.Start(AOISettings.StaticMeasureID);
            }
            
            int _sUnitsCount = _sUnits.Count;
            for (int i = 0; i < _sUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _sUnits[i].AOIData;
                aoiUnit.CellKey = LodUtils.GetCellKey(_sUnits[i].transform.position, cellSize);
                ProcessSingleUnit(aoiUnit, sCells, false);
            }
            
            int _dUnitsCount = _dUnits.Count;
            for (int i = 0; i < _dUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _dUnits[i].AOIData;
                aoiUnit.CellKey = LodUtils.GetCellKey(_dUnits[i].transform.position, cellSize);
                ProcessSingleUnit(aoiUnit, sCells, false);
            }
            
            
            
            if (AOISettings.PerformMeasurements)
            {
                _runtimeData.StaticResult = TimeTracker.Finish(AOISettings.StaticMeasureID, false);
            }
        }

        private void ProcessSingleUnit(LodUnitAOI aoiUnit, NativeParallelMultiHashMap<int3, int> cells, bool isDynamic)
        {
            // int radius = aoiUnit.Radius;
            // int3 cellKey = aoiUnit.CellKey;
            
            if (isDynamic) aoiUnit.DynamicNeighbours.Clear();
            else aoiUnit.StaticNeighbours.Clear();
            
            NativeList<int> tempNeighbours = new NativeList<int>(Allocator.Persistent);

            int lodUnitIndex = 0;

            if (aoiUnit.IsDynamic) lodUnitIndex = aoiUnit.DUnitIndex;
            else lodUnitIndex = aoiUnit.SUnitIndex;

            AOIUnitJob job = new AOIUnitJob();

            job.Cells = cells;
            job.Neighbours = tempNeighbours;
            job.CenterCell = aoiUnit.CellKey;
            job.Radius = aoiUnit.Radius;
            job.IsWholeDynamic = isDynamic;
            job.IsSelfDynamic = aoiUnit.IsDynamic;
            job.LodUnitIndex = lodUnitIndex;
            job.Schedule().Complete();

            for (int i = 0; i < tempNeighbours.Length; i++)
            {
                int index = tempNeighbours[i];
                
                if (isDynamic) aoiUnit.DynamicNeighbours.Add(DynamicLODController.Instance.GetUnitByIndex(index));
                else  aoiUnit.StaticNeighbours.Add(StaticLODController.Instance.GetUnitByIndex(index));
            }
            //JobHandle jobHandle = job.che
            
            
            //AOIBurstUtils.FillNeighboursList(cells, tempNeighbours, aoiUnit.CellKey, aoiUnit.Radius, isDynamic, aoiUnit.IsDynamic, lodUnitIndex);
            
            // for (int x = -radius; x <= radius; x++)
            // {
            //     for (int y = -radius; y <= radius; y++)
            //     {
            //         for (int z = -radius; z <= radius; z++)
            //         {
            //             int3 offset = new int3(x, y, z);
            //             if (math.lengthsq(offset) > radius * radius) continue;
            //             
            //             int3 checkCell = cellKey + offset;
            //             ProcessCell(checkCell, aoiUnit, ref cells, isDynamic);
            //         }
            //     }
            // }
            
            if (isDynamic) aoiUnit.RaiseOnDynamicNeighboursChanged();
            else aoiUnit.RaiseOnStaticNeighboursChanged();

            tempNeighbours.Dispose();
        }

        private void ProcessCell(int3 cellKey, LodUnitAOI aoiUnit, ref NativeParallelMultiHashMap<int3, int> cells, bool isDynamic)
        {
            if (cells.TryGetFirstValue(cellKey, out int value, out var it))
            {
                do
                {
                    if (aoiUnit.IsDynamic && isDynamic && aoiUnit.DUnitIndex == value) continue;
                    if (!aoiUnit.IsDynamic && !isDynamic && aoiUnit.SUnitIndex == value) continue;
                    
                    if (isDynamic) aoiUnit.DynamicNeighbours.Add(DynamicLODController.Instance.GetUnitByIndex(value));
                    else  aoiUnit.StaticNeighbours.Add(StaticLODController.Instance.GetUnitByIndex(value));
                }
                while (cells.TryGetNextValue(out value, ref it));
            }
            
        }

        public void RegisterAOI(DLodUnit unit)
        {
            if (unit ==null) return;
            unit.AOIData.UnitIndex = _dUnits.Count;
            _dUnits.Add(unit);
            unit.AOIData.MarkAsRegistered();
        }
        
        public void RegisterAOI(SLodUnit unit)
        {
            if (unit ==null) return;
            unit.AOIData.UnitIndex = _sUnits.Count;
            _sUnits.Add(unit);
            unit.AOIData.MarkAsRegistered();
        }

        public void UnRegisterAOI(DLodUnit unit)
        {
            int index = unit.AOIData.UnitIndex;
            int last = _dUnits.Count - 1;

            if (index < 0 || index > last) return;

            if (index != last)
            {
                DLodUnit lastUnit = _dUnits[last];
                _dUnits[index] = lastUnit;
                lastUnit.AOIData.UnitIndex = index;
            }

            _dUnits.RemoveAt(last);
            unit.AOIData.MarkAsUnRegistered();
        }
        
        public void UnRegisterAOI(SLodUnit unit)
        {
            int index = unit.AOIData.UnitIndex;
            int last = _sUnits.Count - 1;

            if (index < 0 || index > last) return;

            if (index != last)
            {
                SLodUnit lastUnit = _sUnits[last];
                _sUnits[index] = lastUnit;
                lastUnit.AOIData.UnitIndex = index;
            }

            _sUnits.RemoveAt(last);
            unit.AOIData.MarkAsUnRegistered();
        }

    }
}
