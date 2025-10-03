using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace Spacats.LOD
{
    [Serializable]
    public class LodUnitAOI
    {
        [HideInInspector] public int UnitIndex;
        
        public int Radius = 1;
        public bool AutoUpdate = false;
        // public List<int> StaticNeighbours = new List<int>();
        // public List<int> DynamicNeighbours = new List<int>();
            
        public List<DLodUnit> DynamicNeighbours = new List<DLodUnit>();
        public List<SLodUnit> StaticNeighbours = new List<SLodUnit>();
        
        [SerializeField] private bool _isRegistered = false;
        public int3 CellKey;

        private DLodUnit _selfDLodUnit;
        private SLodUnit _selfSLodUnit;

        public bool IsDynamic => _selfDLodUnit != null;
        
        public int DUnitIndex=>_selfDLodUnit == null ? -1: _selfDLodUnit.LODData.UnitIndex;
        public int SUnitIndex=>_selfSLodUnit == null ? -1: _selfSLodUnit.LODData.UnitIndex;
        
        public bool IsRegistered => _isRegistered;
        
        public void MarkAsUnRegistered()
        {
            _isRegistered = false;
        }
        
        public void MarkAsRegistered()
        {
            _isRegistered = true;
        }

        public void SetSelf(DLodUnit dLodUnit, SLodUnit sLodUnit)
        {
            _selfDLodUnit = dLodUnit;
            _selfSLodUnit = sLodUnit;
        }

        public void RaiseOnDynamicNeighboursChanged()
        {
            _selfDLodUnit?.RaiseOnDynamicNeighboursChanged();
            _selfSLodUnit?.RaiseOnDynamicNeighboursChanged();
        }
        
        public void RaiseOnStaticNeighboursChanged()
        {
            _selfSLodUnit?.RaiseOnStaticNeighboursChanged();
            _selfDLodUnit?.RaiseOnStaticNeighboursChanged();
        }
    }
}