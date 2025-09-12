using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class SLodUnit : MonoBehaviour
    {
        public bool RegisterOnEnable = false;
        private bool _isQuitting = false;
        private bool _isRegistered = false;

        [SerializeField]
        private LodUnitReciever _receiver;

        public Action<int> OnLodChanged;
        public SLodUnitData LODData;


        private void OnApplicationQuit()
        {
            _isQuitting = true;
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
            LODData.Scale = gameObject.transform.lossyScale.x;
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

        //private void OnDrawGizmosSelected()
        //{
        //    if (!ShowGizmos) return;
        //    Vector3 center = gameObject.transform.position;
        //    float scale = gameObject.transform.localScale.x;

        //    float sphereAlpha = 0.1f;
        //    Color gizColor = Color.green;
        //    gizColor.a = sphereAlpha;
        //    Gizmos.color = gizColor;
        //    Gizmos.DrawSphere(center, LODData.Data.Lod0 * scale);

        //    gizColor = Color.yellow;
        //    gizColor.a = sphereAlpha;
        //    Gizmos.color = gizColor;
        //    Gizmos.DrawSphere(center, LODData.Data.Lod1 * scale);

        //    gizColor = Color.blue;
        //    gizColor.a = sphereAlpha;
        //    Gizmos.color = gizColor;
        //    Gizmos.DrawSphere(center, LODData.Data.Lod2 * scale);

        //    gizColor = Color.red;
        //    gizColor.a = sphereAlpha;
        //    Gizmos.color = gizColor;
        //    Gizmos.DrawSphere(center, LODData.Data.Lod3 * scale);

        //    gizColor = Color.black;
        //    gizColor.a = sphereAlpha;
        //    Gizmos.color = gizColor;
        //    Gizmos.DrawSphere(center, LODData.Data.Lod4 * scale);
        //}

        public void ChangeLOD(int value)
        {
            OnLodChanged?.Invoke(value);

            LODData.CurrentLod = value;
            _receiver?.OnLodChanged(LODData.CurrentLod);
        }
    }
}
