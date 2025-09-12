using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spacats.Utils;

namespace Spacats.LOD
{
    public class GUISceneSwitcher : GUIButtons
    {
        public string SceneToLoad = "";
        public Transform LodTarget;

        public bool SetDynamicLODTarget = false;
        public bool SetStaticLODTarget = false;

        private void Awake()
        {
            if (SetDynamicLODTarget) DynamicLODController.Instance.SetTarget(LodTarget);
            if (SetStaticLODTarget) StaticLODController.Instance.SetTarget(LodTarget);
        }

        protected override string GetButtonLabel(int index)
        {
            switch (index)
            {
                case 0: return "Load " + SceneToLoad;
            }
            return base.GetButtonLabel(index);
        }

        protected override void OnButtonClick(int index)
        {
            switch (index)
            {
                case 0: SceneLoaderHelper.LoadScene(SceneToLoad); break;
            }
        }
    }
}