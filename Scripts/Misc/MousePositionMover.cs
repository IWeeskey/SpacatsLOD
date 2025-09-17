using UnityEngine;

namespace Spacats.LOD
{
    public class MousePositionMover : MonoBehaviour
    {
        [SerializeField] private Camera _camera; // Камера, с которой смотрим

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Update()
        {
            if (_camera == null) _camera = Camera.main;

            Vector3 pos = transform.position;

            // Берём позицию мыши на экране
            Vector3 mousePos = Input.mousePosition;

            // Тут важно: нужно указать Z (расстояние от камеры до объекта)
            mousePos.z = Mathf.Abs(_camera.transform.position.y - pos.y);

            // Переводим экранные координаты в мировые
            Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);

            // Обновляем только X и Z, оставляя Y
            pos.x = worldPos.x;
            pos.z = worldPos.z;

            transform.position = pos;
        }
    }
}
