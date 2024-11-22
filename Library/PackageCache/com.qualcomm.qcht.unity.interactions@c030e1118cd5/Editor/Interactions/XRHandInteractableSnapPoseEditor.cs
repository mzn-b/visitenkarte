// /******************************************************************************
//  * File: XRHandInteractableSnapPoseEditor.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using System;
using QCHT.Interactions.Core;
using QCHT.Interactions.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using QCHT.Interactions.Hands;
using Unity.XR.CoreUtils;
using UnityEditor.Search;
using Object = UnityEngine.Object;

namespace QCHT.Interactions.Proximal.Editor
{
    [CustomEditor(typeof(XRHandInteractableSnapPose))]
    public sealed class XRHandInteractableSnapPoseEditor : UnityEditor.Editor
    {
        private sealed class FreezeOption
        {
            public bool X;
            public bool Y;
            public bool Z;
        }

        public VisualTreeAsset m_InspectorXML;
        private VisualElement _inspector;

        private HandGhost _handGhost;
        public HandGhost HandGhost => _handGhost;

        private XRHandInteractableSnapPose _snapPose;

        [SerializeField] private GameObject leftGhost;
        [SerializeField] private GameObject rightGhost;

        private const float HandleSize = 0.005f;
        private static readonly Color s_saveColor = new Color(0.321f, 0.498f, 0.611f);
        private static readonly Color s_editColor = new Color(0.321f, 0.611f, 0.325f);

        private bool _editing;

        #region Editor

        private void OnEnable()
        {
            _snapPose = target as XRHandInteractableSnapPose;

            if (_snapPose == null)
            {
                return;
            }

            var data = _snapPose.Data;
            InstantiateGhost(data.Handedness);

            if (_handGhost != null)
            {
                _handGhost.HandPose = data;
            }

            // Centers scene view on snap pose
            if (SceneView.lastActiveSceneView != null)
            {
                var center = _snapPose.transform.position;
                var size = Vector3.one * 0.2f;
                var bounds = new Bounds(center, size);

                // Can throw null exception on domain reload 
                try
                {
                    SceneView.lastActiveSceneView.Frame(bounds, false);
                }
                catch (NullReferenceException e)
                {
                    Debug.LogWarning("[XRHandInteractableSnapPoseEditor] Failed to frame the current hand pose : " +
                                     $"{e.Message}");
                }
            }
        }

        private void OnDisable()
        {
            if (_handGhost != null)
            {
                DestroyImmediate(_handGhost.gameObject);
                _handGhost = null;
            }
        }

