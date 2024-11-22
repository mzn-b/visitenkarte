// /******************************************************************************
// * File: XRSwitchRaysToTeleportManagerEditor.cs
// * Copyright (c) 2024 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
// *
// * Confidential and Proprietary - Qualcomm Technologies, Inc.
// *
// ******************************************************************************/

using QCHT.Interactions.Distal;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(XRSwitchRaysToTeleportManager))]
public class XRSwitchRaysToTeleportManagerEditor : Editor
{
    // this are serialized variables in YourClass
    private SerializedProperty _distalRayInteractor;
    private SerializedProperty _teleportRayInteractor;

    private SerializedProperty _controller;
    private SerializedProperty _triggerTeleportMode;

    private SerializedProperty _rayTriggerLength;
    private SerializedProperty _devicePos;
    private SerializedProperty _deviceRot;
    private SerializedProperty _durationGesture;
    private SerializedProperty _activateTeleport;
    private SerializedProperty _cancelTeleport;

    private void OnEnable()
    {
        _distalRayInteractor = serializedObject.FindProperty("distalRayInteractor");
        _teleportRayInteractor = serializedObject.FindProperty("teleportRayInteractor");

        _controller = serializedObject.FindProperty("controller");
        _triggerTeleportMode = serializedObject.FindProperty("triggerTeleportMode");

        _rayTriggerLength = serializedObject.FindProperty("rayTriggerLength");
        _devicePos = serializedObject.FindProperty("devicePos");
        _deviceRot = serializedObject.FindProperty("deviceRot");
        _durationGesture = serializedObject.FindProperty("durationGesture");
        _activateTeleport = serializedObject.FindProperty("activateTeleport");
        _cancelTeleport = serializedObject.FindProperty("cancelTeleport");
    }

    public override void OnInspectorGUI()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((XRSwitchRaysToTeleportManager)target),
                typeof(XRSwitchRaysToTeleportManager), false);
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(_distalRayInteractor);
        EditorGUILayout.PropertyField(_teleportRayInteractor);
        EditorGUILayout.PropertyField(_controller);

        //0 : Hands Controllers
        //1 : Motion Controllers
        if (_controller.enumValueFlag == 0)
        {
            EditorGUILayout.PropertyField(_triggerTeleportMode);
            EditorGUILayout.PropertyField(_rayTriggerLength);

            //0: TeleportAreaDetected
            //1: GesturePerformed
            if (_triggerTeleportMode.enumValueFlag == 0)
            {
                EditorGUILayout.PropertyField(_devicePos);
                EditorGUILayout.PropertyField(_deviceRot);
            }
            else
            {
                EditorGUILayout.PropertyField(_durationGesture);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(_activateTeleport);
            EditorGUILayout.PropertyField(_cancelTeleport);
        }

        serializedObject.ApplyModifiedProperties();
    }
}