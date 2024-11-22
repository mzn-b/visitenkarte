// /******************************************************************************
// * File: XRSwitchRaysToTeleportManager.cs
// * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
// *
// * Confidential and Proprietary - Qualcomm Technologies, Inc.
// *
// ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using QCHT.Interactions.Core;
using QCHT.Interactions.Extensions;
using QCHT.Interactions.Hands;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace QCHT.Interactions.Distal
{
    /// <summary>
    /// Allows switching from normal ray mode to teleport ray mode
    /// This component is used on Hand Controllers and Motions controllers
    /// </summary>
    [DefaultExecutionOrder(k_UpdateOrder)]
    public class XRSwitchRaysToTeleportManager : MonoBehaviour
    {
        public const int k_UpdateOrder = XRInteractionUpdateOrder.k_Controllers - 1;

        /// <summary>
        /// Rays modes
        /// </summary>
        public enum RaysMode
        {
            Normal,
            Teleport
        }

        /// <summary>
        /// Type of controllers
        /// </summary>
        public enum Controller
        {
            HandController,
            MotionController
        }

        /// <summary>
        /// Triggers that will allow teleport mode
        /// </summary>
        public enum TriggerTeleportMode
        {
            TeleportAreaDetected,
            GesturePerformed
        }

        [SerializeField] private XRRayInteractor distalRayInteractor;

        [SerializeField] private XRRayInteractor teleportRayInteractor;

        [Tooltip("Defines the type of controllers used on this script.")] [SerializeField]
        private Controller controller;

        [Tooltip("In case of using a Hand Controller, defines what will trigger the teleport mode.")] [SerializeField]
        private TriggerTeleportMode triggerTeleportMode;

        [Tooltip("The length of the ray used to detect a Teleport Area.")] [SerializeField]
        private float rayTriggerLength = 15.0f;

        [Tooltip(
            "Defines the device position used for the ray starting point. By default, the PointerPos of the appropriate hand.")]
        [SerializeField]
        private InputActionProperty devicePos;

        [Tooltip(
            "Defines the device rotation used for the ray rotation. By default, the PointerRot of the appropriate hand.")]
        [SerializeField]
        private InputActionProperty deviceRot;

        [Tooltip("The duration for which the gesture must be kept to perform the switch.")] [SerializeField]
        private float durationGesture = 0.75f;

        [Tooltip("In case of using a Motion Controller, the action used to trigger the teleport mode.")]
        [SerializeField]
        private InputActionProperty activateTeleport;

        [Tooltip("In case of using a Motion Controller, the action used to cancel the teleport mode.")] [SerializeField]
        private InputActionProperty cancelTeleport;

        private IEnumerator _interactionEventRoutine;
        private bool _postponedDeactivateTeleport;
        private readonly List<InputAction> _locomotionActives = new List<InputAction>();

        [Space] public UnityEvent<IXRRayProvider, RaysMode> OnRaysModeSwitched =
            new UnityEvent<IXRRayProvider, RaysMode>();

        private XRHandTrackingSubsystem _subsystem;
        private XRHandedness _xrHandedness;

        private RaysMode _mode = RaysMode.Normal;
        private float _timer;

        /// <summary>
        /// Reference to the normal XRRayInteractor (distal selection).
        /// </summary>
        public XRRayInteractor DistalRayInteractor
        {
            get => distalRayInteractor;
            set => distalRayInteractor = value;
        }

        /// <summary>
        /// Reference to the teleport XRRayInteractor.
        /// </summary>
        public XRRayInteractor TeleportRayInteractor
        {
            get => teleportRayInteractor;
            set => teleportRayInteractor = value;
        }

        /// <summary>
        /// Gets the current rays mode.
        /// It should be Normal or Teleport.
        /// </summary>
        public RaysMode Mode => _mode;

        private void Awake()
        {
            _xrHandedness = GetComponentInParent<XRHandedness>();
            _interactionEventRoutine = OnAfterInteractionEvents();
        }

        private void OnEnable()
        {
            //In case of using a Hand Controller
            if (controller == Controller.HandController)
            {
                if (triggerTeleportMode == TriggerTeleportMode.TeleportAreaDetected)
                    SetupRayDetector();

                _subsystem ??= XRHandTrackingSubsystem.GetSubsystemInManager();

                if (_subsystem != null)
                {
                    _subsystem.OnHandsUpdated += UpdateFromHandData;
                }
            }

            //In case of using a Motion Controller
            else if (controller == Controller.MotionController)
            {
                SetupInteractors();
                StartCoroutine(_interactionEventRoutine);
            }
        }

        private void OnDisable()
        {
            //In case of using a Hand Controller
            if (controller == Controller.HandController)
            {
                if (triggerTeleportMode == TriggerTeleportMode.TeleportAreaDetected)
                    UnSetupRayDetector();

                _subsystem ??= XRHandTrackingSubsystem.GetSubsystemInManager();

                if (_subsystem != null)
                {
                    _subsystem.OnHandsUpdated -= UpdateFromHandData;
                }

                return;
            }

            //In case of using a Motion Controller
            UnsetupInteractors();
            StopCoroutine(_interactionEventRoutine);
        }

        private void SetupInteractors()
        {
            if (distalRayInteractor != null)
            {
                distalRayInteractor.selectEntered.AddListener(OnDistalRaySelectEntered);
                distalRayInteractor.selectExited.AddListener(OnDistalRaySelectExited);
                distalRayInteractor.uiHoverEntered.AddListener(OnDistalUIHoverEntered);
                distalRayInteractor.uiHoverExited.AddListener(OnDistalUIHoverExited);
            }

            if (activateTeleport.action != null)
            {
                activateTeleport.EnableDirectAction();

                activateTeleport.action.performed += StartTeleport;
                activateTeleport.action.performed += StartLocomotion;
                activateTeleport.action.canceled += CancelTeleport;
                activateTeleport.action.canceled += StopLocomotion;
            }

            if (cancelTeleport.action != null)
            {
                cancelTeleport.EnableDirectAction();

                cancelTeleport.action.performed += CancelTeleport;
                cancelTeleport.action.canceled += StopLocomotion;
            }
        }

        private void UnsetupInteractors()
        {
            if (distalRayInteractor != null)
            {
                distalRayInteractor.selectEntered.RemoveListener(OnDistalRaySelectEntered);
                distalRayInteractor.selectExited.RemoveListener(OnDistalRaySelectExited);
                distalRayInteractor.uiHoverEntered.RemoveListener(OnDistalUIHoverEntered);
                distalRayInteractor.uiHoverExited.RemoveListener(OnDistalUIHoverExited);
            }

            if (activateTeleport.action != null)
            {
                activateTeleport.DisableDirectAction();

                activateTeleport.action.performed -= StartTeleport;
                activateTeleport.action.performed -= StartLocomotion;
                activateTeleport.action.canceled -= CancelTeleport;
                activateTeleport.action.canceled -= StopLocomotion;
            }

            if (cancelTeleport.action != null)
            {
                cancelTeleport.DisableDirectAction();

                cancelTeleport.action.performed -= CancelTeleport;
                cancelTeleport.action.canceled -= StopLocomotion;
            }
        }

        private void SetupRayDetector()
        {
            devicePos.EnableDirectAction();
            deviceRot.EnableDirectAction();
        }

        private void UnSetupRayDetector()
        {
            devicePos.DisableDirectAction();
            deviceRot.DisableDirectAction();
        }

        private void OnDistalRaySelectEntered(SelectEnterEventArgs args)
        {
            DisableTPMode();
        }

        private void OnDistalRaySelectExited(SelectExitEventArgs args)
        {
            UpdateTPMode();
        }

        private void OnDistalUIHoverEntered(UIHoverEventArgs args)
        {
            DisableTPMode();
        }

        private void OnDistalUIHoverExited(UIHoverEventArgs args)
        {
            UpdateTPMode();
        }

        private void StartTeleport(InputAction.CallbackContext obj)
        {
            _postponedDeactivateTeleport = false;

            SwitchMode(RaysMode.Teleport);

            if (distalRayInteractor != null)
                distalRayInteractor.gameObject.SetActive(_mode == RaysMode.Normal);

            if (teleportRayInteractor != null)
                teleportRayInteractor.gameObject.SetActive(_mode == RaysMode.Teleport);
        }

        private void StartLocomotion(InputAction.CallbackContext context)
        {
            if (!context.started)
                return;

            _locomotionActives.Add(context.action);
        }

        private void CancelTeleport(InputAction.CallbackContext context)
        {
            _postponedDeactivateTeleport = true;

            SwitchMode(RaysMode.Normal);

            if (distalRayInteractor != null)
                distalRayInteractor.gameObject.SetActive(_mode == RaysMode.Normal);
        }

        private void StopLocomotion(InputAction.CallbackContext context)
        {
            _locomotionActives.Remove(context.action);

            if (_locomotionActives.Count == 0)
                DisableTPMode();
        }

        IEnumerator OnAfterInteractionEvents()
        {
            while (true)
            {
                yield return null;

                if (_postponedDeactivateTeleport)
                {
                    if (teleportRayInteractor != null)
                        teleportRayInteractor.gameObject.SetActive(false);

                    _postponedDeactivateTeleport = false;
                }
            }
        }

        private void UpdateFromHandData(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            if (triggerTeleportMode == TriggerTeleportMode.GesturePerformed)
            {
                var hand = _xrHandedness.Handedness == XrHandedness.XR_HAND_LEFT
                    ? _subsystem.LeftHand
                    : _subsystem.RightHand;

                _timer += Time.deltaTime;

                var tp = CheckHandsTeleport(ref hand);

                if (_mode == RaysMode.Normal && tp)
                {
                    if (_timer >= durationGesture && _mode == RaysMode.Normal)
                    {
                        SwitchMode(RaysMode.Teleport);
                    }
                }
                else if (_mode == RaysMode.Teleport && !tp)
                {
                    _timer = 0.0f;
                    SwitchMode(RaysMode.Normal);
                }
            }

            else if (triggerTeleportMode == TriggerTeleportMode.TeleportAreaDetected)
            {
                var pos = devicePos.action.ReadValue<Vector3>();
                var rot = deviceRot.action.ReadValue<Quaternion>();

                var pose = new Pose(pos, rot);
                XROriginUtility.TransformPose(ref pose, true);

                if (Physics.Raycast(pose.position, pose.forward, out var hit, rayTriggerLength))
                {
                    if (IsATeleport(hit.transform))
                    {
                        if (_mode != RaysMode.Teleport)
                        {
                            SwitchMode(RaysMode.Teleport);
                        }
                    }
                    else
                    {
                        if (_mode == RaysMode.Teleport)
                        {
                            SwitchMode(RaysMode.Normal);
                        }
                    }
                }
                else if (_mode == RaysMode.Teleport)
                {
                    SwitchMode(RaysMode.Normal);
                }
            }

            //Update to current mode
            if (distalRayInteractor != null)
                distalRayInteractor.gameObject.SetActive(_mode == RaysMode.Normal);

            if (teleportRayInteractor != null)
                teleportRayInteractor.gameObject.SetActive(_mode == RaysMode.Teleport);
        }

        private bool IsATeleport(Transform transform)
        {
            var parent = transform.root;
            if (parent.GetComponent<BaseTeleportationInteractable>())
                return true;

            foreach (Transform child in parent)
            {
                if (child.TryGetComponent<BaseTeleportationInteractable>(out var tp))
                    return true;
            }

            return false;
        }

        private void SwitchMode(RaysMode mode)
        {
            _mode = mode;
            OnRaysModeSwitched?.Invoke(_mode == RaysMode.Teleport ? teleportRayInteractor : distalRayInteractor, _mode);
        }

        private static bool CheckHandsTeleport(ref XRHandTrackingSubsystem.Hand hand)
        {
            var teleportFilter = new TeleportFilter()
            {
                Hand = hand,
                Middle = hand.GetFingerCurling(XrFinger.XR_HAND_FINGER_MIDDLE),
                Ring = hand.GetFingerCurling(XrFinger.XR_HAND_FINGER_RING),
                Pinky = hand.GetFingerCurling(XrFinger.XR_HAND_FINGER_PINKY),
            };

            return teleportFilter.IsTeleporting();
        }

        private void DisableTPMode()
        {
            activateTeleport.DisableDirectAction();
            cancelTeleport.DisableDirectAction();
        }

        private void UpdateTPMode()
        {
            activateTeleport.EnableDirectAction();
            cancelTeleport.EnableDirectAction();
        }

        private struct TeleportFilter
        {
            public XRHandTrackingSubsystem.Hand Hand;
            public float Middle;
            public float Ring;
            public float Pinky;

            public bool IsTeleporting()
            {
#if UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
                if (Hand.Handedness == XrHandedness.XR_HAND_LEFT && Input.GetKey(KeyCode.LeftControl))
                    return true;
                if (Hand.Handedness == XrHandedness.XR_HAND_RIGHT && Input.GetKey(KeyCode.RightControl))
                    return true;
#else
                if (Middle > 0.3f && Ring > 0.3f && Pinky > 0.3f)
                    return true;
#endif
                return false;
            }
        }
    }
}