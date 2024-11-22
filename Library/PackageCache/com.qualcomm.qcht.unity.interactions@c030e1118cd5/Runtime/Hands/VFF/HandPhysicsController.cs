// /******************************************************************************
//  * File: HandPhysicsController.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  *
//  ******************************************************************************/

using QCHT.Interactions.Core;
using UnityEngine;
using QCHT.Interactions.Proximal;

namespace QCHT.Interactions.Hands.VFF
{
    public class HandPhysicsController : MonoBehaviour, ISnapPoseReceiver, IHandPhysible
    {
        [field: SerializeField] public XrHandedness Handedness { get; private set; }

        [field: SerializeField] public HandPhysicsPart HandPhysicsPart { get; private set; }

        private XRHandInteractableSnapPoseManager _snapPoseManager;
        
        #region IHandPhysible

        private bool _isPhysible;

        public bool IsPhysible
        {
            get => _isPhysible;
            set
            {
                _isPhysible = value;

                UpdatePhysible();
            }
        }

        #endregion

        private void Awake()
        {
            FindCreateSnapPoseManager();

            // A hand pose can be set or reset even if driver is disabled
            _snapPoseManager.RegisterHandPoseReceiver(this);
        }

        private void OnDestroy()
        {
            _snapPoseManager.UnRegisterHandPoseReceiver(this);
        }

        #region ISnapPoseReceiver

        private bool _isGrabbing;

        public void SetPose(HandData? pose, HandMask? mask, Pose? rootPose)
        {
            _isGrabbing = pose != null;

            UpdatePhysible();
        }

        public void SetRootPose(Pose? rootPose)
        {
            _isGrabbing = rootPose != null;

            UpdatePhysible();
        }

        #endregion

        private void UpdatePhysible()
        {
            if (HandPhysicsPart == null)
            {
                return;
            }

            HandPhysicsPart.IsPhysible = _isPhysible && !_isGrabbing;
        }

        private void FindCreateSnapPoseManager()
        {
            if (_snapPoseManager != null)
                return;

            _snapPoseManager = FindObjectOfType<XRHandInteractableSnapPoseManager>();

            if (_snapPoseManager == null)
            {
                _snapPoseManager = new GameObject(nameof(XRHandInteractableSnapPoseManager),
                    typeof(XRHandInteractableSnapPoseManager)).GetComponent<XRHandInteractableSnapPoseManager>();
            }
        }
    }
}