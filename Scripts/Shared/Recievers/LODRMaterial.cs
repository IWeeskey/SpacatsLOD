using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    public class LODRMaterial : LodUnitReciever
    {
        public MeshRenderer TargetMRenderer;
        public SkinnedMeshRenderer TargetSMRenderer;

        public List<Material> LODMaterials;
        public int MaxLODLevel = 5;
        public bool DisableOnMaxLOD = false;
        public override void OnLodChanged(int newLevel)
        {
            if (TargetMRenderer == null && TargetSMRenderer ==null) return;
            if (LODMaterials == null) return;

            if (newLevel >= MaxLODLevel)
            {
                SetMaterial(LODMaterials.Count-1);

                if (DisableOnMaxLOD)
                {
                    if (TargetMRenderer != null) TargetMRenderer.enabled = false;
                    if (TargetSMRenderer != null) TargetSMRenderer.enabled = false;
                }

                return;
            }

            if (TargetMRenderer != null) TargetMRenderer.enabled = true;
            if (TargetSMRenderer != null) TargetSMRenderer.enabled = true;

            SetMaterial(newLevel);
        }

        public override void OnDNeighboursChanged(List<DLodUnit> newNeighbours)
        {
        }

        public override void OnSNeighboursChanged(List<SLodUnit> newNeighbours)
        {
        }

        private void SetMaterial(int matIndex)
        {
            if (matIndex > LODMaterials.Count) return;
            if (matIndex < 0) return;

            if (TargetMRenderer != null) TargetMRenderer.material = LODMaterials[matIndex];
            if (TargetSMRenderer != null) TargetSMRenderer.material = LODMaterials[matIndex];
        }
    }
}
