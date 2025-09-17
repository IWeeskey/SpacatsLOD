using UnityEngine;

namespace Spacats.LOD
{
    [CreateAssetMenu(fileName = "LodMeshGizmoData", menuName = "Gizmos/LodMeshGizmoData")]
    public class LodMeshGizmoData : ScriptableObject
    {
        public Mesh CuboidLodMesh;
        public Material CuboidLodMaterial;

        private static LodMeshGizmoData _instance;

        public static LodMeshGizmoData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LodMeshGizmoData>("LodMeshGizmoData");
                    if (_instance == null)
                    {
                        Debug.LogWarning("LodMeshGizmoData not found in Resources!");
                        return null;
                    }
                }
                return _instance;
            }
        }
    }
}
