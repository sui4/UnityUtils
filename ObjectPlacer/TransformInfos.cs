using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace ObjectsPlaceTool
{
    [Serializable]
    public class TransformInfo
    {
        [SerializeField] private Vector3 _position = Vector3.zero;
        [SerializeField] private Quaternion _rotation = quaternion.identity;
        [SerializeField] private Vector3 _scale = Vector3.one;
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

        public Vector3 Scale
        {
            get => _scale;
            set => _scale = value;
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
                        Rotation = transform.localRotation,
                        Scale = transform.localScale
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
                        Rotation = transform.rotation,
                        Scale = transform.lossyScale
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
                for (var ti = 0; ti < transforms.Count && ti < _transformInfoList.Count; ti++)
                {
                    TransformInfo data = _transformInfoList[ti];
                    transforms[ti].SetLocalPositionAndRotation(data.Position, data.Rotation);
                    transforms[ti].localScale = data.Scale;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(transforms[ti]);
#endif
                }
            }
            else
            {
                for (var ti = 0; ti < transforms.Count && ti < _transformInfoList.Count; ti++)
                {
                    TransformInfo data = _transformInfoList[ti];
                    transforms[ti].SetPositionAndRotation(data.Position, data.Rotation);
                    Transform parent = transforms[ti].parent;
                    if (parent)
                    {
                        Vector3 parentLossyScale = parent.lossyScale;
                        transforms[ti].localScale = new Vector3(
                            data.Scale.x / parentLossyScale.x,
                            data.Scale.y / parentLossyScale.y,
                            data.Scale.z / parentLossyScale.z
                            );
                    }
                    else
                    {
                        transforms[ti].localScale = data.Scale;
                    }
#if UNITY_EDITOR
                    EditorUtility.SetDirty(transforms[ti]);
#endif
                }
            }
        }
    }
}