using System.Collections;
using UnityEngine;
using Spacats.Utils;
namespace Spacats.LOD
{
    public class SLODObjectCreator : PlaneGridObjectCreator
    {
        protected override void OnObjectInstantiated(GameObject gObject, int x, int z)
        {
            base.OnObjectInstantiated(gObject, x, z);

            SLodUnit lodUnit = gObject.GetComponent<SLodUnit>();
            lodUnit.RequestAddLOD();
        }
    }
}
