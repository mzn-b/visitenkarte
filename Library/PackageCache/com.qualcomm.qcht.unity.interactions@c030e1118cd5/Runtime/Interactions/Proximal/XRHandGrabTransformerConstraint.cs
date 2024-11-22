// /******************************************************************************
//  * File: XRHandGrabTransformerConstraint.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  *
//  ******************************************************************************/

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QCHT.Interactions.Proximal
{
    public interface IXRHandGrabTransformerConstraint
    {
        public void CalculateTargetPose(Pose attachPose, out Pose targetPose);
    }

    [RequireComponent(typeof(XRHandGrabTransformer)), DisallowMultipleComponent]
    public abstract class XRHandGrabTransformerConstraint : MonoBehaviour, IXRHandGrabTransformerConstraint
    {
        public virtual void OnEnable()
        {
            // Just to enable script tick in editor 
        }

        public void OnValidate() => GetComponent<XRHandGrabTransformer>().Constraint = this;

        public abstract void CalculateTargetPose(Pose attachPose, out Pose targetPose);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(XRHandGrabTransformerConstraint))]
    public abstract class XRHandGrabTransformerConstraintEditor : Editor
    {
        public virtual void OnSceneGUI()
        {
            // Just to expose OnSceneGUI public
        }
    }
#endif
}