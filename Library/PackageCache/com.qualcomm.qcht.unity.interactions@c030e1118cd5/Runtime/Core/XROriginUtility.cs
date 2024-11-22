// /******************************************************************************
//  * File: XROriginUtility.cs
//  * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using UnityEngine;
using Unity.XR.CoreUtils;

namespace QCHT.Interactions.Core
{
    public static class XROriginUtility
    {
        private static XROrigin s_xrOrigin;

        public static XROrigin FindXROrigin()
        {
            return s_xrOrigin = s_xrOrigin != null ? s_xrOrigin : Object.FindObjectOfType<XROrigin>();
        }

        public static Camera GetOriginCamera()
        {
            if (s_xrOrigin != null)
            {
                return s_xrOrigin.Camera;
            }
            
            var camera = FindXROrigin()?.Camera;
            if (camera == null)
            {
                camera = Camera.main;
            }

            return camera;
        }

        public static Transform GetOriginTransform()
        {
            if (s_xrOrigin != null)
            {
                return s_xrOrigin.transform;
            }
            
            return FindXROrigin()?.transform;
        }

        public static Transform GetTrackablesParent()
        {
            if (s_xrOrigin != null)
            {
                return s_xrOrigin.TrackablesParent;
            }
            
            return FindXROrigin()?.TrackablesParent;
        }

        public static GameObject GetCameraFloorOffsetObject()
        {
            if (s_xrOrigin != null)
            {
                return s_xrOrigin.CameraFloorOffsetObject;
            }
            
            return FindXROrigin()?.CameraFloorOffsetObject;
        }


        /// <summary>
        /// Converts XR Origin based position to World scene position
        /// </summary>
        /// <param name="point"> Position to transform </param>
        /// <param name="applyCameraOffset"> Should apply camera Y-offset? </param>
        public static void TransformPoint(ref Vector3 point, bool applyCameraOffset = false)
        {
            var origin = GetOriginTransform();
            if (origin == null)
            {
                return;
            }
            
            point = origin.TransformPoint(point);
                
            if (applyCameraOffset)
            {
                var cameraOffset = GetCameraFloorOffsetObject();
                if (cameraOffset != null)
                {
                    point.y += cameraOffset.transform.position.y - origin.position.y;
                }
            }
        }

        /// <summary>
        /// Converts XR Origin based pose to World scene pose
        /// </summary>
        /// <param name="pose"> Pose to transform </param>
        /// <param name="applyCameraOffset"> Should apply camera Y-offset? </param>
        public static void TransformPose(ref Pose pose, bool applyCameraOffset = false)
        {
            var origin = GetOriginTransform();
            if (origin == null)
            {
                return;
            }
            
            pose = origin.TransformPose(pose);
            
            if (applyCameraOffset)
            {
                var cameraOffset = GetCameraFloorOffsetObject();
                if (cameraOffset != null)
                {
                    pose.position.y += cameraOffset.transform.position.y - origin.position.y;
                }
            }
        }
    }
}