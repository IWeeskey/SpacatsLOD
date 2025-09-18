using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class SLodUnit : MonoBehaviour
    {
        private bool _isQuitting = false;
        private bool _isRegistered = false;

        [SerializeField]
        private LodUnitReciever _receiver;

        public Action<int> OnLodChanged;
        public SLodUnitData LODData;

        public List<bool> DrawGizmo = new List<bool>();
        public bool RegisterOnEnable = false;
        public bool IsRegistered => _isRegistered;


        private void OnApplicationQuit()
        {
            _isQuitting = true;
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

        private void OnEnable()
        {
            MarkAsUnRegistered();
            if (_receiver == null && gameObject.GetComponent<LodUnitReciever>() != null) _receiver = gameObject.GetComponent<LodUnitReciever>();

            if (RegisterOnEnable) RequestAddLOD();
        }

        private void OnDisable()
        {
            RequestRemoveLOD();
        }

        private void ResetValues()
        {
            LODData.UnitIndex = 0;
            LODData.CurrentLod = 5;
        }

        private void RefreshSelfData()
        {
            LODData.Position = gameObject.transform.position;
            LODData.Rotation = gameObject.transform.rotation;
            LODData.Scale = gameObject.transform.lossyScale.x;
        }

        public void Refresh()
        {
            RequestRemoveLOD();
            RequestAddLOD();
        }

        public void RequestAddLOD()
        {
            if (_isRegistered) return;

            ResetValues();
            RefreshSelfData();
            if (StaticLODController.HasInstance) StaticLODController.Instance.AddRequest(this, RequestTypes.Add);
        }

        public void RequestRemoveLOD()
        {
            if (_isQuitting) return;
            if (!_isRegistered) return;

            if (StaticLODController.HasInstance) StaticLODController.Instance.AddRequest(this, RequestTypes.Remove);
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
    }
}
