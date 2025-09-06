using UnityEngine;

namespace Spacats.LOD
{
    public class MousePositionMover : MonoBehaviour
    {
        public float moveRangeX = 100f;
        public float moveRangeZ = 100f;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Update()
        {
            Vector3 mousePos = Input.mousePosition;

            float normalizedX = (mousePos.x / Screen.width - 0.5f) * 2f;
            float normalizedZ = (mousePos.y / Screen.height - 0.5f) * 2f;

            float posX = normalizedX * moveRangeX;
            float posZ = normalizedZ * moveRangeZ;

            transform.position = new Vector3(posX, transform.position.y, posZ);
        }
    }
}
