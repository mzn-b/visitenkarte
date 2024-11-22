// /******************************************************************************
//  * File: HandPhysicsPart.cs
//  * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using UnityEngine;
using QCHT.Interactions.Core;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace QCHT.Interactions.Hands.VFF
{
    public class HandPhysicsPart : HandJointUpdater, IHandPhysible
    {
        [SerializeField] private HandsPhysicsBoneConfiguration configuration;

        public HandsPhysicsBoneConfiguration Configuration
        {
            get => configuration;
            set => configuration = value;
        }

        [SerializeField] private HandPhysicsPart connectedPhysicsPart;

        public HandPhysicsPart ConnectedPhysicsPart
        {
            get => connectedPhysicsPart;
            set => connectedPhysicsPart = value;
        }

        [SerializeField] private Collider collider;

        public Collider Collider
        {
            get => collider;
            set => collider = value;
        }

        private Rigidbody _rigidBody;
        private ConfigurableJoint _joint;
        private Transform _centerOfMass;
        private Transform _anchor;
        private Transform _connectedAnchor;

        #region IHandPhysible

        private bool _isPhysible = true;

        public bool IsPhysible
        {
            get => _isPhysible;
            set
            {
                _isPhysible = value;
                
                if (_rigidBody != null)
                {
                    _rigidBody.isKinematic = !value;
                }
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _joint = gameObject.AddComponent<ConfigurableJoint>();
        }

        private void Start()
        {
            if (ConnectedPhysicsPart != null)
            {
                Physics.IgnoreCollision(ConnectedPhysicsPart.Collider, Collider);

                if (ConnectedPhysicsPart.TryGetComponent<Rigidbody>(out var connectedRigidBody))
                {
                    _joint.connectedBody = connectedRigidBody;
                }
            }

            if (TryGetComponent(out _rigidBody))
            {
                _rigidBody.isKinematic = !_isPhysible;
                
                InstantiateCenterOfMass();
            }

            InstantiateAnchor();
            InstantiateConnectedAnchor();
        }

        private void FixedUpdate() => UpdatePart();

        #endregion

        #region HandJointUpdater

        public override void UpdateJoint(XrSpace space, BoneData data)
        {
            if (!IsPhysible || _connectedAnchor == null)
            {
                base.UpdateJoint(space, data);
                return;
            }
            
            if (space == XrSpace.XR_HAND_WORLD)
            {
                if (data.UpdatePosition)
                {
                    _connectedAnchor.position = data.Position;
                }

                if (data.UpdateRotation)
                {
                    _connectedAnchor.rotation = data.Rotation;
                }
            }
            else
            {
                if (data.UpdatePosition)
                {
                    _connectedAnchor.localPosition = data.Position;
                }

                if (data.UpdateRotation)
                {
                    _connectedAnchor.localRotation = data.Rotation;
                }
            }

            if (_joint != null)
            {
                _joint.configuredInWorldSpace = false;
                _joint.axis = data.Rotation * Vector3.right;
                _joint.secondaryAxis = data.Rotation * Vector3.up;
            }
        }

        #endregion

        private void UpdatePart()
        {
            UpdateJointConfiguration();

            if (!IsPhysible)
            {
                return;
            }

            if (_rigidBody != null)
            {
                UpdateRigidBodyConfiguration();

                _rigidBody.centerOfMass = _rigidBody.transform.InverseTransformPoint(_centerOfMass.position);
            }

            // Target Rotation
            if (_joint.configuredInWorldSpace)
            {
                _joint.SetTargetRotation(_connectedAnchor.rotation, _anchor.rotation);
            }
            else
            {
                _joint.SetTargetRotationLocal(_connectedAnchor.rotation, _anchor.rotation);
            }

            if (_joint.connectedBody != null)
            {
                _joint.connectedAnchor = _connectedAnchor.localPosition;
            }
            else
            {
                _joint.connectedAnchor = _connectedAnchor.position;
            }
        }

        private void UpdateJointConfiguration()
        {
            // RigidBody
            _joint.massScale = Configuration.JointMassScale;
            _joint.connectedMassScale = Configuration.JointConnectedMassScale;

            // Angular
            _joint.angularXMotion = _joint.angularYMotion = _joint.angularZMotion = Configuration.AngularMotion;
            _joint.angularXDrive = _joint.angularYZDrive = Configuration.AngularDrive.ToJointDrive;
            _joint.slerpDrive = Configuration.AngularDrive.ToJointDrive;

            //Motion
            _joint.xMotion = _joint.yMotion = _joint.zMotion = Configuration.LinearMotion;
            _joint.xDrive = _joint.yDrive = _joint.zDrive = Configuration.MotionDrive.ToJointDrive;

            // Control
            _joint.projectionMode = Configuration.ProjectionMode;
            _joint.rotationDriveMode = Configuration.RotationDriveMode;
            _joint.autoConfigureConnectedAnchor = Configuration.AutoConfigureConnectedAnchor;
            _joint.enableCollision = Configuration.EnableCollision;
            _joint.enablePreprocessing = Configuration.EnablePreprocessing;
        }

        private void UpdateRigidBodyConfiguration()
        {
            _rigidBody.mass = Configuration.RigidbodyMass;
            _rigidBody.drag = Configuration.RigidbodyDrag;
            _rigidBody.angularDrag = Configuration.RigidbodyAngularDrag;
            _rigidBody.useGravity = Configuration.UseGravity;
            _rigidBody.collisionDetectionMode = Configuration.CollisionDetectionMode;
        }

        private void InstantiateCenterOfMass()
        {
            var centerOfMass = new GameObject(name + ".CenterOfMass").transform;
            centerOfMass.SetParent(transform);
            centerOfMass.localPosition = _rigidBody.centerOfMass;
            _centerOfMass = centerOfMass;
        }

        private void InstantiateAnchor()
        {
            var anchor = new GameObject(name + ".Anchor").transform;
            var jointTransform = _joint.transform;
            anchor.position = jointTransform.position;
            anchor.rotation = jointTransform.rotation;
            anchor.SetParent(jointTransform);
            _anchor = anchor;
        }

        private void InstantiateConnectedAnchor()
        {
            var connectedAnchor = new GameObject(name + ".ConnectedAnchor").transform;
            var jointTransform = _joint.transform;
            connectedAnchor.SetParent(ConnectedPhysicsPart
                ? ConnectedPhysicsPart.transform
                : jointTransform.parent);
            connectedAnchor.position = jointTransform.position;
            connectedAnchor.rotation = jointTransform.rotation;
            _connectedAnchor = connectedAnchor;
        }
    }

    public static class ConfigurableJointExtensions
    {
        public static void SetTargetRotationLocal(this ConfigurableJoint joint, Quaternion targetLocalRotation,
            Quaternion startLocalRotation)
        {
            if (joint.configuredInWorldSpace)
                return;

            SetTargetRotationInternal(joint, targetLocalRotation, startLocalRotation, Space.Self);
        }

        public static void SetTargetRotation(this ConfigurableJoint joint, Quaternion targetWorldRotation,
            Quaternion startWorldRotation)
        {
            if (!joint.configuredInWorldSpace)
                return;

            SetTargetRotationInternal(joint, targetWorldRotation, startWorldRotation, Space.World);
        }

        private static void SetTargetRotationInternal(ConfigurableJoint joint, Quaternion targetRotation,
            Quaternion startRotation, Space space)
        {
            var axis = joint.axis;
            var forward = Vector3.Cross(axis, joint.secondaryAxis).normalized;
            var up = Vector3.Cross(forward, axis).normalized;
            var worldToJointSpace = Quaternion.LookRotation(forward, up);
            var resultRotation = Quaternion.Inverse(worldToJointSpace);

            if (space == Space.World)
                resultRotation *= startRotation * Quaternion.Inverse(targetRotation);
            else
                resultRotation *= Quaternion.Inverse(targetRotation) * startRotation;

            resultRotation *= worldToJointSpace;
            joint.targetRotation = resultRotation;
        }
    }
}