using System.Collections.Generic;
using UnityEngine;

namespace Spacats.LOD
{
    public class LODRSpriteColor : LodUnitReciever
    {
        public SpriteRenderer TargetRenderer;
        public List<Color> LODColors;
        public int MaxLODLevel = 5;
        public bool DisableOnMaxLOD = false;
        public override void OnLodChanged(int newLevel)
        {
            if (TargetRenderer == null) return;
            if (LODColors == null) return;

            if (newLevel >= MaxLODLevel)
            {
                TargetRenderer.color = LODColors[LODColors.Count-1];
                if (DisableOnMaxLOD) TargetRenderer.enabled = false;
                return;
            }

            TargetRenderer.enabled = true;
            TargetRenderer.color = LODColors[newLevel];
        }
    }
}
