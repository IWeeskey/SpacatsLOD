using System.Collections.Generic;
using UnityEngine;
using Spacats.Utils;

namespace Spacats.LOD
{
    public class LODRSpriteColorSmooth : LodUnitReciever
    {
        public SpriteRenderer TargetRenderer;
        public List<Color> LODColors;
        public int MaxLODLevel = 5;
        public bool DisableOnMaxLOD = false;
        public float SmoothTime = 0.2f;

        private int CurrentLodLevel = 0;
        private MonoTweenUnit _smoothTween;
        private Color _currentColor;
        private Color _targetColor;
        private void CheckSmoothTween()
        {
            if (_smoothTween != null) return;
            _smoothTween = new MonoTweenUnit(0, SmoothTime, null, LerpColor, OnLerpEnd, false, 0, 0);
        }

        private void OnDisable()
        {
            if (_smoothTween == null) return;
            _smoothTween.Break();
        }

        public override void OnLodChanged(int newLevel)
        {
            if (TargetRenderer == null) return;
            if (LODColors == null) return;

            CheckSmoothTween();

            CurrentLodLevel = newLevel;
            _currentColor = TargetRenderer.color;

            if (newLevel >= MaxLODLevel)
            {
                _targetColor = LODColors[MaxLODLevel - 1];
                _targetColor.a = 0f;
            }
            else
            {
                _targetColor = LODColors[newLevel];
            }

            TargetRenderer.enabled = true;


            _smoothTween.Start();
        }

        private void LerpColor(float progress)
        {
            TargetRenderer.color = Color.Lerp(_currentColor, _targetColor, progress);
        }

        private void OnLerpEnd()
        {
            if (!DisableOnMaxLOD) return;

            if (CurrentLodLevel >= MaxLODLevel)
            {
                TargetRenderer.enabled = false;
            }
        }
    }
}
