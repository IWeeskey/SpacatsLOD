#if UNITY_EDITOR
using Spacats.LOD;
using UnityEditor;
using UnityEngine;

namespace Spacats.Utils
{
    [CustomEditor(typeof(SLODObjectCreator))]
    public class SLODObjectCreatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SLODObjectCreator targetScript = (SLODObjectCreator)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Create Immediate"))
            {
                targetScript.GenerateImmediate();
            }

            if (GUILayout.Button("Clear"))
            {
                targetScript.Clear();
            }
        }

    }
}
#endif
