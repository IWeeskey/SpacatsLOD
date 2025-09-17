using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class DLodUnit : MonoBehaviour
    {
        [SerializeField]
        private List<LodUnitReciever> _receivers = new();
        private bool _isQuitting = false;
        private bool _isRegistered = false;

        public int RecieversCount => _receivers.Count;
        public Action<int> OnLodChanged;
        public DLodUnitData LODData;


        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnEnable()
        {
            MarkAsUnRegistered();
            ResetValues();
            RequestAdd();
        }

        private void OnDisable()
        {
            RequestRemove();
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

            foreach (var receiver in _receivers) receiver.OnLodChanged(LODData.CurrentLod);
        }
    }
}
