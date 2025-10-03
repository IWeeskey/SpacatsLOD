#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spacats.LOD
{
    [CustomEditor(typeof(SLodUnit), true)]
    [CanEditMultipleObjects]
    public class SLodUnitEditor : Editor
    {
        private List<Color> _lodColors = new List<Color>() { Color.green, Color.yellow, new Color(1f,0.5f,0f), Color.red, new Color(1f, 0f, 0.5f) };

        SerializedProperty _aoiDataProp;
        SerializedProperty _aoiAutoUpdateProp;
        
        bool _oldAOIValue = false;
        
        void OnEnable()
        {
            _aoiDataProp = serializedObject.FindProperty("_aoiData");
            _aoiAutoUpdateProp = _aoiDataProp.FindPropertyRelative("AutoUpdate");
            _oldAOIValue = _aoiAutoUpdateProp.boolValue;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CheckAOTAutoUpdate();
            DrawLodSettings();
            DrawAOISettings();
            DrawLodDistances();
            DrawCuboidSettings();
            DrawButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            DrawGizmo();
        }

        private void CheckAOTAutoUpdate()
        {
            if (_aoiAutoUpdateProp.boolValue == _oldAOIValue) return;
            _oldAOIValue = _aoiAutoUpdateProp.boolValue;
            
            SLodUnit targetScript = (SLodUnit)target;
            
            if (_oldAOIValue) targetScript.TryRegisterAOI();
            else targetScript.TryUnregisterAOI();
        }
        
        private void DrawLodSettings()
        {
            SLodUnit targetScript = (SLodUnit)target;

            GUILayout.TextArea("Current LOD: " + targetScript.LODData.CurrentLod);
            GUILayout.TextArea("Registered: " + targetScript.IsRegistered);

            SerializedProperty registerOnEnable = serializedObject.FindProperty("RegisterOnEnable");
            EditorGUILayout.PropertyField(registerOnEnable, new GUIContent("Register On Enable"));

            SerializedProperty receiver = serializedObject.FindProperty("_receiver");
            EditorGUILayout.PropertyField(receiver, new GUIContent("Reciever"));
        }
        
        private void DrawAOISettings()
        {
            EditorGUILayout.Space();
            SerializedProperty aoiData = serializedObject.FindProperty("_aoiData");
            EditorGUILayout.PropertyField(aoiData, new GUIContent("AOI"));
        }

        private void DrawLodDistances()
        {
            EditorGUILayout.Space();
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
            if (targetScript.DrawGizmo.Count == 0) return;

            for (int i = 0; i < 5; i++)
            {
                if (!targetScript.DrawGizmo[i]) continue;

                if (!targetScript.LODData.CuboidCalculations) PaintSphere(i);
                else PaintChamferedCuboid(i);
            }
        }

        private void PaintSphere(int index)
        {
            SLodUnit targetScript = (SLodUnit)target;

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

        private void PaintChamferedCuboid(int index)
        {
            SLodUnit targetScript = (SLodUnit)target;
            float distance = targetScript.LODData.Distances.GetByIndex(index);
            Vector3 size = targetScript.LODData.CuboidData.Size + distance*2f;

            Color color = _lodColors[index];
            color.a = 0.01f;

            Transform t = targetScript.transform.transform;

            Matrix4x4 matrix = Matrix4x4.TRS(t.position, t.rotation, size);
            Camera cam = SceneView.currentDrawingSceneView != null
                ? SceneView.currentDrawingSceneView.camera
                : Camera.current;

            if (cam == null) return;
            if (LodMeshGizmoData.Instance == null) return;

            Material drawMat = LodMeshGizmoData.Instance.CuboidLodMaterial;

            if (drawMat.HasProperty("_TintColor")) drawMat.SetColor("_TintColor", color);
            if (drawMat.HasProperty("_Color")) drawMat.SetColor("_Color", color);

            var prevZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
           
            Graphics.DrawMesh(LodMeshGizmoData.Instance.CuboidLodMesh, matrix, LodMeshGizmoData.Instance.CuboidLodMaterial, 0, cam, 0, null, ShadowCastingMode.Off, false, null);

            Handles.zTest = prevZTest;
        }

        private void DrawButtons()
        {
            SLodUnit targetScript = (SLodUnit)target;

            if (GUILayout.Button("REFRESH"))
            {
                targetScript.Refresh();
            }
        }
    }
}
#endif
