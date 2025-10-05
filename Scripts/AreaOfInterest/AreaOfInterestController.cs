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
        
        private const int UNITS_PER_JOB = 100;
        
        private AOIRuntimeData _runtimeData = new AOIRuntimeData();
        public AOISettings AOISettings = new AOISettings();
        
        public (double, string) DynamicResult => _runtimeData.DynamicResult;
        public (double, string) StaticResult => _runtimeData.StaticResult;
        
        public int DUnitsCount => (_dUnits == null) ? 0 : _dUnits.Count;
        public int SUnitsCount => (_sUnits == null) ? 0 : _sUnits.Count;
        
        private NativeArray<AOIJobUnitData> _unitsDataToStatic;
        private NativeArray<AOIJobUnitData> _unitsDataToDynamic;
        
        private int _unitsInStaticJob = 0;
        private int _unitsInDynamicJob = 0;
        
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
            
            if (_unitsDataToDynamic.IsCreated) _unitsDataToDynamic.Dispose();
            if (_unitsDataToStatic.IsCreated) _unitsDataToStatic.Dispose();
        }

        public void ProcessDynamic(NativeParallelMultiHashMap<int3, int> dCells, float cellSize)
        {
            return;
            if (AOISettings.PerformMeasurements)
            {
                TimeTracker.Start(AOISettings.DynamicMeasureID);
            }
            
            int _dUnitsCount = _dUnits.Count;
            for (int i = 0; i < _dUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _dUnits[i].AOIData;
                aoiUnit.CellKey = LodUtils.GetCellKey(_dUnits[i].transform.position, cellSize);
                ProcessSingleUnit(aoiUnit, dCells, true, true);
            }

            int _sUnitsCount = _sUnits.Count;
            for (int i = 0; i < _sUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _sUnits[i].AOIData;
                aoiUnit.CellKey = LodUtils.GetCellKey(_sUnits[i].transform.position, cellSize);
                ProcessSingleUnit(aoiUnit, dCells, true,false);
            }


            if (AOISettings.PerformMeasurements)
            {
                _runtimeData.DynamicResult = TimeTracker.Finish(AOISettings.DynamicMeasureID, false);
                // if (GUIPermanentMessage.Instance != null)
                // {
                //     GUIPermanentMessage.Instance.Message = "";
                //     GUIPermanentMessage.Instance.Message += "\n" +
                //                                             "D Total " +
                //                                             _dUnitsCount.ToString("#,0", CultureInfo.InvariantCulture)
                //                                                 .Replace(",", " ") + "; " +
                //                                             _runtimeData.DynamicResult.Item1.ToString() + "ms;";
                // }
            }
        }
        
        public void ProcessStatic(NativeParallelMultiHashMap<int3, int> sCells, float cellSize)
        {
            if (AOISettings.PerformMeasurements)
            {
                TimeTracker.Start(AOISettings.StaticMeasureID);
            }
            int _sUnitsCount = _sUnits.Count;
            int _dUnitsCount = _dUnits.Count;
            _unitsDataToStatic = new NativeArray<AOIJobUnitData>(_sUnitsCount + _dUnitsCount, Allocator.Persistent);
            
            // int _sUnitsCount = _sUnits.Count;
            // for (int i = 0; i < _sUnitsCount; i++)
            // {
            //     LodUnitAOI aoiUnit = _sUnits[i].AOIData;
            //     aoiUnit.CellKey = LodUtils.GetCellKey(_sUnits[i].transform.position, cellSize);
            //     ProcessSingleUnit(aoiUnit, sCells, false);
            // }
           
            _unitsInStaticJob = 0;
            
            for (int i = 0; i < _dUnitsCount; i++)
            {
                LodUnitAOI aoiUnit = _dUnits[i].AOIData;
                BuildJobFromStatic(i, i == _dUnitsCount - 1,aoiUnit,false, true, sCells);

               
                
                //ProcessSingleUnit(aoiUnit, sCells, false, true);
            }

            _unitsDataToStatic.Dispose();
            
            if (AOISettings.PerformMeasurements)
            {
                _runtimeData.StaticResult = TimeTracker.Finish(AOISettings.StaticMeasureID, false);
                if (GUIPermanentMessage.Instance != null)
                {
                    GUIPermanentMessage.Instance.Message = "";
                    GUIPermanentMessage.Instance.Message += "\n" +
                                                            "D Total " +
                                                            _dUnitsCount.ToString("#,0", CultureInfo.InvariantCulture)
                                                                .Replace(",", " ") + "; " +
                                                            _runtimeData.StaticResult.Item1.ToString() + "ms;";
                }
            }
        }

        private void BuildJobFromStatic(int index, bool isLast, LodUnitAOI aoiUnit, bool isDynamic, bool isSelfDynamic, NativeParallelMultiHashMap<int3, int> cells)
        {
            return;
            int lodUnitIndex = 0;
            int3 cellKey = 0;

            if (isSelfDynamic)
            {
                lodUnitIndex = aoiUnit.DUnitIndexFast;
                cellKey = DynamicLODController.Instance.GetUnitCellPositionByIndex(lodUnitIndex);
            }
            else
            {
                lodUnitIndex = aoiUnit.SUnitIndexFast;
                cellKey = StaticLODController.Instance.GetUnitCellPositionByIndex(lodUnitIndex);
            }
            
            AOIJobUnitData unitData = new AOIJobUnitData();

            unitData.AOIUnitIndex = _unitsInStaticJob;
            unitData.NeighboursIndex = 0;
            unitData.CenterCell = cellKey;
            unitData.IsSelfDynamic = isSelfDynamic;
            unitData.IsWholeDynamic = isDynamic;
            unitData.Radius = aoiUnit.Radius;
            unitData.LodUnitIndex = lodUnitIndex;

            _unitsDataToStatic[_unitsInStaticJob] = unitData;
                
            _unitsInStaticJob++;
            if (isLast || _unitsInStaticJob >= UNITS_PER_JOB)
            {
                int maxUnitsInJob = _unitsInStaticJob;

                NativeList<int> tempNeighbours = new NativeList<int>(0, Allocator.Persistent);
                
                AOIUnitJob job = new AOIUnitJob();
                job.MaxUnitsInJob = maxUnitsInJob;
                job.Cells = cells;
                job.Neighbours = tempNeighbours;
                job.UnitsData = _unitsDataToStatic;
                job.Schedule().Complete();
                
                tempNeighbours.Dispose();
                _unitsInStaticJob = 0;
            }
        }

        private void ProcessSingleUnit(LodUnitAOI aoiUnit, NativeParallelMultiHashMap<int3, int> cells, bool isDynamic, bool isSelfDynamic)
        {
            int lodUnitIndex = 0;
            int3 cellKey = 0;

            if (isSelfDynamic)
            {
                lodUnitIndex = aoiUnit.DUnitIndexFast;
                 cellKey = DynamicLODController.Instance.GetUnitCellPositionByIndex(lodUnitIndex);
            }
            else
            {
                lodUnitIndex = aoiUnit.SUnitIndexFast;
                cellKey = StaticLODController.Instance.GetUnitCellPositionByIndex(lodUnitIndex);
            }


            
            // if (isDynamic) aoiUnit.DynamicNeighbours.Clear();
            // else aoiUnit.StaticNeighbours.Clear();
            

            
            //NativeList<int> tempNeighbours = new NativeList<int>(Allocator.Persistent);

            AOIUnitJob job = new AOIUnitJob();

            //job.Cells = cells;
            //job.Neighbours = tempNeighbours;
            // job.CenterCell = aoiUnit.CellKey;
            // job.Radius = aoiUnit.Radius;
            // job.IsWholeDynamic = isDynamic;
            // job.IsSelfDynamic = aoiUnit.IsDynamic;
            // job.LodUnitIndex = lodUnitIndex;
            job.Schedule().Complete();

            // for (int i = 0; i < tempNeighbours.Length; i++)
            // {
            //     int index = tempNeighbours[i];
            //     
            //     if (isDynamic) aoiUnit.DynamicNeighbours.Add(DynamicLODController.Instance.GetUnitByIndex(index));
            //     else  aoiUnit.StaticNeighbours.Add(StaticLODController.Instance.GetUnitByIndex(index));
            // }
            //
            // if (isDynamic) aoiUnit.RaiseOnDynamicNeighboursChanged();
            // else aoiUnit.RaiseOnStaticNeighboursChanged();

            //tempNeighbours.Dispose();
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
