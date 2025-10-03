using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    [ExecuteInEditMode]
    public class AOIUnitDebugger: MonoBehaviour
    {
        private DLodUnit _selfDLodUnit;
        private SLodUnit _selfSLodUnit;
        
        public LineRenderer DLineRenderer;
        public LineRenderer SLineRenderer;

        private bool _subscribed = false;

        public bool PerformLogic = true;

        private void Awake()
        {
            _subscribed = false;
            GetSelf();
        }
        
        private void GetSelf()
        {
            if (GetComponent<DLodUnit>()!=null) _selfDLodUnit = GetComponent<DLodUnit>();
            if (GetComponent<SLodUnit>()!=null) _selfSLodUnit = GetComponent<SLodUnit>();
        }

        private void OnEnable()
        {
            Subscribe();
        }
        
        private void OnDisable()
        {
            UnSubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            if (_selfDLodUnit) _selfDLodUnit.OnDynamicNeighboursChanged += OnDynamicNeighboursChanged;
            if (_selfDLodUnit) _selfDLodUnit.OnStaticNeighboursChanged += OnStaticNeighboursChanged;
            
            if (_selfSLodUnit) _selfSLodUnit.OnDynamicNeighboursChanged += OnDynamicNeighboursChanged;
            if (_selfSLodUnit) _selfSLodUnit.OnStaticNeighboursChanged += OnStaticNeighboursChanged;
        }

        private void UnSubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            
            if (_selfDLodUnit) _selfDLodUnit.OnDynamicNeighboursChanged -= OnDynamicNeighboursChanged;
            if (_selfDLodUnit) _selfDLodUnit.OnStaticNeighboursChanged -= OnStaticNeighboursChanged;
            
            if (_selfSLodUnit) _selfSLodUnit.OnDynamicNeighboursChanged -= OnDynamicNeighboursChanged;
            if (_selfSLodUnit) _selfSLodUnit.OnStaticNeighboursChanged -= OnStaticNeighboursChanged;
        }

        private void OnDynamicNeighboursChanged(List<DLodUnit> dynamicNeighbours)
        {
            if (!PerformLogic) return; 
            if (DLineRenderer == null) return;
            int pointCount = dynamicNeighbours.Count * 2;
            DLineRenderer.positionCount = pointCount;
            int index = 0;
            Vector3 origin = transform.position;

            foreach (var neighbour in dynamicNeighbours)
            {
                if (neighbour == null) continue;

                Vector3 target = neighbour.transform.position;
                DLineRenderer.SetPosition(index++, origin);
                DLineRenderer.SetPosition(index++, target);
            }
        }
        
        private void OnStaticNeighboursChanged(List<SLodUnit> staticNeighbours)
        {
            if (!PerformLogic) return; 
            if (SLineRenderer == null) return;
            int pointCount = staticNeighbours.Count * 2;
            SLineRenderer.positionCount = pointCount;
            int index = 0;
            Vector3 origin = transform.position;

            foreach (var neighbour in staticNeighbours)
            {
                if (neighbour == null) continue;

                Vector3 target = neighbour.transform.position;
                SLineRenderer.SetPosition(index++, origin);
                SLineRenderer.SetPosition(index++, target);
            }
        }
    }
}
