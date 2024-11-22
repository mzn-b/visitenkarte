// /******************************************************************************
//  * File: XRHandInteractableSnapPoseProviderEditor.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using QCHT.Interactions.Core;
using QCHT.Interactions.Hands;
using QCHT.Interactions.Extensions;

namespace QCHT.Interactions.Proximal.Editor
{
    [CustomEditor(typeof(XRHandInteractableSnapPoseProvider))]
    public sealed class XRHandInteractableSnapPoseProviderEditor : UnityEditor.Editor
    {
        private VisualElement _inspector;

        private XRHandInteractableSnapPoseProvider _provider;
        private XRHandInteractableSnapPoseEditor _snapPoseEditor;

        private float _scale = 1f;

        #region Editor

        private void OnEnable()
        {
            _provider = target as XRHandInteractableSnapPoseProvider;

            if (_provider == null)
            {
                return;
            }

            _provider.FindPoses();
            _provider.SanitizePoseList();

            if (_snapPoseEditor == null && _provider.Poses.Count > 0)
            {
                _snapPoseEditor = CreateEditor(_provider.Poses[0]) as XRHandInteractableSnapPoseEditor;
            }
        }

        private void OnDestroy()
        {
            if (_snapPoseEditor != null)
            {
                DestroyImmediate(_snapPoseEditor);
            }
        }

        private void OnSceneGUI()
        {
            if (_provider == null || _snapPoseEditor == null)
            {
                return;
            }

            var handGhost = _snapPoseEditor.HandGhost;
            if (handGhost == null)
            {
                return;
            }

            var data = new HandData();
            var rootOffset = new Pose();

            if (!_provider.TryGetInterpolatedHandPoseFromScale(ref data, ref rootOffset, _scale))
            {
                return;
            }

            handGhost.HandPose = data;

            var transform = _provider.transform;
            var ghostTransform = handGhost.transform;

            var posOffset = rootOffset.position;
            posOffset.Scale(transform.lossyScale);

            ghostTransform.rotation = transform.rotation * rootOffset.rotation;
            ghostTransform.position = transform.position + transform.rotation * posOffset;
            ghostTransform.localScale = ghostTransform.transform.parent == null
                ? Vector3.one * _scale
                : (Vector3.one * _scale).Divide(transform.lossyScale);
        }

        public override void OnInspectorGUI()
        {
            var handednessProperty = serializedObject.FindProperty("handedness");
            var handedness = (XrHandedness)handednessProperty.intValue;

            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                var newHandedness = (XrHandedness)handednessProperty.intValue;
                if (newHandedness != handedness)
                {
                    // Flip all children poses
                    foreach (var pose in _provider.Poses)
                    {
                        if (pose.Data.Handedness == newHandedness)
                        {
                            continue;
                        }

                        var data = pose.Data;
                        data.Flip();
                        pose.Data = data;
                    }

                    _snapPoseEditor.RefreshGhost(newHandedness);
                }
            }

            if (_provider.Poses.Count > 0)
            {
                EditorGUI.BeginChangeCheck();

                var minPose = _provider.Poses[0];
                var maxPose = _provider.Poses[_provider.Poses.Count - 1];

                var minScale = minPose != null ? minPose.Data.Scale : 0f;
                var maxScale = maxPose != null ? maxPose.Data.Scale : 1f;

                if (Mathf.Abs(minScale - maxScale) < Mathf.Epsilon)
                {
                    GUI.enabled = false;
                }

                _scale = EditorGUILayout.Slider("Scale", _scale, minScale, maxScale);

                GUI.enabled = true;

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }
        }

        #endregion
    }
}