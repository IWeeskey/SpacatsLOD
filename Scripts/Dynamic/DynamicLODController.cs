using UnityEngine;
using Spacats.Utils;
using Unity.Mathematics;
using System.Globalization;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spacats.LOD
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(-10)]
    public class DynamicLODController : Controller
    {
        private static DynamicLODController _instance;

        private bool _isCreated = false;
        private DLodRuntimeData _runtimeData = new DLodRuntimeData();
        private DLodDisposeData _disposeData = new DLodDisposeData();

        public static bool HasInstance => _instance == null ? false : true;
        public static DynamicLODController Instance { get { if (_instance == null) Debug.LogError("DynamicLODController is not registered yet!"); return _instance; } }
        public DLodSettings LodSettings = new DLodSettings();

        public bool IsControllerRegistered => _registered;
        public int RequestsCount => (_disposeData.RequestsDict == null) ? 0 : _disposeData.RequestsDict.Count;
        public int LodUnitsCount => (_disposeData.Units == null) ? 0 : _disposeData.Units.Count;
        public int TransformCount => (_disposeData.UnitsTransform.isCreated) ? _disposeData.UnitsTransform.length : 0;
        public Vector3 TargetPosition => _runtimeData.TargetPosition;
        public (double, string) TotalTimeResult => _runtimeData.TotalResult;
        public (double, string) JobTimeResult => _runtimeData.JobTimeResult;
        public int ChangedLodsCount => _runtimeData.ChangedLodsCount;

        protected override void COnRegister()
        {
            base.COnRegister();
            _instance = this;
            _runtimeData.InSceneLoading = false;
            RefreshTargetPosition();
            Clear();
        }

        protected override void COnRegisteredEnable()
        {
            base.COnRegisteredEnable();
            if (!_isCreated) Clear();
        }

        protected override void COnRegisteredDisable()
        {
            base.COnRegisteredDisable();
            Dispose();
        }

        public override void COnSceneUnloading(Scene scene)
        {
            base.COnSceneUnloading(scene);
            _runtimeData.InSceneLoading = true;
            TryCompleteJob(true);
        }

        public override void COnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            base.COnSceneLoaded(scene, mode);
            _runtimeData.InSceneLoading = false;
        }

        public void Clear()
        {
            Dispose();
            Create();
        }

        public void SetTarget(Transform target)
        {
            LodSettings.Target = target;
        }

        private void Dispose()
        {
            if (!_isCreated) return;

            TryCompleteJob(true);
            _disposeData.Dispose();
            _runtimeData.JobScheduled = false;
            _runtimeData.ChangedLodsCount = 0;
            _isCreated = false;
        }

        private void Create()
        {
            if (_isCreated) return;

            _disposeData.Create(LodSettings);
            _isCreated = true;
        }

        private void TryCompleteJob(bool force = false)
        {
            if (!_runtimeData.JobScheduled) return;
            if (!_disposeData.LodJobHandle.IsCompleted && !force) return;

            _runtimeData.JobScheduled = false;
            _disposeData.LodJobHandle.Complete();
            _runtimeData.LastUpdateTime = Time.realtimeSinceStartup;

            HandleJobResult();

            if (LodSettings.PerformMeasurements)
            {
                _runtimeData.TotalResult = TimeTracker.Finish(LodSettings.TotalMeasureID, false);
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message = 
                        "D Total " + LodUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") + "; " +
                        _runtimeData.TotalResult.Item1.ToString() + "ms; changed " + ChangedLodsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
            }
        }

        private void HandleJobResult()
        {
            if (_applicationIsQuitting) return;

            foreach (int2 kv in _disposeData.ChangedLods)
            {
                _disposeData.Units[kv.x]?.ChangeLOD(kv.y);
            }
               
            _runtimeData.ChangedLodsCount = _disposeData.ChangedLods.Length;
            _disposeData.ChangedLods.Clear();

            _runtimeData.LastCellsCount = _disposeData.Cells.Count();
        }

        private void ProcessRequests()
        {
            if (_applicationIsQuitting) return;

            foreach (var kv in _disposeData.RequestsDict)
            {
                if (kv.Value == RequestTypes.Add)
                {
                    ProcessAddRequest(kv.Key);
                }
                else if (kv.Value == RequestTypes.Remove)
                {
                    ProcessRemoveRequest(kv.Key);
                }
            }

            _disposeData.RequestsDict.Clear();
        }

        public void AddRequest(DLodUnit unit, RequestTypes request)
        {
            if (LodSettings.AsyncLogic)
            {
                if (request == RequestTypes.Add) ProcessSingleUnit(unit);
                if (_disposeData.RequestsDict.ContainsKey(unit))
                {
                    _disposeData.RequestsDict[unit] = request;
                    return;
                }

                _disposeData.RequestsDict.Add(unit, request);
                return;
            }

            TryCompleteJob();

            switch ((int)request)
            {
                case (int)RequestTypes.Add: ProcessSingleUnit(unit); ProcessAddRequest(unit); break;
                case (int)RequestTypes.Remove: ProcessRemoveRequest(unit); break;
            }
        }

        private void ProcessAddRequest(DLodUnit unit)
        {
            if (unit ==null) return;
            if (unit.transform==null) return;
            if (_disposeData.Units.Count >= LodSettings.MaxUnitCount) return;
            if (unit.LODData.UnitIndex < _disposeData.Units.Count && _disposeData.Units[unit.LODData.UnitIndex] == unit) return;

            unit.LODData.UnitIndex = _disposeData.Units.Count;
            unit.MarkAsRegistered();

            _disposeData.Units.Add(unit);
            _disposeData.UnitsTransform.Add(unit.transform);
            _disposeData.UnitsData[unit.LODData.UnitIndex] = unit.LODData;
        }

        private void ProcessRemoveRequest(DLodUnit unit)
        {
            int index = unit.LODData.UnitIndex;
            int last = _disposeData.Units.Count - 1;

            if (index < 0 || index > last) return;

            if (index != last)
            {
                DLodUnit lastUnit = _disposeData.Units[last];
                _disposeData.Units[index] = lastUnit;
                lastUnit.LODData.UnitIndex = index;
                _disposeData.UnitsData[index] = lastUnit.LODData;
            }

            _disposeData.Units.RemoveAt(last);
            _disposeData.UnitsTransform.RemoveAtSwapBack(index);
            unit.MarkAsUnRegistered();

        }

        private void ProcessSingleUnit(DLodUnit unit)
        {
            float distance = math.distance(_runtimeData.TargetPosition, unit.transform.position);
            float mult = LodUtils.GetMultiplierFromList(unit.LODData.GroupIndex, ref _disposeData.GroupMultipliers);

            int lodLevel = LodUtils.LevelForDistance(distance, in unit.LODData.Distances, unit.transform.localScale.x, mult);
            unit.ChangeLOD(lodLevel);
        }

        public override void CSharedUpdate()
        {
            base.CSharedUpdate();

            if (!LodSettings.PerformLogic) return;
            if (!_isCreated) return;
            if (_runtimeData.InSceneLoading) return;

            TryCompleteJob();

            if (Time.realtimeSinceStartup - _runtimeData.LastUpdateTime >= LodSettings.UpdateRateMS / 1000f)
            {
                RefreshTargetPosition();
                if (!LodSettings.AsyncLogic)
                {
                    ProcessInstant();
                }
                else if (!_runtimeData.JobScheduled)
                {
                    ProcessAsync();
                }
            }
        }

        private void RefreshTargetPosition()
        {
#if UNITY_EDITOR
            bool inEditor = true;
            if (Application.isPlaying) inEditor = false;

            if (LodSettings.UseEditorCamera && inEditor && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                _runtimeData.TargetPosition = SceneView.lastActiveSceneView.camera.transform.position;
                return;
            }
#endif

            if (LodSettings.Target != null)
            {
                _runtimeData.TargetPosition = LodSettings.Target.position;
                return;
            }

            _runtimeData.TargetPosition = Vector3.zero;
            TryToShowLog("Not found target for lods", LogType.Log);
        }

        public void SetGroupMultipliers(List<float> values)
        {
            LodSettings.GroupMultipliers = values != null ? new List<float>(values) : new List<float>();
        }

        public void ProcessInstant()
        {
            TryCompleteJob();
            ProcessRequests();
            ApplySettings();
            
            if (LodSettings.PerformMeasurements)
            {
                TimeTracker.Start(LodSettings.TotalMeasureID);
                TimeTracker.Start(LodSettings.JobMeasureID);
            }

            _runtimeData.LastUpdateTime = Time.realtimeSinceStartup;
            _disposeData.InstantJob(_runtimeData);

            if (LodSettings.PerformMeasurements)
            {
                _runtimeData.JobTimeResult = TimeTracker.Finish(LodSettings.JobMeasureID, false);
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message = "D Job time: " + _runtimeData.JobTimeResult.Item1.ToString() + "ms";
            }

            HandleJobResult();

            if (LodSettings.PerformMeasurements)
            {
                _runtimeData.TotalResult = TimeTracker.Finish(LodSettings.TotalMeasureID, false);
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message += "\n" +
                        "D Total " + LodUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") + "; " +
                        _runtimeData.TotalResult.Item1.ToString() + "ms; changed " + ChangedLodsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
            }
        }

        private void ProcessAsync()
        {
            ProcessRequests();
            ApplySettings();
            if (LodSettings.PerformMeasurements) TimeTracker.Start(LodSettings.TotalMeasureID);
            _disposeData.ScheduleJob(_runtimeData);
        }

        private void ApplySettings()
        {
            _runtimeData.CellSize = LodSettings.CellSize;
            _runtimeData.PerformCellCalculations = LodSettings.PerformCellCalculations;

            _disposeData.RefreshGroupMultipliers(LodSettings);
        }
        
        public int GetCellsCount()
        {
            return _runtimeData.LastCellsCount;
        }
    }
}
