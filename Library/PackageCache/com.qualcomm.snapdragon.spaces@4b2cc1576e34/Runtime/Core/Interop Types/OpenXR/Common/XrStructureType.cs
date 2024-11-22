/*
 * Copyright (c) Qualcomm Technologies, Inc. and/or its subsidiaries.
 * All rights reserved.
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 */

namespace Qualcomm.Snapdragon.Spaces
{
    internal enum XrStructureType
    {
        XR_TYPE_SYSTEM_GET_INFO = 4,
	XR_TYPE_SYSTEM_PROPERTIES = 5,

        XR_TYPE_SPACE_LOCATION = 42,

        #region Spatial anchor structure types
        XR_TYPE_SPATIAL_ANCHOR_CREATE_INFO_MSFT = 1000039000,
        XR_TYPE_SPATIAL_ANCHOR_SPACE_CREATE_INFO_MSFT = 1000039001,
        XR_TYPE_SPATIAL_ANCHOR_PERSISTENCE_INFO_MSFT = 1000142000,
        XR_TYPE_SPATIAL_ANCHOR_FROM_PERSISTED_ANCHOR_CREATE_INFO_MSFT = 1000142001,
        #endregion

        #region Hand Tracking structure types
        XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT = 1000051001,
        XR_TYPE_HAND_JOINTS_LOCATE_INFO_EXT = 1000051002,
        XR_TYPE_HAND_JOINT_LOCATIONS_EXT = 1000051003,
        #endregion

        #region Scene Understanding structure types
        XR_TYPE_SCENE_OBSERVER_CREATE_INFO_MSFT = 1000097000,
        XR_TYPE_SCENE_CREATE_INFO_MSFT = 1000097001,
        #endregion

        #region Image Tracking structure types
        XR_TYPE_IMAGE_TRACKER_DATA_SET_IMAGE_QCOM = 1000303003,
        XR_TYPE_IMAGE_TRACKER_CREATE_INFO_QCOM = 1000303004,
        XR_TYPE_IMAGE_TARGETS_TRACKING_MODE_INFO_QCOM = 1000303005,
        XR_TYPE_IMAGE_TARGETS_LOCATE_INFO_QCOM = 1000303006,
        XR_TYPE_IMAGE_TARGET_LOCATIONS_QCOM = 1000303009,
        #endregion

        #region Plane Detection structure types
        XR_TYPE_PLANE_DETECTION_CREATE_INFO_QCOM = 1000305001,
        XR_TYPE_PLANES_LOCATE_INFO_QCOM = 1000305002,
        XR_TYPE_PLANE_LOCATIONS_QCOM = 1000305003,
        XR_TYPE_PLANE_CONVEX_HULL_BUFFER_INFO_QCOM = 1000305007,
        XR_TYPE_PLANE_CONVEX_HULL_VERTEX_BUFFER_QCOM = 1000305008,
        #endregion

        #region Ray cast structure types
        XR_TYPE_RAY_CAST_CREATE_INFO_QCOM = 1000307000,
        XR_TYPE_RAY_COLLISIONS_GET_INFO_QCOM = 1000307001,
        XR_TYPE_RAY_COLLISIONS_QCOM = 1000307002,
        XR_TYPE_RAY_COLLISION_QCOM = 1000307003,
        #endregion

        #region Camera Access structure types
        XR_TYPE_CAMERA_INFO_QCOMX = 1000311000,
        XR_TYPE_CAMERA_FRAME_CONFIGURATION_QCOMX = 1000311001,
        XR_TYPE_CAMERA_ACTIVATION_INFO_QCOMX = 1000311002,
        XR_TYPE_CAMERA_FRAME_DATA_QCOMX = 1000311004,
        XR_TYPE_CAMERA_FRAME_BUFFERS_QCOMX = 1000311005,
        XR_TYPE_CAMERA_FRAME_PLANE_QCOMX = 1000311007,
        XR_TYPE_CAMERA_SENSOR_PROPERTIES_QCOMX = 1000311011,
        XR_TYPE_CAMERA_SENSOR_INTRINSICS_QCOMX = 1000311012,
        XR_TYPE_CAMERA_ACTIVATION_FRAME_CONFIGURATION_INFO_QCOMX = 1000311013,
        XR_TYPE_CAMERA_FRAME_BUFFER_QCOMX = 1000311014,
        XR_TYPE_CAMERA_FRAME_HARDWARE_BUFFER_QCOMX = 1000311015,
        XR_TYPE_CAMERA_SENSOR_INFOS_QCOMX = 1000311016,
        #endregion

        #region QR Code Tracking structure types
        XR_TYPE_SYSTEM_MARKER_TRACKING_PROPERTIES_QCOMX = 1000312000,
        XR_TYPE_MARKER_TRACKER_CREATE_INFO_QCOMX = 1000312001,
        XR_TYPE_MARKER_DETECTION_START_INFO_QCOMX = 1000312002,
        XR_TYPE_QR_CODE_VERSION_FILTER_QCOMX = 1000312003,
        XR_TYPE_MARKER_SPACE_CREATE_INFO_QCOMX = 1000312004,
        XR_TYPE_MARKER_TRACKING_MODE_INFO_QCOMX = 1000312005,
        XR_TYPE_USER_DEFINED_MARKER_SIZE_QCOMX = 1000312006,
        #endregion

        XR_TYPE_COMPONENT_VERSION_QCOM = 1000308000
    }
}
