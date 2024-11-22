// /******************************************************************************
//  * File: XRHandGrabTransformer.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  *
//  ******************************************************************************/

using QCHT.Interactions.Core;
using QCHT.Interactions.Hands;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

#if XRIT_3_0_0_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QCHT.Interactions.Proximal
{
    public class XRHandGrabTransformer : XRSingleGrabFreeTransformer
    {
        [SerializeField, Tooltip("Constraint direction axis")]
        private XRHandGrabTransformerConstraint constraint;

        /// <summary>
        /// Constraint direction axis
        /// </summary>
        public XRHandGrabTransformerConstraint Constraint
        {
            get => constraint;
            set => constraint = value;
        }

        [SerializeField, Tooltip("Should it re-parent interactable just after grabbing it?")]
        private bool restoreParentOnGrab;

        /// <summary>
        /// Should it re-parent interactable just after grabbing it?
        /// Due to XRIT a interactable is un-parented when grabbed and calculation are down in world space.
        /// For some reasons it would be interesting to let it under is original parent.  
        /// </summary>
        public bool RestoreParentOnGrab
        {
            get => restoreParentOnGrab;
            set => restoreParentOnGrab = value;
        }

        private XRHandInteractableSnapPoseManager _snapPoseManager;
        private XRHandInteractableSnapPoseProvider _poseProvider;
        private Transform _origin;
        private Transform _cameraOffset;
        private Transform _parentTransform;

        private bool ConstraintEnabled => constraint != null && constraint.enabled;
        private bool PoseProviderEnabled => _poseProvider != null && _poseProvider.enabled;

        private void OnBeforeTransformParentChanged()
        {
            _parentTransform = transform.parent;
        }

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);

            if (restoreParentOnGrab)
            {
                transform.SetParent(_parentTransform);
            }

            FindXROrigin();
            FindCameraOffset();
            FindOrCreateSnapPoseManager();

            // Find suitable pose provider
            var interactor = grabInteractable.interactorsSelecting[0] as XRDirectInteractor;
            if (interactor == null)
            {
                return;
            }

            var controllerHandedness = interactor.xrController.GetComponentInParent<IHandedness>();
            if (controllerHandedness == null)
            {
                return;
            }

            var poseProviders = GetComponentsInChildren<XRHandInteractableSnapPoseProvider>();
            foreach (var poseProvider in poseProviders)
            {
                if (poseProvider.Handedness == controllerHandedness.Handedness)
                {
                    _poseProvider = poseProvider;
                    break;
                }
            }

            grabInteractable.selectExited.AddListener(OnUnGrab);
        }

        private void OnUnGrab(SelectExitEventArgs args)
        {
            ReleasePose();
            _poseProvider = null;

            args.interactableObject.selectExited.RemoveListener(OnUnGrab);
        }

        public override void Process(XRGrabInteractable grabInteractable,
            XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            // Fallback to classic XRSingleGrabFreeTransformer if no constraint is defined
            if (!ConstraintEnabled)
            {
                base.Process(grabInteractable, updatePhase, ref targetPose, ref localScale);
            }
            else
            {
                switch (updatePhase)
                {
                    case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                    case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                        UpdateTarget(grabInteractable, out targetPose);
                        break;
                }
            }

            UpdateHandPose(ref targetPose);
        }

        private void UpdateTarget(XRGrabInteractable grabInteractable, out Pose targetPose)
        {
            var interactor = grabInteractable.interactorsSelecting[0];
            var attachPose = interactor.GetAttachTransform(grabInteractable).GetWorldPose();

            if (ConstraintEnabled)
            {
                constraint.CalculateTargetPose(attachPose, out targetPose);
            }
            else
            {
                targetPose = attachPose;
            }
        }

        private void UpdateHandPose(ref Pose targetPose)
        {
            if (!PoseProviderEnabled)
            {
                return;
            }

            var handPose = new HandData();
            var rootPose = new Pose();
            var localRootOffset = new Pose();

            if (!_poseProvider.TryGetInterpolatedHandPose(ref handPose, ref rootPose, ref localRootOffset))
            {
                return;
            }

            // When handled freely by pose provider the target position is altered.
            // Local offset and rotation of predefined pose is applied to hand root pose.
            // Target interactable pose is combination of hand root pose + inverted local offset of predefined pose 
            if (!ConstraintEnabled)
            {
                var attachPose = rootPose;

                if (_origin != null)
                {
                    attachPose = _origin.TransformPose(attachPose);
                }

                if (_cameraOffset != null)
                {
                    var oTc = _origin.InverseTransformPose(_cameraOffset.GetWorldPose());
                    attachPose = oTc.ApplyOffsetTo(attachPose);
                }

                attachPose.rotation *= Quaternion.AngleAxis(90f, Vector3.right);

                var posOffset = localRootOffset.position;
                posOffset.Scale(transform.lossyScale);

                targetPose.rotation = attachPose.rotation * Quaternion.Inverse(localRootOffset.rotation);
                targetPose.position = attachPose.position +
                                      attachPose.rotation * (Quaternion.Inverse(localRootOffset.rotation) * -posOffset);

                // In this case root pose should not be altered
                ApplyPose(handPose, null);
            }
            else
            {
                var posOffset = localRootOffset.position;
                posOffset.Scale(transform.lossyScale);

                rootPose.rotation = Quaternion.AngleAxis(90f, Vector3.left) * targetPose.rotation *
                                    localRootOffset.rotation;
                rootPose.position = targetPose.position + targetPose.rotation * posOffset;

                ApplyPose(handPose, rootPose);
            }
        }

        private void FindOrCreateSnapPoseManager()
        {
            if (_snapPoseManager != null)
            {
                return;
            }

            _snapPoseManager = FindObjectOfType<XRHandInteractableSnapPoseManager>();

            if (_snapPoseManager == null)
            {
                _snapPoseManager = new GameObject(nameof(XRHandInteractableSnapPoseManager),
                    typeof(XRHandInteractableSnapPoseManager)).GetComponent<XRHandInteractableSnapPoseManager>();
            }
        }

        private void FindCameraOffset()
        {
            if (_cameraOffset != null)
            {
                return;
            }

            var cameraFloorOffsetObject = XROriginUtility.GetCameraFloorOffsetObject();
            if (cameraFloorOffsetObject != null)
            {
                _cameraOffset = cameraFloorOffsetObject.transform;
            }
        }

        private void FindXROrigin()
        {
            if (_origin != null)
            {
                return;
            }

            _origin = XROriginUtility.GetOriginTransform();
        }

        private void ApplyPose(HandData? pose, Pose? rootPose)
        {
            if (_snapPoseManager == null || _poseProvider == null)
            {
                return;
            }

            _snapPoseManager.SetHandPose(_poseProvider.Handedness, pose, _poseProvider.Mask, rootPose);
        }

        private void ReleasePose()
        {
            if (_snapPoseManager == null || _poseProvider == null)
                return;

            _snapPoseManager.SetHandPose(_poseProvider.Handedness, null, null, null);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(XRHandGrabTransformer))]
    public class XRHandGrabTransformerEditor : Editor
    {
        private XRHandGrabTransformer _transformer;
        private XRHandGrabTransformerConstraintEditor _constraintEditor;

        private void Awake()
        {
            _transformer = target as XRHandGrabTransformer;

            if (_transformer != null && _transformer.Constraint != null)
            {
                _constraintEditor = CreateEditor(_transformer.Constraint) as XRHandGrabTransformerConstraintEditor;
            }
        }

        public void OnSceneGUI()
        {
            if (_constraintEditor != null)
            {
                _constraintEditor.OnSceneGUI();
            }
        }
    }
#endif
}