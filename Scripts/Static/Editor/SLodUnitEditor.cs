#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Spacats.LOD
{
    [CustomEditor(typeof(SLodUnit), true)]
    [CanEditMultipleObjects]
    public class SLodUnitEditor : Editor
    {
        private List<Color> _lodColors = new List<Color>() { Color.green, Color.yellow, new Color(1f,0.5f,0f), Color.red, new Color(1f, 0f, 0.5f) };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLodSettings();
            DrawLodDistances();
            DrawCuboidSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            DrawGizmo();
        }

        private void DrawLodSettings()
        {
            SLodUnit targetScript = (SLodUnit)target;
            GUILayout.TextArea("Current LOD: " + targetScript.LODData.CurrentLod);

            SerializedProperty registerOnEnable = serializedObject.FindProperty("RegisterOnEnable");
            EditorGUILayout.PropertyField(registerOnEnable, new GUIContent("Register On Enable"));

            SerializedProperty receiver = serializedObject.FindProperty("_receiver");
            EditorGUILayout.PropertyField(receiver, new GUIContent("Reciever"));
        }

        private void DrawLodDistances()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("LOD Distances", EditorStyles.boldLabel);

            SerializedProperty sLodUnitData = serializedObject.FindProperty("LODData");
            SerializedProperty gizmoList = serializedObject.FindProperty("DrawGizmo");

            SerializedProperty lodDistances = sLodUnitData.FindPropertyRelative("Distances");

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

        private void DrawCuboidSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cuboid Settings", EditorStyles.boldLabel);
            SerializedProperty sLodUnitData = serializedObject.FindProperty("LODData");

            SerializedProperty cuboidCalculations = sLodUnitData.FindPropertyRelative("CuboidCalculations");
            EditorGUILayout.PropertyField(cuboidCalculations, new GUIContent("Cuboid Calculations"));

            if (cuboidCalculations.boolValue)
            {
                SerializedProperty cuboidData = sLodUnitData.FindPropertyRelative("CuboidData");

                SerializedProperty cuboidSize = cuboidData.FindPropertyRelative("Size");
                EditorGUILayout.PropertyField(cuboidSize, new GUIContent("Cuboid Size"));
            }
        }
        private void DrawGizmo()
        {
            SLodUnit targetScript = (SLodUnit)target;

            for (int i = 0; i < 5; i++)
            {
                if (!targetScript.DrawGizmo[i]) continue;

                PaintSphere(i);
            }
        }

        private void PaintSphere(int index)
        {
            SLodUnit targetScript = (SLodUnit)target;
            if (targetScript.DrawGizmo.Count==0) return;
            if (!targetScript.DrawGizmo[index]) return;

            Color sphereColor= _lodColors[index];
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
