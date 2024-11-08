using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ObjectsPlaceTool
{
    public static class PlacerUtils
    {
        // parentの直下の子オブジェクトのうち、T型のコンポーネントを持つものを取得する
        public static void GetDirectChildrenComponent<T>(Transform parent, out List<T> components) where T : Component
        {
            components = new List<T>();
            foreach (var child in parent.GetComponentsInChildren<T>())
            {
                if (child.transform == parent) continue;
                if (child.transform.parent != parent) continue;
                components.Add(child);
            }
        }

        public static void ApplyPreset(TransformInfos preset, List<Transform> transforms, bool isLocal)
        {
            preset.ApplyTransforms(transforms, isLocal);
        }

#if UNITY_EDITOR
        public static void CreatePreset(List<Transform> transforms, bool isLocal)
        {
            var preset = ScriptableObject.CreateInstance<TransformInfos>();
            preset.SaveTransforms(transforms, isLocal);
            var path = EditorUtility.SaveFilePanelInProject("Save User Transforms Preset", "UserTransformsPreset",
                "asset",
                "");
            if (!string.IsNullOrWhiteSpace(path))
            {
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
            }
        }
#endif

        // apply only y
        public static void RotateYToLookAtTarget(Transform target, List<Transform> transforms)
        {
            foreach (Transform transform in transforms)
            {
                Vector3 eulerAngles = transform.eulerAngles;
                transform.LookAt(target);
                eulerAngles.y = transform.eulerAngles.y;
                transform.eulerAngles = eulerAngles;
#if UNITY_EDITOR
                EditorUtility.SetDirty(transform);
#endif
            }
        }

        #region FunctionsToCalcTransform
        // Box内のランダムな位置に配置する。localPositionとlocalRotationとして設定する
        public static Vector3 GetRandomPosInBox(Bounds bounds)
        {
            return new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
        }

        public static Vector3 GetRandomPosInCylinder(CylinderBounds bounds)
        {
            Vector3 center = bounds.Center;
            float radius = bounds.Radius;
            float height = bounds.Height;
            Vector2 xzPos = UnityEngine.Random.insideUnitCircle * radius;
            return new Vector3(xzPos.x, UnityEngine.Random.Range(-height / 2, height / 2), xzPos.y) + center;
        }

        public static Vector3 GetRandomPosInTriangle(IsoscelesTriangleBounds bounds)
        {
            return IsoscelesTriangleBounds.GetRandomPosInIsoscelesTriangle(bounds);
        }

        public static Vector3 GetAlignedPosInBox(Bounds bounds, int row, int column, int index)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float cellWidth = (max.x - min.x) / row;
            float cellDepth = (max.z - min.z) / column;

            int ri = index / column;
            int ci = index % column;
            return new Vector3(
                min.x + cellWidth * ri + cellWidth * 0.5f,
                (max.y + min.y) * 0.5f,
                min.z + cellDepth * ci + cellDepth * 0.5f
            );
        }

        public static Quaternion GetRandomRotationInRange(Vector3 min, Vector3 max)
        {
            return Quaternion.Euler(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y),
                UnityEngine.Random.Range(min.z, max.z));
        }

        public static Vector3 GetRandomValueInRange(Vector3 min, Vector3 max)
        {
            return new Vector3(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y),
                UnityEngine.Random.Range(min.z, max.z));
        }

        public static Vector3 SampleOnNavmesh(Vector3 position, float sampleRadius)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            else
            {
                return position;
            }
        }
        #endregion

