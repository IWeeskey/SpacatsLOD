#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Spacats.LOD
{
    [CustomEditor(typeof(DynamicLODController), true)]
    public class DynamicLODControllerEditor : Editor
    {
        private int _tabIndex = 0;
        private readonly string[] _tabHeaders = { "Dynamic LOD", "Controller info"};
        public override void OnInspectorGUI()
        {
            SetDefaultParameters();
            serializedObject.Update();
            _tabIndex = GUILayout.Toolbar(_tabIndex, _tabHeaders);
            EditorGUILayout.Space();

            switch (_tabIndex)
            {
                case 0:
                    DrawLODTab();
                    break;
                case 1:
                    DrawControllerTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLODTab()
        {
            DrawLODFields();
            DrawLODButtons();
        }

        private void DrawControllerTab()
        {
            DynamicLODController targetScript = (DynamicLODController)target;
            GUILayout.TextArea("Is Controller Registered: " + targetScript.IsControllerRegistered);

            SerializedProperty showLogs = serializedObject.FindProperty("ShowLogs");
            EditorGUILayout.PropertyField(showLogs);

            SerializedProperty showCLogs = serializedObject.FindProperty("ShowCLogs");
            EditorGUILayout.PropertyField(showCLogs);
        }

        private void SetDefaultParameters()
        {
            DynamicLODController targetScript = (DynamicLODController)target;
            targetScript.ExecuteInEditor = true;
            targetScript.UniqueTag = "";
            targetScript.PersistsAtScenes.Clear();
        }

        private void DrawLODFields()
        {
            DynamicLODController targetScript = (DynamicLODController)target;
            
            GUILayout.TextArea("Requests: " + targetScript.RequestsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
            GUILayout.TextArea("Units: " + targetScript.LodUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
            GUILayout.TextArea("Transforms: " + targetScript.TransformCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
            GUILayout.TextArea("Position: " + targetScript.TargetPosition.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));

            if (targetScript.PerformMeasurements)
            {
                GUILayout.TextArea(targetScript.MeasurementsResult.Item2);
            }

            SerializedProperty performMeasurements = serializedObject.FindProperty("PerformMeasurements");
            EditorGUILayout.PropertyField(performMeasurements);

            SerializedProperty performLogic = serializedObject.FindProperty("PerformLogic");
            EditorGUILayout.PropertyField(performLogic);

            SerializedProperty useEditorCamera = serializedObject.FindProperty("UseEditorCamera");
            EditorGUILayout.PropertyField(useEditorCamera);

            SerializedProperty asyncLogic = serializedObject.FindProperty("AsyncLogic");
            EditorGUILayout.PropertyField(asyncLogic);

            SerializedProperty maxUnitCount = serializedObject.FindProperty("MaxUnitCount");
            EditorGUILayout.PropertyField(maxUnitCount);

            SerializedProperty updateRateMS = serializedObject.FindProperty("UpdateRateMS");
            EditorGUILayout.PropertyField(updateRateMS);

            SerializedProperty targetTransform = serializedObject.FindProperty("Target");
            EditorGUILayout.PropertyField(targetTransform);


        }

        private void DrawLODButtons()
        {
            DynamicLODController targetScript = (DynamicLODController)target;

            if (GUILayout.Button("Clear"))
            {
                targetScript.Clear();
            }

            if (GUILayout.Button("Process"))
            {
                targetScript.ProcessLODs();
            }

            if (GUILayout.Button("Process Time"))
            {
                targetScript.ProcessLODs(true);
            }
        }
    }
}
#endif
