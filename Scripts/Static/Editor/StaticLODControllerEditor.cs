#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Spacats.LOD
{
    [CustomEditor(typeof(StaticLODController), true)]
    public class StaticLODControllerEditor : Editor
    {
        private int _tabIndex = 0;
        private readonly string[] _tabHeaders = { "Static LOD", "Controller info" };
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
            StaticLODController targetScript = (StaticLODController)target;
            GUILayout.TextArea("Is Controller Registered: " + targetScript.IsControllerRegistered);

            SerializedProperty showLogs = serializedObject.FindProperty("ShowLogs");
            EditorGUILayout.PropertyField(showLogs);

            SerializedProperty showCLogs = serializedObject.FindProperty("ShowCLogs");
            EditorGUILayout.PropertyField(showCLogs);
        }

        private void SetDefaultParameters()
        {
            StaticLODController targetScript = (StaticLODController)target;
            targetScript.ExecuteInEditor = true;
            targetScript.UniqueTag = "";
            targetScript.PersistsAtScenes.Clear();
        }

        private void DrawLODFields()
        {
            StaticLODController targetScript = (StaticLODController)target;

            if (targetScript.LodSettings.PerformMeasurements)
            {
                GUILayout.TextArea("Requests: " + targetScript.RequestsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea("Units: " + targetScript.LodUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea("Position: " + targetScript.TargetPosition.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea("Changed: " + targetScript.ChangedLodsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea("Cells: " + targetScript.GetCellsCount().ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea(targetScript.JobTimeResult.Item2);
                GUILayout.TextArea(targetScript.TotalTimeResult.Item2);
            }

            SerializedProperty lodSettings = serializedObject.FindProperty("LodSettings");
            EditorGUILayout.PropertyField(lodSettings);
        }

        private void DrawLODButtons()
        {
            StaticLODController targetScript = (StaticLODController)target;

            if (GUILayout.Button("Clear"))
            {
                targetScript.Clear();
            }

            if (GUILayout.Button("Process Instant"))
            {
                targetScript.ProcessInstant();
            }
        }
    }
}
#endif
