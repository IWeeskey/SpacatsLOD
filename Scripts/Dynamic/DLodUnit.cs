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

        public int RecieversCount => _receivers.Count;
        public Action<int> OnLodChanged;
        public DLodUnitData LODData;


        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnEnable()
        {
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
            if (DynamicLODController.HasInstance) DynamicLODController.Instance.AddRequest(this, RequestTypes.Add);
        }

        public void RequestRemove()
        {
            if (_isQuitting) return;
            if (DynamicLODController.HasInstance) DynamicLODController.Instance.AddRequest(this, RequestTypes.Remove);
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

            foreach (var receiver in _receivers) receiver.OnLodChanged(LODData.CurrentLod);
        }
    }
}
