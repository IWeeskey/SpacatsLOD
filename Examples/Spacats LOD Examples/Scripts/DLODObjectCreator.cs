using System.Collections;
using UnityEngine;
using Spacats.Utils;
namespace Spacats.LOD
{
    public class DLODObjectCreator : PlaneGridObjectCreator
    {
        protected override void OnObjectInstantiated(GameObject gObject, int x, int z)
        {
            base.OnObjectInstantiated(gObject, x, z);

            DLodUnit lodUnit = gObject.GetComponent<DLodUnit>();
            lodUnit.RequestRemove();
            lodUnit.RequestAdd();
        }
    }
}