using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ObjectsPlaceTool
{
    [Serializable]
    public class TransformInfo
    {
        [SerializeField] private Vector3 _position;
        [SerializeField] private Quaternion _rotation;

        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }
    }


    [CreateAssetMenu(fileName = "TransformInfoList", menuName = "ScriptableObjects/TransformInfoList",
        order = 3)]
    public class TransformInfos : ScriptableObject
    {
        [SerializeField] private List<TransformInfo> _transformInfoList;

        public List<TransformInfo> List
        {
            get => _transformInfoList;
            set => _transformInfoList = value;
        }

        public void SaveTransforms(List<Transform> transformList, bool isLocal)
        {
            _transformInfoList = new List<TransformInfo>();
            // 冗長だが、ループ内で毎回if分岐をするよりはよいだろう
            if (isLocal)
            {
                foreach (Transform transform in transformList)
                {
                    _transformInfoList.Add(new TransformInfo()
                    {
                        Position = transform.localPosition,
                        Rotation = transform.localRotation
                    });
                }
            }
            else
            {
                foreach (Transform transform in transformList)
                {
                    _transformInfoList.Add(new TransformInfo()
                    {
                        Position = transform.position,
                        Rotation = transform.rotation
                    });
                }
            }
        }

        public void ApplyTransforms(List<Transform> transforms, bool isLocal)
        {
            if (transforms.Count > _transformInfoList.Count)
            {
                Debug.LogWarning(
                    "TransformInfos: given transforms count is greater than target info count.\n" +
                    "Some data will not be applied.");
            }

            if (isLocal)
            {
                for (int ti = 0; ti < transforms.Count && ti < _transformInfoList.Count; ti++)
                {
                    var data = _transformInfoList[ti];
                    transforms[ti].SetLocalPositionAndRotation(data.Position, data.Rotation);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(transforms[ti]);
#endif
                }
            }
            else
            {
                for (int ti = 0; ti < transforms.Count && ti < _transformInfoList.Count; ti++)
                {
                    var data = _transformInfoList[ti];
                    transforms[ti].SetPositionAndRotation(data.Position, data.Rotation);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(transforms[ti]);
#endif
                }
            }
        }
    }
}