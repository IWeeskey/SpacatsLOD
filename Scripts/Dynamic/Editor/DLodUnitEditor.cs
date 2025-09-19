#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spacats.LOD
{
    [CustomEditor(typeof(DLodUnit), true)]
    [CanEditMultipleObjects]
    public class DLodUnitEditor : Editor
    {
        private List<Color> _lodColors = new List<Color>() { Color.green, Color.yellow, new Color(1f, 0.5f, 0f), Color.red, new Color(1f, 0f, 0.5f) };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLodSettings();
            DrawLodDistances();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            DrawGizmo();
        }

        private void DrawLodSettings()
        {
            DLodUnit targetScript = (DLodUnit)target;

            GUILayout.TextArea("Current LOD: " + targetScript.LODData.CurrentLod);
            GUILayout.TextArea("Registered: " + targetScript.IsRegistered);

            SerializedProperty receiver = serializedObject.FindProperty("_receiver");
            EditorGUILayout.PropertyField(receiver, new GUIContent("Reciever"));
        }

        private void DrawLodDistances()
        {
            SerializedProperty sLodUnitData = serializedObject.FindProperty("LODData");
            SerializedProperty gizmoList = serializedObject.FindProperty("DrawGizmo");

            SerializedProperty lodDistances = sLodUnitData.FindPropertyRelative("Distances");
            SerializedProperty groupIndex = sLodUnitData.FindPropertyRelative("GroupIndex");

            EditorGUILayout.PropertyField(groupIndex, new GUIContent("GroupIndex"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("LOD Distances", EditorStyles.boldLabel);

            for (int i = 0; i < 5; i++)
            {
                DrawGizmoCheck(lodDistances, gizmoList, i);
            }
        }

        private void DrawGizmoCheck(SerializedProperty lodDistances, SerializedProperty gizmoList, int index)
        {
            EditorGUILayout.BeginHorizontal();

            SerializedProperty distance = lodDistances.FindPropertyRelative("Lod" + index.ToString());
            EditorGUILayout.PropertyField(distance, new GUIContent("LOD " + index.ToString() + ":"));

            if (gizmoList == null || !gizmoList.isArray || gizmoList.arraySize == 0)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }

            SerializedProperty element = gizmoList.GetArrayElementAtIndex(index);
            element.boolValue = EditorGUILayout.Toggle("Gizmo", element.boolValue);

            EditorGUILayout.EndHorizontal();
        }
        private void DrawGizmo()
        {
            DLodUnit targetScript = (DLodUnit)target;
            if (targetScript.DrawGizmo.Count == 0) return;

            for (int i = 0; i < 5; i++)
            {
                if (!targetScript.DrawGizmo[i]) continue;
                PaintSphere(i);
            }
        }

        private void PaintSphere(int index)
        {
            DLodUnit targetScript = (DLodUnit)target;

            Color sphereColor = _lodColors[index];
            sphereColor.a = 0.15f;
            Handles.color = sphereColor;

            Handles.SphereHandleCap(
                0,
                targetScript.transform.position,
                Quaternion.identity,
                targetScript.LODData.Distances.GetByIndex(index) * 2f,
                EventType.Repaint
            );
        }
    }
}
#endif
