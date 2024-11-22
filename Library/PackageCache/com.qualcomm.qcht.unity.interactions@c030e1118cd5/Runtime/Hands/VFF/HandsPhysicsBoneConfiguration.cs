// /******************************************************************************
//  * File: HandsPhysicsBoneConfiguration.cs
//  * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using System;
using UnityEngine;

namespace QCHT.Interactions.Hands.VFF
{
    [Serializable]
    public class HandPhysicsJointDrive
    {
        public float Spring = 1f;
        public float Damper = 0.01f;
        public float MaxForce = float.MaxValue;
        
        public JointDrive ToJointDrive => new JointDrive 
        {
                positionSpring = Spring,
                positionDamper = Damper,
                maximumForce = MaxForce
        };
    }
    
    [Serializable]
    [CreateAssetMenu(menuName = "QCHT/Interactions/VFF/HandsPhysicsBoneConfiguration")]
    public sealed class HandsPhysicsBoneConfiguration : ScriptableObject
    {
        [Header("Rigidbody")]
        public bool UseGravity;
        public float RigidbodyMass = 0.1f;
        public float RigidbodyDrag;
        public float RigidbodyAngularDrag;
        public CollisionDetectionMode CollisionDetectionMode;

        [Header("Joint")]
        public float JointMassScale = 1.0f;
        public float JointConnectedMassScale = 1.0f;
        public ConfigurableJointMotion LinearMotion;
        public HandPhysicsJointDrive MotionDrive;
        public ConfigurableJointMotion AngularMotion;
        public HandPhysicsJointDrive AngularDrive;

        [Space] 
        public JointProjectionMode ProjectionMode= JointProjectionMode.None;
        public RotationDriveMode RotationDriveMode = RotationDriveMode.Slerp;
        public bool AutoConfigureConnectedAnchor;
        public bool EnableCollision;
        public bool EnablePreprocessing;
    }
}