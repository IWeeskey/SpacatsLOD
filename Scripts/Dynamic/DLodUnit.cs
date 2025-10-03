using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class DLodUnit : MonoBehaviour
    {
        [SerializeField] private LodUnitReciever _receiver;
        [SerializeField] private LodUnitAOI _aoiData;
        public LodUnitAOI AOIData => _aoiData;
        
        private bool _isQuitting = false;
        private bool _isRegistered = false;

        public Action<int> OnLodChanged;
        public Action<List<DLodUnit>> OnDynamicNeighboursChanged;
        public Action<List<SLodUnit>> OnStaticNeighboursChanged;
        
        public DLodUnitData LODData;

        public List<bool> DrawGizmo = new List<bool>();
        public bool IsRegistered => _isRegistered;
        public LodUnitReciever Receiver => _receiver;
        
        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnEnable()
        {
            MarkAsUnRegistered();
            _aoiData.MarkAsUnRegistered();
            _aoiData.SetSelf(this, null);
            if (_receiver == null && gameObject.GetComponent<LodUnitReciever>() != null) _receiver = gameObject.GetComponent<LodUnitReciever>();

            ResetValues();
            RequestAdd();
            TryRegisterAOI();
        }

        private void OnDisable()
        {
            RequestRemove();
            TryUnregisterAOI();
        }

        private void OnValidate()
        {
            FixGizmo();
        }

        private void FixGizmo()
        {
            if (DrawGizmo == null) DrawGizmo = new List<bool>();
            if (DrawGizmo.Count < 5)
            {
                while (DrawGizmo.Count < 5)
                    DrawGizmo.Add(false);
            }
        }

        private void ResetValues()
        {
            LODData.UnitIndex = 0;
            LODData.CurrentLod = 5;
        }

        public void RequestAdd()
        {
            if (_isRegistered) return;
            if (DynamicLODController.HasInstance) DynamicLODController.Instance.AddRequest(this, RequestTypes.Add);
        }

        public void RequestRemove()
        {
            if (_isQuitting) return;
            if (!_isRegistered) return;

            if (DynamicLODController.HasInstance) DynamicLODController.Instance.AddRequest(this, RequestTypes.Remove);
        }

        public void MarkAsRegistered()
        {
            _isRegistered = true;
        }

        public void MarkAsUnRegistered()
        {
            _isRegistered = false;
        }

        public void ChangeLOD(int value)
        {
            OnLodChanged?.Invoke(value);
            LODData.CurrentLod = value;

            _receiver?.OnLodChanged(LODData.CurrentLod);
        }

        public void TryRegisterAOI()
        {
            if (!_aoiData.AutoUpdate) return;
            if (_aoiData.IsRegistered) return;
            if (!AreaOfInterestController.HasInstance) return;
            AreaOfInterestController.Instance.RegisterAOI(this);
        }
            

        public void TryUnregisterAOI()
        {
            if (_aoiData.AutoUpdate) return;
            if (!_aoiData.IsRegistered) return;
            if (!AreaOfInterestController.HasInstance) return;
            AreaOfInterestController.Instance.UnRegisterAOI(this);
        }

        public void RaiseOnDynamicNeighboursChanged()
        {
            OnDynamicNeighboursChanged?.Invoke(_aoiData.DynamicNeighbours);
            _receiver?.OnDNeighboursChanged(_aoiData.DynamicNeighbours);
        }
        
        public void RaiseOnStaticNeighboursChanged()
        {
            OnStaticNeighboursChanged?.Invoke(_aoiData.StaticNeighbours);
            _receiver?.OnSNeighboursChanged(_aoiData.StaticNeighbours);
        }
    }
}
