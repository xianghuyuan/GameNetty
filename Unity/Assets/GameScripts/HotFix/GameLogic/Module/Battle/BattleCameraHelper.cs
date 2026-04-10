using Cinemachine;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 战斗相机辅助类
    /// Cinemachine 虚拟相机，实时跟随玩家，玩家始终在屏幕左侧。
    /// </summary>
    public static class BattleCameraHelper
    {
        private static CinemachineVirtualCamera _vcam;
        private static GameObject _vcamGo;
        private static Camera _mainCam;

        private const float CameraDistance = 10f;

        public static void SetupCameraFollow(Transform followTarget)
        {
            if (followTarget == null)
            {
                Log.Error("BattleCameraHelper: followTarget is null");
                return;
            }

            Cleanup();

            _mainCam = Camera.main;
            if (_mainCam != null)
            {
                var brain = _mainCam.GetComponent<CinemachineBrain>();
                if (brain == null)
                {
                    brain = _mainCam.gameObject.AddComponent<CinemachineBrain>();
                }
                brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
                brain.m_DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Style.Cut, 0f);
            }

            _vcamGo = new GameObject("BattleVirtualCamera");
            _vcam = _vcamGo.AddComponent<CinemachineVirtualCamera>();

            _vcam.Follow = followTarget;
            _vcam.m_Lens.OrthographicSize = _mainCam != null ? _mainCam.orthographicSize : 5f;
            _vcam.m_Lens.NearClipPlane = 0.1f;
            _vcam.m_Lens.FarClipPlane = 50f;

            var body = _vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            if (body != null)
            {
                body.m_XDamping = 0f;
                body.m_YDamping = 0f;
                body.m_ZDamping = 0f;
                body.m_TrackedObjectOffset = Vector3.zero;
                body.m_CameraDistance = CameraDistance;

                body.m_DeadZoneWidth = 0f;
                body.m_DeadZoneHeight = 1f;
                body.m_SoftZoneWidth = 1f;
                body.m_SoftZoneHeight = 1f;

                body.m_LookaheadTime = 0f;
                body.m_LookaheadSmoothing = 0f;

                // 玩家在屏幕 35% 位置（偏左），留出右侧视野
                body.m_ScreenX = 0.35f;
                body.m_ScreenY = 0.5f;
            }

            _vcam.m_Priority = 10;

            Log.Info("BattleCameraHelper: Cinemachine vcam setup complete");
        }

        /// <summary>
        /// 相机视口半宽（世界单位）
        /// </summary>
        private static float HalfWidth
        {
            get
            {
                if (_vcam != null && _vcam.m_Lens.Orthographic)
                    return _vcam.m_Lens.OrthographicSize * _vcam.m_Lens.Aspect;
                if (_mainCam != null && _mainCam.orthographic)
                    return _mainCam.orthographicSize * _mainCam.aspect;
                return 8f;
            }
        }

        /// <summary>
        /// 玩家在屏幕上的水平锚点比例
        /// </summary>
        private const float ScreenAnchorX = 0.35f;

        /// <summary>
        /// 获取虚拟相机的计算位置 X。
        /// 因为是零延迟实时跟随，直接从 Follow 目标反算：cameraX = targetX + (0.5 - screenX) * viewportWidth
        /// </summary>
        public static float GetCameraX()
        {
            if (_vcam != null && _vcam.Follow != null)
            {
                float targetX = _vcam.Follow.position.x;
                return targetX + (0.5f - ScreenAnchorX) * HalfWidth * 2f;
            }
            if (_mainCam != null) return _mainCam.transform.position.x;
            return 0f;
        }

        public static float GetCameraLeftBound()
        {
            return GetCameraX() - HalfWidth;
        }

        public static float GetCameraRightBound()
        {
            float cameraX = GetCameraX();
            float right = cameraX + HalfWidth;
            Log.Info($"BattleCameraHelper: cameraX={cameraX:F2}, halfWidth={HalfWidth:F2}, rightBound={right:F2}, mainCamX={(_mainCam != null ? _mainCam.transform.position.x.ToString("F2") : "null")}, followX={(_vcam != null && _vcam.Follow != null ? _vcam.Follow.position.x.ToString("F2") : "null")}");
            return right;
        }

        public static float GetCameraWidth()
        {
            return HalfWidth * 2f;
        }

        public static void Cleanup()
        {
            if (_vcamGo != null)
            {
                UnityEngine.Object.Destroy(_vcamGo);
                _vcamGo = null;
                _vcam = null;
            }
            _mainCam = null;
        }
    }
}
