using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class BattleCameraComponent : Entity, IAwake<Transform, Camera, CinemachineVirtualCamera>, IUpdate, IDestroy
    {
        public Transform Target { get; set; }
        public Camera MainCamera { get; set; }
        public CinemachineVirtualCamera VirtualCamera { get; set; }
        public GameObject VirtualCameraGameObject { get; set; }
        public float OrthographicSize { get; set; }
        public float XVelocity { get; set; }
        public bool IsReturningToAnchor { get; set; }
        public bool EnableDebugLogging { get; set; }
        public List<BattleCameraBackdropTileGroup> BackdropTileGroups { get; } = new();
    }

    public sealed class BattleCameraBackdropTileGroup
    {
        public float TileWidth;
        public float SpanWidth;
        public readonly List<Transform> Tiles = new();
    }
}
