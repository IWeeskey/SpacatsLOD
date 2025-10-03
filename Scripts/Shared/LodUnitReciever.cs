using System.Collections.Generic;
using UnityEngine;
namespace Spacats.LOD
{
    [ExecuteInEditMode]
    public abstract class LodUnitReciever: MonoBehaviour
    {
        public abstract void OnLodChanged(int newLevel);
        public abstract void OnDNeighboursChanged(List<DLodUnit> newNeighbours);
        public abstract void OnSNeighboursChanged(List<SLodUnit> newNeighbours);
    }
}
