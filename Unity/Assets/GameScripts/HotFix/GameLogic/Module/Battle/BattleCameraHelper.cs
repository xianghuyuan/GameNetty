using Cinemachine;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 战斗相机门面。实际更新逻辑由 BattleCameraComponentSystem 驱动。
    /// </summary>
    public static class BattleCameraHelper
    {
        private static bool _enableDebugLogging;

        public static void SetDebugLogging(bool enabled)
        {
            _enableDebugLogging = enabled;
            BattleCameraComponent component = global::Init.Root?.GetComponent<BattleCameraComponent>();
            component?.SetDebugLogging(enabled);
        }

        public static void SetupCameraFollow(Transform followTarget)
        {
            Scene root = global::Init.Root;
            if (root == null)
            {
                Log.Error("BattleCameraHelper: Init.Root is null");
                return;
            }

            if (followTarget == null)
            {
                Log.Error("BattleCameraHelper: followTarget is null");
                return;
            }

            Cleanup();

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain == null)
                {
                    brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
                }

                brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
                brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
            }

            GameObject vcamGo = new GameObject("BattleVirtualCamera");
            CinemachineVirtualCamera vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
            vcam.Follow = null;
            vcam.m_Lens.Orthographic = true;
            vcam.m_Lens.OrthographicSize = mainCam != null ? mainCam.orthographicSize : 5f;
            vcam.m_Lens.NearClipPlane = 0.1f;
            vcam.m_Lens.FarClipPlane = 50f;
            vcam.m_Priority = 10;

            BattleCameraComponent component = root.AddComponent<BattleCameraComponent, Transform, Camera, CinemachineVirtualCamera>(
                followTarget,
                mainCam,
                vcam);
            component.SetDebugLogging(_enableDebugLogging);

            Log.Info("BattleCameraHelper: BattleCameraComponent setup complete");
        }

        public static float GetCameraX()
        {
            BattleCameraComponent component = global::Init.Root?.GetComponent<BattleCameraComponent>();
            if (component != null)
            {
                return component.GetCameraX();
            }

            Camera mainCam = Camera.main;
            return mainCam != null ? mainCam.transform.position.x : 0f;
        }

        public static float GetCameraLeftBound()
        {
            return GetCameraX() - GetCameraWidth() * 0.5f;
        }

        public static float GetCameraRightBound()
        {
            float cameraX = GetCameraX();
            float right = cameraX + GetCameraWidth() * 0.5f;
            if (_enableDebugLogging)
            {
                Camera mainCam = Camera.main;
                BattleCameraComponent component = global::Init.Root?.GetComponent<BattleCameraComponent>();
                Log.Info($"BattleCameraHelper: cameraX={cameraX:F2}, width={GetCameraWidth():F2}, rightBound={right:F2}, mainCamX={(mainCam != null ? mainCam.transform.position.x.ToString("F2") : "null")}, targetX={(component?.Target != null ? component.Target.position.x.ToString("F2") : "null")}");
            }

            return right;
        }

        public static float GetCameraWidth()
        {
            BattleCameraComponent component = global::Init.Root?.GetComponent<BattleCameraComponent>();
            if (component != null)
            {
                return component.GetViewportWidth();
            }

            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.orthographic)
            {
                return mainCam.orthographicSize * mainCam.aspect * 2f;
            }

            return 16f;
        }

        public static void Cleanup()
        {
            BattleCameraComponent component = global::Init.Root?.GetComponent<BattleCameraComponent>();
            component?.Dispose();
        }
    }
}
