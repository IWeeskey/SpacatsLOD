using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spacats.Utils;
using System;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;

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

        private bool _jobScheduled = false;
        private float _lastUpdateTime = 0f;

        private Dictionary<DLodUnit, RequestTypes> _requestsDict;
        private List<DLodUnit> _units;

        private (double, string) _measurementsResult;
        private string _measurementsID = "DLOD UPDATE";

        private Vector3 _targetPosition;
        public Vector3 TargetPosition => _targetPosition;

        private TransformAccessArray _unitsTransform;
        private NativeArray<DLodUnitData> _unitsData;
        private NativeList<int2> _changedLods;
        private DLodJob _lodJob;
        private JobHandle _lodJobHandle;

        public bool UseEditorCamera = false;
        public bool PerformLogic = false;
        public bool AsyncLogic = false;
        public bool PerformMeasurements = false;
        public int MaxUnitCount = 1000000;
        public float UpdateRateMS = 16;
        public (double, string) MeasurementsResult=> _measurementsResult;
        //public double TimeSpentMS = 0;
        //public double UpdateMS = 0;
        public Transform Target;

        public static bool HasInstance => _instance == null ? false : true;
        public static DynamicLODController Instance { get { if (_instance == null) Debug.LogError("DynamicLODController is not registered yet!"); return _instance; } }

        public bool IsControllerRegistered => _registered;
        public int RequestsCount => (_requestsDict == null) ? 0 : _requestsDict.Count;
        public int LodUnitsCount => (_units == null) ? 0 : _units.Count;
        public int TransformCount => (_unitsTransform.isCreated) ? _unitsTransform.length : 0;
        public int ChangedLodsCount = 0;
    
        protected override void COnRegister()
        {
            base.COnRegister();
            _instance = this;
        }

        protected override void COnEnable()
        {
            base.COnEnable();
            Clear();
        }

        protected override void COnDisable()
        {
            base.COnDisable();
            Dispose();
        }

        public void Clear()
        {
            Dispose();
            Create();
        }

        private void Dispose()
        {
            TryCompleteJob();

            _units?.Clear();
            _requestsDict?.Clear();
            if (_unitsTransform.isCreated) _unitsTransform.Dispose();
            if (_unitsData.IsCreated) _unitsData.Dispose();
            if (_changedLods.IsCreated) _changedLods.Dispose();

            _jobScheduled = false;
            ChangedLodsCount = 0;
        }

        private void Create()
        {
            if (_units == null) _units = new List<DLodUnit>();
            if (_requestsDict == null) _requestsDict = new Dictionary<DLodUnit, RequestTypes>();

            _unitsTransform = new TransformAccessArray(MaxUnitCount, -1);
            _unitsData = new NativeArray<DLodUnitData>(MaxUnitCount, Allocator.Persistent);
            _changedLods = new NativeList<int2>(MaxUnitCount, Allocator.Persistent);
        }

        private void TryCompleteJob()
        {
            if (!_jobScheduled) return;
            if (!_lodJobHandle.IsCompleted) return;

            _jobScheduled = false;
            _lodJobHandle.Complete();
            _lastUpdateTime = Time.realtimeSinceStartup;
            HandleJobResult();
            ProcessRequests();

            if (PerformMeasurements)
            {
                _measurementsResult = TimeTracker.Finish(_measurementsID, false);
                if (GUIPermanentMessage.Instance != null) GUIPermanentMessage.Instance.Message = _measurementsResult.Item2;
            }
        }

        private void HandleJobResult()
        {
            if (_applicationIsQuitting) return;

            foreach (int2 kv in _changedLods)
            {
                _units[kv.x]?.ChangeLOD(kv.y);
            }

            if (_changedLods.Length > ChangedLodsCount) ChangedLodsCount = _changedLods.Length;

            _changedLods.Clear();
        }

        private void ProcessRequests()
        {
            if (_applicationIsQuitting) return;

            foreach (var kv in _requestsDict)
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

            _requestsDict.Clear();
        }

        public void AddRequest(DLodUnit unit, RequestTypes request)
        {
            if (AsyncLogic)
            {
                if (_requestsDict.ContainsKey(unit))
                {
                    _requestsDict[unit] = request;
                    return;
                }

                if (request == RequestTypes.Add) ProcessInstant(unit);
                _requestsDict.Add(unit, request);
                return;
            }

            TryCompleteJob();

            switch ((int)request)
            {
                case (int)RequestTypes.Add: ProcessInstant(unit); ProcessAddRequest(unit); break;
                case (int)RequestTypes.Remove: ProcessRemoveRequest(unit); break;
            }
        }


        private void ProcessAddRequest(DLodUnit unit)
        {
            if (_units.Count >= MaxUnitCount) return;
            if (unit.LODData.UnitIndex < _units.Count && _units[unit.LODData.UnitIndex] == unit) return;

            unit.LODData.UnitIndex = _units.Count;
            _units.Add(unit);
            _unitsTransform.Add(unit.transform);
            _unitsData[unit.LODData.UnitIndex] = unit.LODData;
        }


        private void ProcessRemoveRequest(DLodUnit unit)
        {
            int index = unit.LODData.UnitIndex;
            int last = _units.Count - 1;

            if (index < 0 || index > last) return;

            if (index != last)
            {
                DLodUnit lastUnit = _units[last];
                _units[index] = lastUnit;
                lastUnit.LODData.UnitIndex = index;
                _unitsData[index] = lastUnit.LODData;
            }

            _units.RemoveAt(last);
            _unitsTransform.RemoveAtSwapBack(index);

        }

        private void ProcessInstant(DLodUnit unit)
        {
            float distance = math.distance(TargetPosition, unit.transform.position);

            int lodLevel = LodUtils.LevelForDistance(distance, in unit.LODData.Data, transform.localScale.x);
            unit.ChangeLOD(lodLevel);
        }

        public override void CSharedUpdate()
        {
            base.CSharedUpdate();

            if (!PerformLogic) return;
            TryCompleteJob();
            if (Time.realtimeSinceStartup - _lastUpdateTime >= UpdateRateMS / 1000f)
            {
                RefreshTargetPosition();
                if (!AsyncLogic)
                {
                    _lastUpdateTime = Time.realtimeSinceStartup;
                    ProcessLODs();
                }
                else if (!_jobScheduled)
                {
                    ScheduleJob();
                }
            }
        }

        private void RefreshTargetPosition()
        {
#if UNITY_EDITOR
            bool inEditor = true;
            if (Application.isPlaying) inEditor = false;

            if (UseEditorCamera && inEditor && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                _targetPosition = SceneView.lastActiveSceneView.camera.transform.position;
                return;
            }
#endif

            if (Target != null)
            {
                _targetPosition = Target.position;
                return;
            }

            _targetPosition = Vector3.zero;
            TryToShowLog("Not found target for lods", LogType.Log);
        }

        public void ProcessLODs(bool logs = false)
        {
            InstantJob();
        }

        private void InstantJob()
        {
            TryCompleteJob();

            if (PerformMeasurements) TimeTracker.Start(_measurementsID);

            NativeList<int2>.ParallelWriter changedLodsWriter = _changedLods.AsParallelWriter();

            _lodJob = new DLodJob();
            _lodJob.TargetPosition = _targetPosition;
            _lodJob.UnitsData = _unitsData;
            _lodJob.ChangedLodsWriter = changedLodsWriter;

            _lodJobHandle = _lodJob.ScheduleReadOnlyByRef(_unitsTransform, 64);
            _lodJobHandle.Complete();
            HandleJobResult();

            if (PerformMeasurements)
            {
                _measurementsResult = TimeTracker.Finish(_measurementsID,false);
                if (GUIPermanentMessage.Instance!=null) GUIPermanentMessage.Instance.Message = _measurementsResult.Item2;
            }
        }

        private void ScheduleJob()
        {
            if (PerformMeasurements) TimeTracker.Start(_measurementsID);

            NativeList<int2>.ParallelWriter changedLodsWriter = _changedLods.AsParallelWriter();

            _lodJob = new DLodJob();
            _lodJob.TargetPosition = _targetPosition;
            _lodJob.UnitsData = _unitsData;
            _lodJob.ChangedLodsWriter = changedLodsWriter;

            _jobScheduled = true;
            _lodJobHandle = _lodJob.ScheduleReadOnlyByRef(_unitsTransform, 64);
        }
    }
}
