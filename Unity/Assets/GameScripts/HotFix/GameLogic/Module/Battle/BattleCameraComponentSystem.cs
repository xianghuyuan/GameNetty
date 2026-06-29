using Cinemachine;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(BattleCameraComponent))]
    [FriendOf(typeof(BattleCameraComponent))]
    public static partial class BattleCameraComponentSystem
    {
        private const float StableLeftScreenX = 0.30f;
        private const float StableRightScreenX = 0.45f;
        private const float PreferredScreenX = 0.38f;
        private const float RightFollowSmoothTime = 0.16f;
        private const float LeftFollowSmoothTime = 0.34f;
        private const float MaxRightFollowSpeed = 18f;
        private const float MaxLeftFollowSpeed = 8f;
        private const float CameraY = 0f;
        private const float CameraZ = -10f;
        private const float ReturnEpsilon = 0.05f;
        private const string BackdropTileSuffix = "[BattleTile]";
        private const int BackdropTileRepeatCount = 2;

        [EntitySystem]
        private static void Awake(this BattleCameraComponent self, Transform target, Camera mainCamera, CinemachineVirtualCamera virtualCamera)
        {
            self.Target = target;
            self.MainCamera = mainCamera;
            self.VirtualCamera = virtualCamera;
            self.VirtualCameraGameObject = virtualCamera != null ? virtualCamera.gameObject : null;
            self.OrthographicSize = virtualCamera != null ? virtualCamera.m_Lens.OrthographicSize : 5f;
            self.XVelocity = 0f;
            self.IsReturningToAnchor = false;
            self.PrepareScrollableBackdrop();
            self.ResetToTarget();
        }

        [EntitySystem]
        private static void Destroy(this BattleCameraComponent self)
        {
            if (self.VirtualCameraGameObject != null)
            {
                UnityEngine.Object.Destroy(self.VirtualCameraGameObject);
            }

            self.Target = null;
            self.MainCamera = null;
            self.VirtualCamera = null;
            self.VirtualCameraGameObject = null;
            self.BackdropTileGroups.Clear();
            self.XVelocity = 0f;
            self.IsReturningToAnchor = false;
        }

        [EntitySystem]
        private static void Update(this BattleCameraComponent self)
        {
            self.UpdateCamera();
        }

        public static void SetDebugLogging(this BattleCameraComponent self, bool enabled)
        {
            self.EnableDebugLogging = enabled;
        }

        public static float GetCameraX(this BattleCameraComponent self)
        {
            if (self?.VirtualCameraGameObject != null)
            {
                return self.VirtualCameraGameObject.transform.position.x;
            }

            if (self?.MainCamera != null)
            {
                return self.MainCamera.transform.position.x;
            }

            if (self?.VirtualCamera != null)
            {
                return self.VirtualCamera.State.FinalPosition.x;
            }

            return 0f;
        }

        public static float GetViewportWidth(this BattleCameraComponent self)
        {
            float aspect = self.MainCamera != null
                ? self.MainCamera.aspect
                : (Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f);
            return self.OrthographicSize * aspect * 2f;
        }

        private static void ResetToTarget(this BattleCameraComponent self)
        {
            if (self.Target == null || self.VirtualCameraGameObject == null)
            {
                return;
            }

            float width = self.GetViewportWidth();
            float cameraX = GetCameraXForScreenX(self.Target.position.x, PreferredScreenX, width);
            self.VirtualCameraGameObject.transform.position = new Vector3(cameraX, CameraY, CameraZ);
            self.XVelocity = 0f;
            self.IsReturningToAnchor = false;
        }

        private static void UpdateCamera(this BattleCameraComponent self)
        {
            if (self.Target == null || self.VirtualCameraGameObject == null)
            {
                return;
            }

            float width = self.GetViewportWidth();
            Transform vcamTransform = self.VirtualCameraGameObject.transform;
            float currentX = vcamTransform.position.x;
            float targetX = self.Target.position.x;
            float stableLeftX = GetWorldXAtScreenX(currentX, StableLeftScreenX, width);
            float stableRightX = GetWorldXAtScreenX(currentX, StableRightScreenX, width);
            float preferredLeftX = GetWorldXAtScreenX(currentX, PreferredScreenX, width);
            float desiredX = currentX;

            if (targetX > stableRightX || targetX < stableLeftX)
            {
                self.IsReturningToAnchor = true;
            }

            if (self.IsReturningToAnchor)
            {
                desiredX = GetCameraXForScreenX(targetX, PreferredScreenX, width);
                if (Mathf.Abs(targetX - preferredLeftX) <= ReturnEpsilon)
                {
                    self.IsReturningToAnchor = false;
                    desiredX = currentX;
                    self.XVelocity = 0f;
                }
            }

            bool pullingLeft = desiredX < currentX;
            float smoothTime = pullingLeft ? LeftFollowSmoothTime : RightFollowSmoothTime;
            float maxSpeed = pullingLeft ? MaxLeftFollowSpeed : MaxRightFollowSpeed;
            float velocity = self.XVelocity;
            float nextX = Mathf.SmoothDamp(currentX, desiredX, ref velocity, smoothTime, maxSpeed, Time.deltaTime);
            self.XVelocity = velocity;

            vcamTransform.position = new Vector3(nextX, CameraY, CameraZ);
            self.RecycleBackdropTiles(nextX, width);

            if (self.EnableDebugLogging)
            {
                Log.Info($"BattleCameraComponent: targetX={targetX:F2}, cameraX={nextX:F2}, stable=[{stableLeftX:F2},{stableRightX:F2}], desiredX={desiredX:F2}");
            }
        }

        private static void PrepareScrollableBackdrop(this BattleCameraComponent self)
        {
            self.BackdropTileGroups.Clear();
            SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            float viewportWidth = self.GetViewportWidth();
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (renderer.gameObject.layer == LayerMask.NameToLayer("UI"))
                {
                    continue;
                }

                if (renderer.name.Contains(BackdropTileSuffix))
                {
                    continue;
                }

                float tileWidth = renderer.bounds.size.x;
                if (tileWidth < viewportWidth * 0.8f)
                {
                    continue;
                }

                BattleCameraBackdropTileGroup group = new()
                {
                    TileWidth = tileWidth,
                    SpanWidth = tileWidth * (BackdropTileRepeatCount * 2 + 1),
                };
                group.Tiles.Add(renderer.transform);

                for (int i = 1; i <= BackdropTileRepeatCount; i++)
                {
                    group.Tiles.Add(CreateBackdropTile(renderer, -tileWidth * i));
                    group.Tiles.Add(CreateBackdropTile(renderer, tileWidth * i));
                }

                self.BackdropTileGroups.Add(group);
            }
        }

        private static void RecycleBackdropTiles(this BattleCameraComponent self, float cameraX, float viewportWidth)
        {
            if (self.BackdropTileGroups.Count == 0)
            {
                return;
            }

            float cameraLeft = cameraX - viewportWidth * 0.5f;
            float cameraRight = cameraX + viewportWidth * 0.5f;

            foreach (BattleCameraBackdropTileGroup group in self.BackdropTileGroups)
            {
                foreach (Transform tile in group.Tiles)
                {
                    if (tile == null)
                    {
                        continue;
                    }

                    float tileLeft = tile.position.x - group.TileWidth * 0.5f;
                    float tileRight = tile.position.x + group.TileWidth * 0.5f;
                    Vector3 localPosition = tile.localPosition;

                    if (tileRight < cameraLeft)
                    {
                        localPosition.x += group.SpanWidth;
                        tile.localPosition = localPosition;
                    }
                    else if (tileLeft > cameraRight)
                    {
                        localPosition.x -= group.SpanWidth;
                        tile.localPosition = localPosition;
                    }
                }
            }
        }

        private static float GetWorldXAtScreenX(float cameraX, float screenX, float viewportWidth)
        {
            return cameraX + (screenX - 0.5f) * viewportWidth;
        }

        private static float GetCameraXForScreenX(float worldX, float screenX, float viewportWidth)
        {
            return worldX - (screenX - 0.5f) * viewportWidth;
        }

        private static Transform CreateBackdropTile(SpriteRenderer source, float offsetX)
        {
            string tileName = $"{source.name}{BackdropTileSuffix}{offsetX:+0;-0}";
            Transform parent = source.transform.parent;
            Transform existing = parent != null ? parent.Find(tileName) : null;
            if (existing != null)
            {
                return existing;
            }

            GameObject tile = UnityEngine.Object.Instantiate(source.gameObject, parent);
            tile.name = tileName;
            Vector3 localPosition = source.transform.localPosition;
            localPosition.x += offsetX;
            tile.transform.localPosition = localPosition;
            return tile.transform;
        }
    }
}