        private void OnSceneGUI()
        {
            var transform = _snapPose.transform;
            var ghostTransform = _handGhost.transform;
            ghostTransform.rotation = transform.rotation;
            ghostTransform.position = transform.position;
            ghostTransform.localScale = ghostTransform.transform.parent == null
                ? transform.localScale
                : transform.localScale.Divide(transform.parent.lossyScale);
            var data = _snapPose.Data;
            
            EditorGUI.BeginChangeCheck();

            if (_editing)
            {
                UpdateJointHandles(_handGhost, data.Scale);
                ApplyGhostToPose(_handGhost, ref data);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _snapPose.Data = data;
                EditorUtility.SetDirty(_snapPose);
            }
            
            if (!Mathf.Approximately(data.Scale, transform.localScale.x))
            {
                data.Scale = transform.localScale.x;
                _snapPose.Data = data;
                EditorUtility.SetDirty(_snapPose);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            _inspector = new VisualElement();
            m_InspectorXML.CloneTree(_inspector);

            const string cStop = "Save";
            const string cEdit = "Edit joints";

            // Edit
            var editButton = _inspector.Q<Button>("editButton");
            editButton.clicked += () =>
            {
                _editing = !_editing;

                editButton.text = _editing ? cStop : cEdit;
                editButton.style.backgroundColor = _editing ? s_editColor : s_saveColor;

                SceneView.RepaintAll();
            };

            // Convert
            var convertButton = _inspector.Q<Button>("convertButton");
            convertButton.clicked += () =>
            {
                var snapPose = target as XRHandInteractableSnapPose;
                if (snapPose == null)
                {
                    return;
                }

                var data = snapPose.Data;
                data.ConvertToNew();
                snapPose.Data = data;

                if (_handGhost != null)
                {
                    _handGhost.HandPose = data;
                }

                EditorUtility.SetDirty(snapPose);
                SceneView.RepaintAll();
            };

            // Flip
            var flipButton = _inspector.Q<Button>("flipButton");
            flipButton.clicked += () =>
            {
                var snapPose = target as XRHandInteractableSnapPose;
                if (snapPose == null)
                {
                    return;
                }

                var data = snapPose.Data;
                data.Flip();
                snapPose.Data = data;

                RefreshGhost(data.Handedness);

                if (_handGhost != null)
                {
                    _handGhost.HandPose = data;
                }

                EditorUtility.SetDirty(snapPose);
                SceneView.RepaintAll();
            };

            // Export
            var exportButton = _inspector.Q<Button>("exportButton");
            exportButton.clicked += () =>
            {
                var snapPose = target as XRHandInteractableSnapPose;
                if (snapPose == null)
                {
                    return;
                }

                var handPose = CreateInstance<HandPose>();
                handPose.FromHandData(snapPose.Data);

                AssetUtils.CreateAssetInSettingsFromObj(handPose, "HandPoses", "NewHandPose");
            };

            // Import
            var importButton = _inspector.Q<Button>("importButton");
            importButton.clicked += () =>
            {
                SearchService.ShowObjectPicker(OnSelectHandler, null, string.Empty, string.Empty, typeof(HandPose));
            };

            return _inspector;
        }

        private void OnSelectHandler(Object selectObject, bool enable)
        {
            var handPose = (HandPose)selectObject;
            if (handPose == null)
            {
                Debug.LogWarning("[XRHandInteractableSnapPoseEditor:OnSelectHandler] " +
                                 "Select object is not HandPose type");
                return;
            }

            var snapPose = target as XRHandInteractableSnapPose;
            if (snapPose == null)
            {
                return;
            }

            snapPose.Data = handPose.ToHandData();

            if (_handGhost != null)
            {
                _handGhost.HandPose = snapPose.Data;
            }
        }

        #endregion

        private void InstantiateGhost(XrHandedness handedness)
        {
            var ghostPrefab = handedness == XrHandedness.XR_HAND_LEFT ? leftGhost : rightGhost;
            var obj = Instantiate(ghostPrefab, null, true);
            obj.hideFlags = HideFlags.HideAndDontSave;
            StageUtility.PlaceGameObjectInCurrentStage(obj);

            _handGhost = obj.GetComponent<HandGhost>();

            if (_handGhost == null)
            {
                Debug.LogWarning("[XRHandInteractableSnapPoseEditor:InstantiateGhost] " +
                                 "Failed to get HandGhost component on instantiated game object");
            }
        }

        internal void RefreshGhost(XrHandedness handedness)
        {
            if (_handGhost != null)
            {
                DestroyImmediate(_handGhost.gameObject);
                _handGhost = null;
            }

            InstantiateGhost(handedness);
        }

        private static void ApplyGhostToPose(HandGhost handGhost, ref HandData data)
        {
            if (data.Space == XrSpace.XR_HAND_LOCAL)
            {
                // Thumb
                data.Thumb.BaseData.Position = handGhost.thumbBase.localPosition;
                data.Thumb.BaseData.Rotation = handGhost.thumbBase.localRotation;
                data.Thumb.MiddleData.Position = handGhost.thumbMiddle.localPosition;
                data.Thumb.MiddleData.Rotation = handGhost.thumbMiddle.localRotation;
                data.Thumb.TopData.Position = handGhost.thumbTop.localPosition;
                data.Thumb.TopData.Rotation = handGhost.thumbTop.localRotation;

                // Index
                data.Index.BaseData.Position = handGhost.indexBase.localPosition;
                data.Index.BaseData.Rotation = handGhost.indexBase.localRotation;
                data.Index.MiddleData.Position = handGhost.indexMiddle.localPosition;
                data.Index.MiddleData.Rotation = handGhost.indexMiddle.localRotation;
                data.Index.TopData.Position = handGhost.indexTop.localPosition;
                data.Index.TopData.Rotation = handGhost.indexTop.localRotation;

                // Middle
                data.Middle.BaseData.Position = handGhost.middleBase.localPosition;
                data.Middle.BaseData.Rotation = handGhost.middleBase.localRotation;
                data.Middle.MiddleData.Position = handGhost.middleMiddle.localPosition;
                data.Middle.MiddleData.Rotation = handGhost.middleMiddle.localRotation;
                data.Middle.TopData.Position = handGhost.middleTop.localPosition;
                data.Middle.TopData.Rotation = handGhost.middleTop.localRotation;

                // Ring
                data.Ring.BaseData.Position = handGhost.ringBase.localPosition;
                data.Ring.BaseData.Rotation = handGhost.ringBase.localRotation;
                data.Ring.MiddleData.Position = handGhost.ringMiddle.localPosition;
                data.Ring.MiddleData.Rotation = handGhost.ringMiddle.localRotation;
                data.Ring.TopData.Position = handGhost.ringTop.localPosition;
                data.Ring.TopData.Rotation = handGhost.ringTop.localRotation;

                // Pinky
                data.Pinky.BaseData.Position = handGhost.pinkyBase.localPosition;
                data.Pinky.BaseData.Rotation = handGhost.pinkyBase.localRotation;
                data.Pinky.MiddleData.Position = handGhost.pinkyMiddle.localPosition;
                data.Pinky.MiddleData.Rotation = handGhost.pinkyMiddle.localRotation;
                data.Pinky.TopData.Position = handGhost.pinkyTop.localPosition;
                data.Pinky.TopData.Rotation = handGhost.pinkyTop.localRotation;
            }
            else
            {
                // Thumb
                data.Thumb.BaseData.Position = handGhost.thumbBase.position;
                data.Thumb.BaseData.Rotation = handGhost.thumbBase.rotation;
                data.Thumb.MiddleData.Position = handGhost.thumbMiddle.position;
                data.Thumb.MiddleData.Rotation = handGhost.thumbMiddle.rotation;
                data.Thumb.TopData.Position = handGhost.thumbTop.position;
                data.Thumb.TopData.Rotation = handGhost.thumbTop.rotation;

                // Index
                data.Index.BaseData.Position = handGhost.indexBase.position;
                data.Index.BaseData.Rotation = handGhost.indexBase.rotation;
                data.Index.MiddleData.Position = handGhost.indexMiddle.position;
                data.Index.MiddleData.Rotation = handGhost.indexMiddle.rotation;
                data.Index.TopData.Position = handGhost.indexTop.position;
                data.Index.TopData.Rotation = handGhost.indexTop.rotation;

                // Middle
                data.Middle.BaseData.Position = handGhost.middleBase.position;
                data.Middle.BaseData.Rotation = handGhost.middleBase.rotation;
                data.Middle.MiddleData.Position = handGhost.middleMiddle.position;
                data.Middle.MiddleData.Rotation = handGhost.middleMiddle.rotation;
                data.Middle.TopData.Position = handGhost.middleTop.position;
                data.Middle.TopData.Rotation = handGhost.middleTop.rotation;

                // Ring
                data.Ring.BaseData.Position = handGhost.ringBase.position;
                data.Ring.BaseData.Rotation = handGhost.ringBase.rotation;
                data.Ring.MiddleData.Position = handGhost.ringMiddle.position;
                data.Ring.MiddleData.Rotation = handGhost.ringMiddle.rotation;
                data.Ring.TopData.Position = handGhost.ringTop.position;
                data.Ring.TopData.Rotation = handGhost.ringTop.rotation;

                // Pinky
                data.Pinky.BaseData.Position = handGhost.pinkyBase.position;
                data.Pinky.BaseData.Rotation = handGhost.pinkyBase.rotation;
                data.Pinky.MiddleData.Position = handGhost.pinkyMiddle.position;
                data.Pinky.MiddleData.Rotation = handGhost.pinkyMiddle.rotation;
                data.Pinky.TopData.Position = handGhost.pinkyTop.position;
                data.Pinky.TopData.Rotation = handGhost.pinkyTop.rotation;
            }
        }

        private static void UpdateJointHandles(HandGhost handGhost, float scale)
        {
            // Thumb 
            UpdateJointHandle(handGhost.thumbBase, scale, new FreezeOption());
            UpdateJointHandle(handGhost.thumbMiddle, scale, new FreezeOption { Z = true, Y = true });
            UpdateJointHandle(handGhost.thumbTop, scale, new FreezeOption { Z = true, Y = true });

            // Index 
            UpdateJointHandle(handGhost.indexBase, scale, new FreezeOption { Y = true });
            UpdateJointHandle(handGhost.indexMiddle, scale, new FreezeOption { Z = true, Y = true });
            UpdateJointHandle(handGhost.indexTop, scale, new FreezeOption { Z = true, Y = true });

            // Middle
            UpdateJointHandle(handGhost.middleBase, scale, new FreezeOption { Y = true });
            UpdateJointHandle(handGhost.middleMiddle, scale, new FreezeOption { Z = true, Y = true });
            UpdateJointHandle(handGhost.middleTop, scale, new FreezeOption { Z = true, Y = true });

            // Ring
            UpdateJointHandle(handGhost.ringBase, scale, new FreezeOption { Y = true });
            UpdateJointHandle(handGhost.ringMiddle, scale, new FreezeOption { Z = true, Y = true });
            UpdateJointHandle(handGhost.ringTop, scale, new FreezeOption { Z = true, Y = true });

            // Pinky
            UpdateJointHandle(handGhost.pinkyBase, scale, new FreezeOption { Y = true });
            UpdateJointHandle(handGhost.pinkyMiddle, scale, new FreezeOption { Z = true, Y = true });
            UpdateJointHandle(handGhost.pinkyTop, scale, new FreezeOption { Z = true, Y = true });
        }

        private static void UpdateJointHandle(Transform transform, float scale, FreezeOption freeze = null)
        {
            if (freeze is not { X: true })
            {
                UpdateAxisDiscHandle(transform, transform.right, Handles.xAxisColor, scale);
            }

            if (freeze is not { Y: true })
            {
                UpdateAxisDiscHandle(transform, transform.forward, Handles.yAxisColor, scale);
            }

            if (freeze is not { Z: true })
            {
                UpdateAxisDiscHandle(transform, transform.up, Handles.zAxisColor, scale);
            }
        }

        private static void UpdateAxisDiscHandle(Transform transform, Vector3 dir, Color color, float scale)
        {
            Handles.color = color;
            transform.rotation =
                Handles.Disc(transform.rotation, transform.position, dir, HandleSize * scale, false, 0f);
        }
    }
}