#if UNITY_EDITOR
        #region DrawGizmo
        // gizmoにcylinderの輪郭を描画する
        public static void DrawWireCylinder(CylinderBounds cylinder)
        {
            // circleの頂点を取得 分割数は32
            var vertices = new List<Vector3>();
            var center = cylinder.Center;
            var radius = cylinder.Radius;
            var height = cylinder.Height;
            // 上の円
            for (var i = 0; i < 32; i++)
            {
                var rad = Mathf.Deg2Rad * (360f / 32f * i);
                var x = Mathf.Cos(rad) * radius;
                var z = Mathf.Sin(rad) * radius;
                vertices.Add(new Vector3(x, -height / 2, z) + center);
            }

            Gizmos.DrawLineStrip(vertices.ToArray(), true);
            vertices.Clear();
            // 下の円
            for (var i = 0; i < 32; i++)
            {
                var rad = Mathf.Deg2Rad * (360f / 32f * i);
                var x = Mathf.Cos(rad) * radius;
                var z = Mathf.Sin(rad) * radius;
                vertices.Add(new Vector3(x, height / 2, z) + center);
            }

            Gizmos.DrawLineStrip(vertices.ToArray(), true);
        }

        // row * columnのマス目を描画する
        public static void DrawWireGrid(Bounds boxBounds, int rows, int columns)
        {
            var min = boxBounds.min;
            var max = boxBounds.max;
            var width = max.x - min.x;
            var depth = max.z - min.z;
            var cellWidth = width / rows;
            var cellDepth = depth / columns;
            var height = (max.y + min.y) * 0.5f;
            // 横線
            for (var i = 0; i < rows; i++)
            {
                var x = min.x + cellWidth * i + cellWidth / 2;
                Gizmos.DrawLine(new Vector3(x, height, min.z), new Vector3(x, height, max.z));
            }

            // 縦線
            for (var i = 0; i < columns; i++)
            {
                var z = min.z + cellDepth * i + cellDepth / 2;
                Gizmos.DrawLine(new Vector3(min.x, height, z), new Vector3(max.x, height, z));
            }
        }
        
        public static void DrawWireIsoscelesTriangle(IsoscelesTriangleBounds triangle)
        {
            // 三角形の3つの頂点を取得
            Vector3 p1 = triangle.P1;
            Vector3 p2 = triangle.P2;
            Vector3 p3 = triangle.P3;

            // ギズモで三角形を描画
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }
        #endregion
#endif
    }


    [Serializable]
    public class CylinderBounds
    {
        [SerializeField] private Vector3 _center;
        [SerializeField] private float _radius;
        [SerializeField] private float _height;

        public Vector3 Center
        {
            get => _center;
            set => _center = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public float Height
        {
            get => _height;
            set => _height = value;
        }

        public CylinderBounds(Vector3 center, float radius, float height)
        {
            _center = center;
            _radius = radius;
            _height = height;
        }
    }
    
    [Serializable]
    public struct IsoscelesTriangleBounds
    {
        public Vector3 Center;
        public float BaseLength;
        public float EularAngle; // 角度はオイラー角で指定
        public float Angle => EularAngle * Mathf.Deg2Rad; // 角度をラジアンに変換

        public IsoscelesTriangleBounds(Vector2 center, float baseLength, float eularAngle)
        {
            Center = center;
            BaseLength = baseLength;
            EularAngle = eularAngle;
        }
        public Vector3 P1 => new(Center.x - BaseLength / 2, Center.y, Center.z); // 三角形の一つの頂点

        public Vector3 P2 => new(Center.x + BaseLength / 2, Center.y, Center.z); // 三角形のもう一つの頂点

        public Vector3 P3 // 三角形の頂点（二等辺の角度を持つ）
        {
            get
            {
                float height = BaseLength * Mathf.Tan(Angle / 2);
                return new Vector3(Center.x, Center.y, Center.z + height);
            }
        }
        
        private static float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
        }
        
        private static bool IsInsideTriangle(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            bool b1 = Sign(p, p1, p2) < 0.0f;
            bool b2 = Sign(p, p2, p3) < 0.0f;
            bool b3 = Sign(p, p3, p1) < 0.0f;

            return ((b1 == b2) && (b2 == b3));
        }
        
        public static Vector3 GetRandomPosInIsoscelesTriangle(IsoscelesTriangleBounds bounds)
        {
            Vector3 randomPos;
            do
            {
                randomPos = new Vector3(
                    UnityEngine.Random.Range(bounds.P1.x, bounds.P2.x),
                    bounds.Center.y,
                    UnityEngine.Random.Range(bounds.P1.z, bounds.P3.z)
                );
            } while (!IsInsideTriangle(randomPos, bounds.P1, bounds.P2, bounds.P3));

            return randomPos;
        }
    }
}