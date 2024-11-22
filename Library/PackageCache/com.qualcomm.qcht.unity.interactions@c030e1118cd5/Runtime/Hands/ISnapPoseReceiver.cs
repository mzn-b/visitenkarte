// /******************************************************************************
//  * File: ISnapPoseReceiver.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  *
//  ******************************************************************************/

using UnityEngine;
using QCHT.Interactions.Core;

namespace QCHT.Interactions.Hands
{
    public interface ISnapPoseReceiver
    {
        public XrHandedness Handedness { get; }

        public void SetPose(HandData? pose, HandMask? mask, Pose? rootPose);

        public void SetRootPose(Pose? rootPose);
    }
}