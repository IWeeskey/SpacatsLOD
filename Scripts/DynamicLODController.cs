using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spacats.Utils;

namespace Spacats.LOD
{
    [DefaultExecutionOrder(-10)]
    public class DynamicLODController : Controller
    {
        private static DynamicLODController _instance;

        public static DynamicLODController Instance
        {
            get
            {
                if (_instance == null) Debug.LogError("DynamicLODController is not registered yet!");
                return _instance;
            }
        }

        protected override void COnRegister()
        {
            base.COnRegister();
            _instance = this;
        }

        protected override void COnEnable()
        {
            base.COnEnable();
        }

        protected override void COnDisable()
        {
            base.COnDisable();
        }
    }
}
