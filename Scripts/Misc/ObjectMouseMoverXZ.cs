using UnityEngine;

namespace Spacats.LOD
{
    public class ObjectMouseMoverXZ : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Update()
        {
            if (_camera == null) _camera = Camera.main;

            Vector3 pos = transform.position;
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(_camera.transform.position.y - pos.y);

            Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);
            pos.x = worldPos.x;
            pos.z = worldPos.z;

            transform.position = pos;
        }
    }
}
