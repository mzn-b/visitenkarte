// /******************************************************************************
//  * File: XRHandGrabTransformerConstraintAxis.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  *
//  ******************************************************************************/

using System;
using Unity.XR.CoreUtils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QCHT.Interactions.Proximal
{
    public class XRHandGrabTransformerConstraintAxis : XRHandGrabTransformerConstraint
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        [SerializeField, Tooltip("Restricted Axis")]
        private Axis axis;

        /// <summary>
        /// Constraint Axis
        /// </summary>
        public Axis ConstraintAxis
        {
            get => axis;
            set => axis = value;
        }

        [SerializeField, Tooltip("Relative Pivot which generally is the direct parent")]
        private Transform relativePivot;

        /// <summary>
        /// Relative pivot for constraint.
        /// In most of cases, it is the direct parent of interactable.
        /// If the relative pivot is null then axis will be locked relatively to world space axis.
        /// </summary>
        public Transform RelativePivot
        {
            get => relativePivot;
            set => relativePivot = value;
        }

        [SerializeField, Tooltip("Minimum value on constraint axis.")]
        private float minValue = -.1f;

        /// <summary>
        /// Minimum value on constraint axis.
        /// </summary>
        public float MinAxisValue
        {
            get => minValue;
            set => minValue = value;
        }

        [SerializeField, Tooltip("Maximum value on constraint axis")]
        private float maxValue = .1f;

        /// <summary>
        /// Maximum value on constraint axis.
        /// </summary>
        public float MaxAxisValue
        {
            get => maxValue;
            set => maxValue = value;
        }

        [SerializeField, Range(0f, 1f), Tooltip("Current value along constraint axis (normalized).")]
        private float axisValue = .5f;

        /// <summary>
        /// Current value along constraint axis (normalized).
        /// Min value = 0f
        /// Max value = 1f
        /// </summary>
        public float AxisValue
        {
            get => axisValue;
            set => axisValue = value;
        }

        public override void CalculateTargetPose(Pose attachPose, out Pose targetPose)
        {
            var t = transform;
            var pivot = relativePivot != null ? relativePivot : transform.parent;

            var pose = pivot.InverseTransformPose(attachPose);
            var pos = pose.position;
            var pivotAxis = Quaternion.Inverse(pivot.rotation) * GetScaledPivotAxis();
            pos = GetProjectedOnPivotAxis(pos, pivotAxis);

            if (pivot != transform)
            {
                targetPose.rotation = t.rotation;
                targetPose.position = pivot.position + pivot.rotation * pos;
            }
            else
            {
                targetPose.rotation = t.rotation;
                targetPose.position = t.rotation * pos;
            }
        }

        internal Vector3 GetScaledPivotAxis()
        {
            Vector3 pivotAxis;

            if (RelativePivot != null)
            {
                pivotAxis = axis switch
                {
                    Axis.X => RelativePivot.right,
                    Axis.Y => RelativePivot.up,
                    Axis.Z => RelativePivot.forward,
                    _ => Vector3.zero
                };

                pivotAxis.Scale(RelativePivot.lossyScale);
            }
            else
            {
                pivotAxis = axis switch
                {
                    Axis.X => Vector3.right,
                    Axis.Y => Vector3.up,
                    Axis.Z => Vector3.forward,
                    _ => Vector3.zero
                };
            }

            return pivotAxis;
        }

        private Vector3 GetProjectedOnPivotAxis(Vector3 proj, Vector3 axis)
        {
            proj = Vector3.Project(proj, axis);
            var value = Vector3.Dot(axis, proj);
            if (value > 0 && proj.sqrMagnitude >= maxValue * maxValue * axis.sqrMagnitude)
            {
                proj = axis * maxValue;
                value = maxValue;
            }
            else if (value < 0 && proj.sqrMagnitude >= minValue * minValue * axis.sqrMagnitude)
            {
                proj = axis * minValue;
                value = minValue;
            }

            axisValue = (value + Math.Abs(minValue)) / (Math.Abs(maxValue) + Math.Abs(minValue));

#if UNITY_EDITOR
            var pivot = relativePivot;
            var color = value > 0 ? Color.red : Color.yellow;
            Debug.DrawRay(pivot.position, proj, color);
#endif
            return proj;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(XRHandGrabTransformerConstraintAxis))]
    public class XRHandGrabTransformerConstraintAxisEditor : XRHandGrabTransformerConstraintEditor
    {
        private XRHandGrabTransformerConstraintAxis _constraintAxis;

        private void OnEnable()
        {
            _constraintAxis = target as XRHandGrabTransformerConstraintAxis;
            if (_constraintAxis == null)
            {
                return;
            }

            if (_constraintAxis.RelativePivot == null)
            {
                _constraintAxis.RelativePivot = _constraintAxis.transform.parent;
            }
        }

        public override void OnSceneGUI()
        {
            UpdatePosition();
            DrawConstraintAxis();
        }

        private void UpdatePosition()
        {
            var pivotAxis = _constraintAxis.GetScaledPivotAxis();
            var remappedValue = Mathf.Lerp(_constraintAxis.MinAxisValue, _constraintAxis.MaxAxisValue, _constraintAxis.AxisValue);
            var pivotPos = _constraintAxis.RelativePivot != null ? _constraintAxis.RelativePivot.position : Vector3.zero;
            _constraintAxis.transform.position = pivotPos + pivotAxis * remappedValue;
        }

        private void DrawConstraintAxis()
        {
            var axis = _constraintAxis.GetScaledPivotAxis();
            var pivotPos = _constraintAxis.RelativePivot != null ? _constraintAxis.RelativePivot.position : Vector3.zero;
            var p1 = pivotPos + axis * _constraintAxis.MinAxisValue;
            var p2 = pivotPos + axis * _constraintAxis.MaxAxisValue;

            Handles.color = Color.yellow;
            Handles.DrawLine(p1, p2);
        }
    }
#endif
}