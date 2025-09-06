using UnityEngine;
namespace Spacats.LOD
{
    [ExecuteInEditMode]
    public abstract class LodUnitReciever: MonoBehaviour
    {
        public abstract void OnLodChanged(int newLevel);
    }
}
