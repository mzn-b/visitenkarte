// /******************************************************************************
//  * File: XRHandInteractableSnapPoseProvider.cs
//  * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
//  *
//  * Confidential and Proprietary - Qualcomm Technologies, Inc.
//  *
//  ******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using QCHT.Interactions.Core;
using QCHT.Interactions.Hands;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

#if XRIT_3_0_0_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#endif

namespace QCHT.Interactions.Proximal
{
    [DisallowMultipleComponent]
    public class XRHandInteractableSnapPoseProvider : MonoBehaviour
    {
        [SerializeField, Tooltip("Handedness of poses set.\n" +
                                 "Changing it in editor will automatically flip all poses in poses list.")]
        private XrHandedness handedness;

        /// <summary>
        /// Handedness of poses.
        /// </summary>
        public XrHandedness Handedness
        {
            get => handedness;
        }

        [SerializeField, Tooltip("Finger states when the pose is applied to interactable.\n " +
                                 "Required, the finger will be locked on the object.\n" +
                                 "Free, the finger will continue to be tracked by hand tracking subsystem.")]
        private HandMask mask;

        /// <summary>
        /// Hand mask giving which fingers are "free" and which will be constrained when applying pose.
        /// </summary>
        public HandMask Mask
        {
            get => mask;
        }

        [SerializeField, Tooltip("Hand poses using different hand scales.")]
        private List<XRHandInteractableSnapPose> poses;

        /// <summary>
        /// List of hand poses that will be interpolated by hand scale.
        /// </summary>
        public List<XRHandInteractableSnapPose> Poses
        {
            get => poses;
            set => poses = value;
        }

        private XRHandTrackingSubsystem _subsystem;

        protected void OnEnable()
        {
            if (!TryFindInteractable())
            {
                enabled = false;
                return;
            }

            _interactable.addDefaultGrabTransformers = false;
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);

            SanitizePoseList();
        }

        protected void OnDisable()
        {
            if (_interactable)
            {
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
                _interactable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        private void FindHandTrackingSubsystem()
        {
            if (_subsystem != null)
            {
                return;
            }

            _subsystem = XRHandTrackingSubsystem.GetSubsystemInManager();
        }

        public void FindPoses()
        {
            poses = GetComponentsInChildren<XRHandInteractableSnapPose>().ToList();
        }

        public void SanitizePoseList()
        {
            var validPoses = poses.Where(pose => pose != null).ToList();
            validPoses.Sort((s1, s2) => s1.Data.Scale.CompareTo(s2.Data.Scale));
            poses = validPoses;
        }

        #region XR Grab Interactable callbacks

        internal bool TryGetInterpolatedHandPose(ref HandData handData, ref Pose rootPose, ref Pose localRootOffset)
        {
            if (_subsystem == null)
            {
                FindHandTrackingSubsystem();
            }

            if (_subsystem == null || !_subsystem.running)
            {
                return false;
            }

            var hand = handedness == XrHandedness.XR_HAND_LEFT ? _subsystem.LeftHand : _subsystem.RightHand;
            rootPose = hand.Root;

            if (TryGetInterpolatedHandPoseFromScale(ref handData, ref localRootOffset, hand.Scale))
            {
                var t = transform;
                localRootOffset.rotation = t.localRotation * localRootOffset.rotation;
                localRootOffset.position = t.localPosition + t.localRotation * localRootOffset.position;
                return true;
            }

            return false;
        }

        internal bool TryGetInterpolatedHandPoseFromScale(ref HandData handData, ref Pose localRootOffset, float scale)
        {
            if (poses == null || poses.Count == 0)
            {
                return false;
            }

            localRootOffset = Pose.identity;

            Vector3 lPos;
            Quaternion lRot;

            if (poses.Count == 1)
            {
                handData = poses[0].Data;
                lRot = poses[0].transform.localRotation;
                lPos = poses[0].transform.localPosition;
            }
            else
            {
                var i1 = 0;
                for (var i = 0; i <= poses.Count - 2; i++)
                {
                    if (scale > poses[i].Data.Scale)
                    {
                        i1 = i;
                    }
                }

                var i2 = i1 + 1;
                var t = (scale - poses[i1].Data.Scale) / (poses[i2].Data.Scale - poses[i1].Data.Scale);
                
                if (t > Mathf.Epsilon)
                {
                    handData = HandData.Lerp(poses[i1].Data, poses[i2].Data, t);
                    lPos = Vector3.Lerp(poses[i1].transform.localPosition, poses[i2].transform.localPosition, t);
                    lRot = Quaternion.Lerp(poses[i1].transform.localRotation, poses[i2].transform.localRotation, t);
                }
                else
                {
                    handData = poses[i1].Data;
                    lPos = poses[i1].transform.localPosition;
                    lRot = poses[i1].transform.localRotation;
                }
            }

            localRootOffset.rotation = lRot;
            localRootOffset.position = lPos;

            return true;
        }

        #endregion

#if UNITY_EDITOR
        protected void Reset()
        {
            FindPoses();
        }

        protected void OnValidate()
        {
            FindPoses();
        }
#endif

        #region Helper - Add XRHandGrabTransformer when interactable transformer is not defined

        private XRGrabInteractable _interactable;
        private XRHandGrabTransformer _handGrabTransformer;

        private bool TryFindInteractable()
        {
            _interactable = _interactable ? _interactable : GetComponentInParent<XRGrabInteractable>(true);

            if (_interactable == null)
            {
                Debug.LogWarning("[XRHandInteractableSnapPoseProvider:TryFindInteractable] " +
                                 "Unable to find interactable attached to snap pose provider");
                return false;
            }

            if (_interactable.useDynamicAttach)
            {
                Debug.LogWarning("[XRHandInteractableSnapPoseProvider:TryFindInteractable] " +
                                 "Using dynamic attach on interactable with snap pose provider may not work as expected.");
            }

            return _interactable;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            var interactor = args.interactorObject as XRDirectInteractor;
            if (interactor == null)
            {
                return;
            }

            var handed = interactor.xrController.GetComponentInParent<IHandedness>();
            if (handed == null || handed.Handedness != handedness)
            {
                return;
            }

            if (_interactable.singleGrabTransformersCount == 0 && _interactable.multipleGrabTransformersCount == 0)
            {
                if (_handGrabTransformer == null)
                {
                    _handGrabTransformer = gameObject.AddComponent<XRHandGrabTransformer>();
                }

                _interactable.AddSingleGrabTransformer(_handGrabTransformer);
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            var interactor = args.interactorObject as XRDirectInteractor;
            if (interactor == null)
            {
                return;
            }

            if (_handGrabTransformer != null)
            {
                _interactable.RemoveSingleGrabTransformer(_handGrabTransformer);
            }
        }

        #endregion
    }
}