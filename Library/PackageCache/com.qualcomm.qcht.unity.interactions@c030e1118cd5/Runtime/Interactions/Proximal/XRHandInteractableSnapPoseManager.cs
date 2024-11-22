// /******************************************************************************
//  * File: XRHandInteractableSnapPoseManager.cs
//  * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using System.Collections.Generic;
using QCHT.Interactions.Core;
using QCHT.Interactions.Hands;
using UnityEngine;

namespace QCHT.Interactions.Proximal
{
    public class XRHandInteractableSnapPoseManager : MonoBehaviour
    {
        protected readonly List<ISnapPoseReceiver> _snapPoseReceivers =
            new List<ISnapPoseReceiver>();
        
        public void RegisterHandPoseReceiver(ISnapPoseReceiver receiver)
        {
            if (_snapPoseReceivers.Contains(receiver))
                return;
            
            _snapPoseReceivers.Add(receiver);
        }

        public void UnRegisterHandPoseReceiver(ISnapPoseReceiver receiver)
        {
            _snapPoseReceivers.Remove(receiver);
        }
        
        public void SetHandPose(XrHandedness handedness, HandData? snapPose, HandMask? mask, Pose? rootPose)
        {
            foreach (var receiver in _snapPoseReceivers)
            {
                if (receiver == null)
                    continue;
                
                if (receiver.Handedness == handedness) 
                    receiver.SetPose(snapPose, mask, rootPose);
            }
        }

        public void SetHandRoot(XrHandedness handedness, Pose? rootPose)
        {
            foreach (var receiver in _snapPoseReceivers)
            {
                if (receiver == null)
                    continue;

                if (receiver.Handedness == handedness)
                {
                    if (rootPose.HasValue)
                        receiver.SetRootPose(rootPose.Value);
                    else
                        receiver.SetRootPose(null);
                }
            }
        }
    }
}