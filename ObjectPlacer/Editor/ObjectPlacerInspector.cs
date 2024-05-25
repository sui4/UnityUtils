using System;
using UnityEditor;
using UnityEngine;

namespace ObjectsPlaceTool
{
    [CustomEditor(typeof(ObjectPlacer))]
    public class ObjectPlacerInspector : Editor
    {
        private SerializedProperty _parentProp;
        private SerializedProperty _placerTypeProp;

        private SerializedProperty _placeCountProp;
        private SerializedProperty _prefabToPlaceProp;

        private SerializedProperty _boxBoundsProp;
        private SerializedProperty _cylinderBoundsProp;
        private SerializedProperty _presetProp;
        private SerializedProperty _rowsProp;
        private SerializedProperty _columnsProp;
        private SerializedProperty _triangleBoundsProp;
        // rotation
        private SerializedProperty _isRandomRotationProp;
        private SerializedProperty _rotation;
        private SerializedProperty _minRotation;
        private SerializedProperty _maxRotation;
        // random offset pos
        private SerializedProperty _addRandomPosProp;
        private SerializedProperty _minPos;
        private SerializedProperty _maxPos;

        private SerializedProperty _isLocalProp;
        private SerializedProperty _sampleOnNavMeshProp;
        private SerializedProperty _sampleOnNavMeshRadiusProp;

        private SerializedProperty _useIndexOffsetProp;
        private SerializedProperty _indexOffsetProp;
        private SerializedProperty _targetCountProp;
        private ObjectPlacer Target => target as ObjectPlacer;

        private void OnEnable()
        {
            _parentProp = serializedObject.FindProperty("_parent");
            _placerTypeProp = serializedObject.FindProperty("_placerType");
            _placeCountProp = serializedObject.FindProperty("_placeCount");
            _prefabToPlaceProp = serializedObject.FindProperty("_placePrefab");
            // for placer
            _boxBoundsProp = serializedObject.FindProperty("_boxBounds");
            _cylinderBoundsProp = serializedObject.FindProperty("_cylinderBounds");
            _presetProp = serializedObject.FindProperty("_preset");
            _rowsProp = serializedObject.FindProperty("_rows");
            _columnsProp = serializedObject.FindProperty("_columns");
            _triangleBoundsProp = serializedObject.FindProperty("_triangleBounds");
            _isRandomRotationProp = serializedObject.FindProperty("_isRandomRotation");
            _rotation = serializedObject.FindProperty("_rotation");
            _minRotation = serializedObject.FindProperty("_minRotation");
            _maxRotation = serializedObject.FindProperty("_maxRotation");
            // random offset pos
            _addRandomPosProp = serializedObject.FindProperty("_addRandomPos");
            _minPos = serializedObject.FindProperty("_minPos");
            _maxPos = serializedObject.FindProperty("_maxPos");
            // shared
            _isLocalProp = serializedObject.FindProperty("_isLocal");
            _sampleOnNavMeshProp = serializedObject.FindProperty("_sampleOnNavMesh");
            _sampleOnNavMeshRadiusProp = serializedObject.FindProperty("_sampleRadius");
            
            _useIndexOffsetProp = serializedObject.FindProperty("_useIndexOffset");
            _indexOffsetProp = serializedObject.FindProperty("_indexOffset");
            _targetCountProp = serializedObject.FindProperty("_targetCount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                BaseGUI();
                EditorGUILayout.Separator();
                EditorGUI.indentLevel++;
                switch (Target.PlacerType)
                {
                    case ObjectPlacer.PlaceType.RandomInBox:
                        RandomInBoxGUI();
                        break;
                    case ObjectPlacer.PlaceType.RandomInCylinder:
                        RandomInCylinderGUI();
                        break;
                    case ObjectPlacer.PlaceType.Preset:
                        PresetGUI();
                        break;
                    case ObjectPlacer.PlaceType.AlignInRect:
                        AlignInBoxGUI();
                        break;
                    case ObjectPlacer.PlaceType.OnlyRotation:
                        OnlyRotationGUI();
                        break;
                    case ObjectPlacer.PlaceType.RandomInTriangle:
                        RandomInTriangleGUI();
                        break;
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Separator();

                if (GUILayout.Button("Place Objects"))
                {
                    Target.PlaceObjects();
                }

                EditorGUILayout.Separator();

                ExportGUI();

                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void BaseGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target),
                typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(_parentProp, new GUIContent("Parent to place"));

            EditorGUILayout.PropertyField(_placeCountProp, new GUIContent("Total Count"));
            EditorGUILayout.PropertyField(_useIndexOffsetProp, new GUIContent("Use Index Offset"));
            if (_useIndexOffsetProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_targetCountProp, new GUIContent("Target Count"));
                EditorGUILayout.PropertyField(_indexOffsetProp, new GUIContent("Index Offset"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_prefabToPlaceProp, new GUIContent("Prefab to place"));

            EditorGUILayout.PropertyField(_placerTypeProp, new GUIContent("Placer Type"));
        }

        private void RandomInBoxGUI()
        {
            EditorGUILayout.PropertyField(_boxBoundsProp);
            OptionGUI();
        }

        private void RandomInCylinderGUI()
        {
            var pos = _cylinderBoundsProp.FindPropertyRelative("_center");
            var radius = _cylinderBoundsProp.FindPropertyRelative("_radius");
            var height = _cylinderBoundsProp.FindPropertyRelative("_height");
            EditorGUILayout.LabelField("Cylinder Bounds");
            EditorGUILayout.PropertyField(pos);
            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(height);
            OptionGUI();
        }

        private void PresetGUI()
        {
            EditorGUILayout.PropertyField(_presetProp);
            EditorGUILayout.PropertyField(_isLocalProp, new GUIContent("Use Local Transform"));
        }

        private void AlignInBoxGUI()
        {
            EditorGUILayout.PropertyField(_boxBoundsProp);
            EditorGUILayout.PropertyField(_rowsProp);
            EditorGUILayout.PropertyField(_columnsProp);
            OptionGUI();
        }

        private void RandomInTriangleGUI()
        {
            EditorGUILayout.PropertyField(_triangleBoundsProp);
            OptionGUI();
        }
        private void OnlyRotationGUI()
        {
            OptionGUI();
        }

        private void OptionGUI()
        {
            RotationGUI();
            AddPosGUI();
            SampleOnNavmeshGUI();
        }
        
        private void SampleOnNavmeshGUI()
        {
            EditorGUILayout.PropertyField(_sampleOnNavMeshProp);
            if (_sampleOnNavMeshProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_sampleOnNavMeshRadiusProp);
                EditorGUI.indentLevel--;
            }
        }

        private void RotationGUI()
        {
            EditorGUILayout.PropertyField(_isRandomRotationProp);
            if (_isRandomRotationProp.boolValue)
            {
                EditorGUILayout.PropertyField(_minRotation);
                EditorGUILayout.PropertyField(_maxRotation);
            }
            else
            {
                EditorGUILayout.PropertyField(_rotation);
            }
        }

        private void AddPosGUI()
        {
            EditorGUILayout.PropertyField(_addRandomPosProp);
            if (_addRandomPosProp.boolValue)
            {
                EditorGUILayout.PropertyField(_minPos);
                EditorGUILayout.PropertyField(_maxPos);
            }
        }
        private void ExportGUI()
        {
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_isLocalProp, new GUIContent("Use Local Transform"));
            if (GUILayout.Button("Bake Current Children's Transform as Preset"))
            {
                Target.CreateAndBakePlayersTransform();
            }

            EditorGUI.indentLevel--;
        }
    }
}