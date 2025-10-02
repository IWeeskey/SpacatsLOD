#if UNITY_EDITOR
using Spacats.LOD;
using UnityEditor;
using UnityEngine;

namespace Spacats.Utils
{
    [CustomEditor(typeof(DLODObjectCreator))]
    public class DLODObjectCreatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DLODObjectCreator targetScript = (DLODObjectCreator)target;

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