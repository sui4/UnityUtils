using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace ObjectsPlaceTool
{
    public class ObjectPlacer : MonoBehaviour
    {
        private List<Transform> _placedTransforms = new();
        [SerializeField] private Transform _parent;

        [SerializeField] private PlaceType _placerType;
        [SerializeField] private int _placeCount = 50;
        [SerializeField] private GameObject _placePrefab;

        // parameter for PlaceType.RandomInBox, AlignInBox
        [SerializeField] private Bounds _boxBounds = new(Vector3.zero, Vector3.one);

        // parameter for PlaceType.RandomInCylinder
        [SerializeField] private CylinderBounds _cylinderBounds = new(Vector3.zero, 1f, 0f);

        // parameter for PlaceType.Preset
        [SerializeField] private TransformInfos _preset;

        // parameter for PlaceType.AlignInBox
        [SerializeField] private int _rows = 10;
        [SerializeField] private int _columns = 10;
        
        // parameter for PlaceType.RandomInTriangle
        [SerializeField] private IsoscelesTriangleBounds _triangleBounds = new(Vector2.zero, 3.0f,  45);

        // parameter for Rotation
        [SerializeField] private bool _isRandomRotation = true;
        [SerializeField] private Quaternion _rotation = Quaternion.identity;
        [SerializeField] private Vector3 _minRotation = new(0f, -180f, 0f);
        [SerializeField] private Vector3 _maxRotation = new(0f, 180f, 0f);

        [SerializeField] private bool _addRandomPos;
        [SerializeField] private Vector3 _minPos;
        [SerializeField] private Vector3 _maxPos;
        // parameter for scale
        [SerializeField] private bool _mulRandomScale;
        [SerializeField] private Vector3 _minScale = new(0.9f, 0.9f, 0.9f);
        [SerializeField] private Vector3 _maxScale = new(1.1f, 1.1f, 1.1f);
        
        [SerializeField] private bool _isLocal = true;
        [SerializeField] private bool _sampleOnNavMesh = true;
        [SerializeField] private float _sampleRadius = 1f;
        
        [SerializeField] private bool _useIndexOffset;
        [SerializeField] private int _indexOffset;
        [SerializeField] private int _targetCount = 50;
        public PlaceType PlacerType => _placerType;

        public List<Transform> PlacedTransforms => _placedTransforms;

        // Method to place objects
        public enum PlaceType
        {
            RandomInBox, // Box内のランダムな位置に配置する
            RandomInCylinder, // Sphere内のランダムな位置に配置する
            Preset, // あらかじめ決められた位置に配置する
            AlignInRect, // Rect内に均等に配置する
            OnlyRotation, // 位置はそのままで回転だけ変える
            RandomInTriangle, // Triangle内のランダムな位置に配置する
        }

        private struct PlacerOption
        {
            public bool SampleOnNavMesh;
            public float SampleRadius;
            public bool RandomRotation;
            public Quaternion Rotation;
            public Vector3 MinRotation;
            public Vector3 MaxRotation;

            public bool AddRandomPos;
            public Vector3 MinPos;
            public Vector3 MaxPos;
            public bool MultiplyRandomScale;
            public Vector3 MinScale;
            public Vector3 MaxScale;
        }

        private void OnValidate()
        {
            if (_parent == null)
            {
                _parent = transform;
            }

            PlacerUtils.GetDirectChildrenComponent(_parent, out _placedTransforms);
            _placeCount = Mathf.Max(0, _placeCount);
            _rows = Mathf.Max(1, _rows);
            _columns = Mathf.Max(1, _columns);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Matrix4x4 matrixCache = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Color colorCache = Gizmos.color;
            Gizmos.color = Color.red;
            switch (_placerType)
            {
                case PlaceType.RandomInBox:
                    Gizmos.DrawWireCube(_boxBounds.center, _boxBounds.size);
                    break;
                case PlaceType.RandomInCylinder:
                    PlacerUtils.DrawWireCylinder(_cylinderBounds);
                    break;
                case PlaceType.Preset:
                    break;
                case PlaceType.AlignInRect:
                    // Gizmos.DrawWireCube(_boxBounds.center + transform.position, _boxBounds.size);
                    PlacerUtils.DrawWireGrid(_boxBounds, _rows, _columns);
                    break;
                case PlaceType.RandomInTriangle:
                    PlacerUtils.DrawWireIsoscelesTriangle(_triangleBounds);
                    break;
            }
            Gizmos.color = colorCache;
            Gizmos.matrix = matrixCache;
        }
#endif
        [ContextMenu("PlaceObjects")]
        public void PlaceObjects()
        {
            PlacerUtils.GetDirectChildrenComponent(_parent, out _placedTransforms);
            AdjustObjectNum(_parent, _placeCount, _placedTransforms, _placePrefab);
            PlaceObjects(_placedTransforms, _placerType);
        }

        [ContextMenu("Clear")]
        public void ClearObjects()
        {
            PlacerUtils.GetDirectChildrenComponent(_parent, out _placedTransforms);
            AdjustObjectNum(_parent, 0, _placedTransforms, _placePrefab);
        }

        private static void AdjustObjectNum(Transform parent, int placeCount, List<Transform> placedObjects,
            GameObject prefabToPlace)
        {
            // count < placeCountならobjectを増やす
            for (var i = placedObjects.Count; i < placeCount; i++)
            {
#if UNITY_EDITOR
                var placedObject = PrefabUtility.InstantiatePrefab(prefabToPlace, parent) as GameObject;
#else
                var placedObject = Instantiate(prefabToPlace, parent);
#endif
                placedObject.name = $"{prefabToPlace.name}_{i:D3}";
                placedObjects.Add(placedObject.transform);
            }

            // count > placeCountならplacedObjectを減らす
            for (var i = placedObjects.Count - 1; i >= placeCount; i--)
            {
#if UNITY_EDITOR
                DestroyImmediate(placedObjects[i].gameObject);
#else
                Destroy(placedObjects[i].gameObject);
#endif
                placedObjects.RemoveAt(i);
            }
        }

        private void PlaceObjects(List<Transform> placedObjects, PlaceType placeType)
        {
            var option = new PlacerOption
            {
                SampleOnNavMesh = _sampleOnNavMesh,
                SampleRadius = _sampleRadius,
                RandomRotation = _isRandomRotation,
                Rotation = _rotation,
                MinRotation = _minRotation,
                MaxRotation = _maxRotation,
                AddRandomPos = _addRandomPos,
                MinPos = _minPos,
                MaxPos = _maxPos,
                MultiplyRandomScale = _mulRandomScale,
                MinScale = _minScale,
                MaxScale = _maxScale,
            };
            if (placeType == PlaceType.Preset)
            {
                _preset.ApplyTransforms(placedObjects, _isLocal);
            }
            else
            {
                int startIndex = _useIndexOffset ? _indexOffset : 0;
                int endIndex = _useIndexOffset ? _indexOffset + _targetCount : placedObjects.Count;
                endIndex = Mathf.Min(endIndex, placedObjects.Count);
                for (int i = startIndex; i < endIndex; i++)
                {
                    Transform target = placedObjects[i];
                    Vector3 position = placeType switch
                    {
                        PlaceType.RandomInBox => PlacerUtils.GetRandomPosInBox(_boxBounds),
                        PlaceType.RandomInCylinder => PlacerUtils.GetRandomPosInCylinder(_cylinderBounds),
                        PlaceType.AlignInRect => PlacerUtils.GetAlignedPosInBox(_boxBounds, _rows, _columns, i),
                        PlaceType.OnlyRotation => target.localPosition,
                        PlaceType.RandomInTriangle => PlacerUtils.GetRandomPosInTriangle(_triangleBounds),
                        _ => Vector3.zero,
                    };

                    Quaternion rotation = option.RandomRotation
                        ? PlacerUtils.GetRandomRotationInRange(option.MinRotation, option.MaxRotation)
                        : option.Rotation;

                    if (option.AddRandomPos)
                    {
                        position += PlacerUtils.GetRandomValueInRange(option.MinPos, option.MaxPos);
                    }

                    if (option.SampleOnNavMesh)
                    {
                        position = PlacerUtils.SampleOnNavmesh(position, option.SampleRadius);
                    }

                    if (option.MultiplyRandomScale)
                    {
                        Vector3 scale = target.localScale;
                        Vector3 mul = PlacerUtils.GetRandomValueInRange(option.MinScale, option.MaxScale);
                        target.localScale = Vector3.Scale(scale, mul);
                    }

                    target.SetLocalPositionAndRotation(position, rotation);
                }
            }

#if UNITY_EDITOR
            foreach (Transform target in placedObjects)
            {
                UnityEditor.EditorUtility.SetDirty(target);
            }
#endif
        }

#if UNITY_EDITOR
        public void CreateAndBakePlayersTransform()
        {
            PlacerUtils.GetDirectChildrenComponent(_parent, out _placedTransforms);
            PlacerUtils.CreatePreset(_placedTransforms, _isLocal);
        }
#endif
    }
}