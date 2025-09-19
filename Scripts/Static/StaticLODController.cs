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
    public class StaticLODController : Controller
    {
        private static StaticLODController _instance;

        private bool _isCreated = false;
        private SLodRuntimeData _runtimeData = new SLodRuntimeData();
        private SLodDisposeData _disposeData = new SLodDisposeData();

        public static bool HasInstance => _instance == null ? false : true;
        public static StaticLODController Instance { get { if (_instance == null) Debug.LogError("StaticLODController is not registered yet!"); return _instance; } }
        public SLodSettings LodSettings = new SLodSettings();

        public bool IsControllerRegistered => _registered;
        public int RequestsCount => (_disposeData.RequestsDict == null) ? 0 : _disposeData.RequestsDict.Count;
        public int LodUnitsCount => (_disposeData.Units == null) ? 0 : _disposeData.Units.Count;
        public Vector3 TargetPosition => _runtimeData.TargetPosition;
        public (double, string) TotalTimeResult => _runtimeData.TotalResult;
        public (double, string) JobTimeResult => _runtimeData.JobTimeResult;
        public int ChangedLodsCount => _runtimeData.ChangedLodsCount;

        protected override void COnRegister()
        {
            base.COnRegister();
            _instance = this;
            Clear();
        }

        protected override void COnRegisteredEnable()
        {
            base.COnEnable();
            if (!_isCreated) Clear();
        }

        protected override void COnRegisteredDisable()
        {
            base.COnDisable();
            Dispose();
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

            TryCompleteJob();
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

        private void TryCompleteJob()
        {
            if (!_runtimeData.JobScheduled) return;
            if (!_disposeData.LodJobHandle.IsCompleted) return;

            _runtimeData.JobScheduled = false;
            _disposeData.LodJobHandle.Complete();
            _runtimeData.LastUpdateTime = Time.realtimeSinceStartup;

            HandleJobResult();
            ProcessRequests();

            if (LodSettings.PerformMeasurements)
            {
                _runtimeData.TotalResult = TimeTracker.Finish(LodSettings.TotalMeasureID, false);
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message =
                        "Total " + LodUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") + "; " +
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

        public void AddRequest(SLodUnit unit, RequestTypes request)
        {
            if (LodSettings.AsyncLogic)
            {
                if (_disposeData.RequestsDict.ContainsKey(unit))
                {
                    _disposeData.RequestsDict[unit] = request;
                    return;
                }

                if (request == RequestTypes.Add) ProcessSingleUnit(unit);
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

        private void ProcessAddRequest(SLodUnit unit)
        {
            if (_disposeData.Units.Count >= LodSettings.MaxUnitCount) return;
            if (unit.LODData.UnitIndex < _disposeData.Units.Count && _disposeData.Units[unit.LODData.UnitIndex] == unit) return;

            unit.LODData.UnitIndex = _disposeData.Units.Count;
            unit.MarkAsRegistered();
            _disposeData.Units.Add(unit);
            _disposeData.UnitsData[unit.LODData.UnitIndex] = unit.LODData;
        }

        private void ProcessRemoveRequest(SLodUnit unit)
        {
            int index = unit.LODData.UnitIndex;
            int last = _disposeData.Units.Count - 1;

            if (index < 0 || index > last) return;

            if (index != last)
            {
                SLodUnit lastUnit = _disposeData.Units[last];
                _disposeData.Units[index] = lastUnit;
                lastUnit.LODData.UnitIndex = index;
                _disposeData.UnitsData[index] = lastUnit.LODData;
            }

            _disposeData.Units.RemoveAt(last);

            unit.MarkAsUnRegistered();
        }

        private void ProcessSingleUnit(SLodUnit unit)
        {
            float distance = math.distance(_runtimeData.TargetPosition, unit.LODData.Position);
            float mult = LodUtils.GetMultiplierFromList(unit.LODData.GroupIndex, ref _disposeData.GroupMultipliers);

            int lodLevel = LodUtils.LevelForDistance(distance, in unit.LODData.Distances, unit.LODData.Scale, mult);
            unit.ChangeLOD(lodLevel);
        }

        public override void CSharedUpdate()
        {
            base.CSharedUpdate();

            if (!LodSettings.PerformLogic) return;
            if (!_isCreated) return;

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
            RefreshGroupMultipliers();
        }

        public void RefreshGroupMultipliers()
        {
            _disposeData.RefreshGroupMultipliers(LodSettings);
        }

        public void ProcessInstant()
        {
            TryCompleteJob();

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
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message = "Job time: " + _runtimeData.JobTimeResult.Item1.ToString() + "ms";
            }

            HandleJobResult();

            if (LodSettings.PerformMeasurements)
            {
                _runtimeData.TotalResult = TimeTracker.Finish(LodSettings.TotalMeasureID, false);
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message += "\n" +
                        "Total " + LodUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") + "; " +
                        _runtimeData.TotalResult.Item1.ToString() + "ms; changed " + ChangedLodsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
            }
        }

        private void ProcessAsync()
        {
            if (LodSettings.PerformMeasurements) TimeTracker.Start(LodSettings.TotalMeasureID);
            _disposeData.ScheduleJob(_runtimeData);
        }
    }
}
