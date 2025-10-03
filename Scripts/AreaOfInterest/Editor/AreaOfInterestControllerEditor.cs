#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Spacats.LOD
{
    [CustomEditor(typeof(AreaOfInterestController), true)]
    public class AreaOfInterestControllerEditor : Editor
    {
        private int _tabIndex = 0;
        private readonly string[] _tabHeaders = { "AreaOfInterest", "Controller info"};
        public override void OnInspectorGUI()
        {
            SetDefaultParameters();
            serializedObject.Update();
            _tabIndex = GUILayout.Toolbar(_tabIndex, _tabHeaders);
            EditorGUILayout.Space();

            switch (_tabIndex)
            {
                case 0:
                    DrawAOITab();
                    break;
                case 1:
                    DrawControllerTab();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAOITab()
        {
            DrawAOTFields();
            DrawAOTButtons();
        }

        private void DrawControllerTab()
        {
            AreaOfInterestController targetScript = (AreaOfInterestController)target;
            GUILayout.TextArea("Is Controller Registered: " + targetScript.IsControllerRegistered);

            SerializedProperty showLogs = serializedObject.FindProperty("ShowLogs");
            EditorGUILayout.PropertyField(showLogs);

            SerializedProperty showCLogs = serializedObject.FindProperty("ShowCLogs");
            EditorGUILayout.PropertyField(showCLogs);
        }

        private void SetDefaultParameters()
        {
            AreaOfInterestController targetScript = (AreaOfInterestController)target;
            targetScript.ExecuteInEditor = true;
            targetScript.UniqueTag = "";
            targetScript.PersistsAtScenes.Clear();
        }

        private void DrawAOTFields()
        {
            AreaOfInterestController targetScript = (AreaOfInterestController)target;

            if (targetScript.AOISettings.PerformMeasurements)
            {
                GUILayout.TextArea("DUnits: " + targetScript.DUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea("SUnits: " + targetScript.SUnitsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                
                // GUILayout.TextArea("Requests: " + targetScript.RequestsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));

                // GUILayout.TextArea("Transforms: " + targetScript.TransformCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                // GUILayout.TextArea("Position: " + targetScript.TargetPosition.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                // GUILayout.TextArea("Changed: " + targetScript.ChangedLodsCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                // GUILayout.TextArea("Cells: " + targetScript.GetCellsCount().ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "));
                GUILayout.TextArea(targetScript.StaticResult.Item2);
                GUILayout.TextArea(targetScript.DynamicResult.Item2);
            }
            
            SerializedProperty aoiSettings = serializedObject.FindProperty("AOISettings");
            EditorGUILayout.PropertyField(aoiSettings);
        }

        private void DrawAOTButtons()
        {
            AreaOfInterestController targetScript = (AreaOfInterestController)target;

            // if (GUILayout.Button("Clear"))
            // {
            //     targetScript.Clear();
            // }
        }
    }
}
#endif
