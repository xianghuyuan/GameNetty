using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(OuterMessage.HttpGetRouterResponse)]
    public partial class HttpGetRouterResponse : MessageObject
    {
        public static HttpGetRouterResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(HttpGetRouterResponse), isFromPool) as HttpGetRouterResponse;
        }

        [MemoryPackOrder(0)]
        public List<string> Realms { get; set; } = new();

        [MemoryPackOrder(1)]
        public List<string> Routers { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Realms.Clear();
            this.Routers.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.RouterSync)]
    public partial class RouterSync : MessageObject
    {
        public static RouterSync Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(RouterSync), isFromPool) as RouterSync;
        }

        [MemoryPackOrder(0)]
        public uint ConnectId { get; set; }

        [MemoryPackOrder(1)]
        public string Address { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConnectId = default;
            this.Address = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_TestRequest)]
    [ResponseType(nameof(M2C_TestResponse))]
    public partial class C2M_TestRequest : MessageObject, ILocationRequest
    {
        public static C2M_TestRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestRequest), isFromPool) as C2M_TestRequest;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string request { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.request = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_TestResponse)]
    public partial class M2C_TestResponse : MessageObject, IResponse
    {
        public static M2C_TestResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestResponse), isFromPool) as M2C_TestResponse;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public string response { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.response = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2G_EnterMap)]
    [ResponseType(nameof(G2C_EnterMap))]
    public partial class C2G_EnterMap : MessageObject, ISessionRequest
    {
        public static C2G_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_EnterMap), isFromPool) as C2G_EnterMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_EnterMap)]
    public partial class G2C_EnterMap : MessageObject, ISessionResponse
    {
        public static G2C_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_EnterMap), isFromPool) as G2C_EnterMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        /// <summary>
        /// 自己的UnitId
        /// </summary>
        [MemoryPackOrder(3)]
        public long MyId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MyId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.MoveInfo)]
    public partial class MoveInfo : MessageObject
    {
        public static MoveInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(MoveInfo), isFromPool) as MoveInfo;
        }

        [MemoryPackOrder(0)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new();

        [MemoryPackOrder(1)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        [MemoryPackOrder(2)]
        public int TurnSpeed { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Points.Clear();
            this.Rotation = default;
            this.TurnSpeed = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.UnitInfo)]
    public partial class UnitInfo : MessageObject
    {
        public static UnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(UnitInfo), isFromPool) as UnitInfo;
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }

        [MemoryPackOrder(2)]
        public int Type { get; set; }

        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 Position { get; set; }

        [MemoryPackOrder(4)]
        public Unity.Mathematics.float3 Forward { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(5)]
        public Dictionary<int, long> KV { get; set; } = new();
        [MemoryPackOrder(6)]
        public MoveInfo MoveInfo { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.ConfigId = default;
            this.Type = default;
            this.Position = default;
            this.Forward = default;
            this.KV.Clear();
            this.MoveInfo = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_CreateUnits)]
    public partial class M2C_CreateUnits : MessageObject, IMessage
    {
        public static M2C_CreateUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateUnits), isFromPool) as M2C_CreateUnits;
        }

        [MemoryPackOrder(0)]
        public List<UnitInfo> Units { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Units.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_CreateMyUnit)]
    public partial class M2C_CreateMyUnit : MessageObject, IMessage
    {
        public static M2C_CreateMyUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateMyUnit), isFromPool) as M2C_CreateMyUnit;
        }

        [MemoryPackOrder(0)]
        public UnitInfo Unit { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Unit = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_StartSceneChange)]
    public partial class M2C_StartSceneChange : MessageObject, IMessage
    {
        public static M2C_StartSceneChange Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_StartSceneChange), isFromPool) as M2C_StartSceneChange;
        }

        [MemoryPackOrder(0)]
        public long SceneInstanceId { get; set; }

        [MemoryPackOrder(1)]
        public string SceneName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SceneInstanceId = default;
            this.SceneName = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_RemoveUnits)]
    public partial class M2C_RemoveUnits : MessageObject, IMessage
    {
        public static M2C_RemoveUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_RemoveUnits), isFromPool) as M2C_RemoveUnits;
        }

        [MemoryPackOrder(0)]
        public List<long> Units { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Units.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_PathfindingResult)]
    public partial class C2M_PathfindingResult : MessageObject, ILocationMessage
    {
        public static C2M_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_PathfindingResult), isFromPool) as C2M_PathfindingResult;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Position = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_Stop)]
    public partial class C2M_Stop : MessageObject, ILocationMessage
    {
        public static C2M_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_Stop), isFromPool) as C2M_Stop;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_PathfindingResult)]
    public partial class M2C_PathfindingResult : MessageObject, IMessage
    {
        public static M2C_PathfindingResult Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_PathfindingResult), isFromPool) as M2C_PathfindingResult;
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }

        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }

        [MemoryPackOrder(2)]
        public List<Unity.Mathematics.float3> Points { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.Position = default;
            this.Points.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_Stop)]
    public partial class M2C_Stop : MessageObject, IMessage
    {
        public static M2C_Stop Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_Stop), isFromPool) as M2C_Stop;
        }

        [MemoryPackOrder(0)]
        public int Error { get; set; }

        [MemoryPackOrder(1)]
        public long Id { get; set; }

        [MemoryPackOrder(2)]
        public Unity.Mathematics.float3 Position { get; set; }

        [MemoryPackOrder(3)]
        public Unity.Mathematics.quaternion Rotation { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Error = default;
            this.Id = default;
            this.Position = default;
            this.Rotation = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2G_Ping)]
    [ResponseType(nameof(G2C_Ping))]
    public partial class C2G_Ping : MessageObject, ISessionRequest
    {
        public static C2G_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Ping), isFromPool) as C2G_Ping;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_Ping)]
    public partial class G2C_Ping : MessageObject, ISessionResponse
    {
        public static G2C_Ping Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Ping), isFromPool) as G2C_Ping;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long Time { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Time = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_Test)]
    public partial class G2C_Test : MessageObject, ISessionMessage
    {
        public static G2C_Test Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Test), isFromPool) as G2C_Test;
        }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            
            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_Reload)]
    [ResponseType(nameof(M2C_Reload))]
    public partial class C2M_Reload : MessageObject, ISessionRequest
    {
        public static C2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_Reload), isFromPool) as C2M_Reload;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string Account { get; set; }

        [MemoryPackOrder(2)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.Password = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_Reload)]
    public partial class M2C_Reload : MessageObject, ISessionResponse
    {
        public static M2C_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_Reload), isFromPool) as M2C_Reload;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_Login)]
    [ResponseType(nameof(R2C_Login))]
    public partial class C2R_Login : MessageObject, ISessionRequest
    {
        public static C2R_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_Login), isFromPool) as C2R_Login;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 帐号
        /// </summary>
        [MemoryPackOrder(1)]
        public string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [MemoryPackOrder(2)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.Password = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_Login)]
    public partial class R2C_Login : MessageObject, ISessionResponse
    {
        public static R2C_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_Login), isFromPool) as R2C_Login;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public string Address { get; set; }

        [MemoryPackOrder(4)]
        public long Key { get; set; }

        [MemoryPackOrder(5)]
        public long GateId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Address = default;
            this.Key = default;
            this.GateId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2G_LoginGate)]
    [ResponseType(nameof(G2C_LoginGate))]
    public partial class C2G_LoginGate : MessageObject, ISessionRequest
    {
        public static C2G_LoginGate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_LoginGate), isFromPool) as C2G_LoginGate;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 帐号
        /// </summary>
        [MemoryPackOrder(1)]
        public long Key { get; set; }

        [MemoryPackOrder(2)]
        public long GateId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Key = default;
            this.GateId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_LoginGate)]
    public partial class G2C_LoginGate : MessageObject, ISessionResponse
    {
        public static G2C_LoginGate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_LoginGate), isFromPool) as G2C_LoginGate;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long PlayerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.PlayerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_TestHotfixMessage)]
    public partial class G2C_TestHotfixMessage : MessageObject, ISessionMessage
    {
        public static G2C_TestHotfixMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_TestHotfixMessage), isFromPool) as G2C_TestHotfixMessage;
        }

        [MemoryPackOrder(0)]
        public string Info { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Info = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_TestRobotCase)]
    [ResponseType(nameof(M2C_TestRobotCase))]
    public partial class C2M_TestRobotCase : MessageObject, ILocationRequest
    {
        public static C2M_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestRobotCase), isFromPool) as C2M_TestRobotCase;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_TestRobotCase)]
    public partial class M2C_TestRobotCase : MessageObject, ILocationResponse
    {
        public static M2C_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestRobotCase), isFromPool) as M2C_TestRobotCase;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.N = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_TestRobotCase2)]
    public partial class C2M_TestRobotCase2 : MessageObject, ILocationMessage
    {
        public static C2M_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestRobotCase2), isFromPool) as C2M_TestRobotCase2;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_TestRobotCase2)]
    public partial class M2C_TestRobotCase2 : MessageObject, ILocationMessage
    {
        public static M2C_TestRobotCase2 Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestRobotCase2), isFromPool) as M2C_TestRobotCase2;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_TransferMap)]
    [ResponseType(nameof(M2C_TransferMap))]
    public partial class C2M_TransferMap : MessageObject, ILocationRequest
    {
        public static C2M_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TransferMap), isFromPool) as C2M_TransferMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_TransferMap)]
    public partial class M2C_TransferMap : MessageObject, ILocationResponse
    {
        public static M2C_TransferMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TransferMap), isFromPool) as M2C_TransferMap;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2G_Benchmark)]
    [ResponseType(nameof(G2C_Benchmark))]
    public partial class C2G_Benchmark : MessageObject, ISessionRequest
    {
        public static C2G_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Benchmark), isFromPool) as C2G_Benchmark;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_Benchmark)]
    public partial class G2C_Benchmark : MessageObject, ISessionResponse
    {
        public static G2C_Benchmark Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Benchmark), isFromPool) as G2C_Benchmark;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_LoginAccount)]
    [ResponseType(nameof(R2C_LoginAccount))]
    public partial class C2R_LoginAccount : MessageObject, ISessionRequest
    {
        public static C2R_LoginAccount Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_LoginAccount), isFromPool) as C2R_LoginAccount;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string AccountName { get; set; }

        [MemoryPackOrder(2)]
        public string Password { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.AccountName = default;
            this.Password = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_LoginAccount)]
    public partial class R2C_LoginAccount : MessageObject, ISessionResponse
    {
        public static R2C_LoginAccount Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_LoginAccount), isFromPool) as R2C_LoginAccount;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public string Token { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Token = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.A2C_Disconnect)]
    public partial class A2C_Disconnect : MessageObject, ISessionMessage
    {
        public static A2C_Disconnect Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(A2C_Disconnect), isFromPool) as A2C_Disconnect;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.ServerInfoProto)]
    public partial class ServerInfoProto : MessageObject
    {
        public static ServerInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ServerInfoProto), isFromPool) as ServerInfoProto;
        }

        [MemoryPackOrder(0)]
        public int Id { get; set; }

        [MemoryPackOrder(1)]
        public int Status { get; set; }

        [MemoryPackOrder(2)]
        public string ServerName { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.Status = default;
            this.ServerName = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_GetServerInfos)]
    [ResponseType(nameof(R2C_GetServerInfos))]
    public partial class C2R_GetServerInfos : MessageObject, ISessionRequest
    {
        public static C2R_GetServerInfos Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_GetServerInfos), isFromPool) as C2R_GetServerInfos;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public string Token { get; set; }

        [MemoryPackOrder(1)]
        public string Account { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Token = default;
            this.Account = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_GetServerInfos)]
    public partial class R2C_GetServerInfos : MessageObject, ISessionResponse
    {
        public static R2C_GetServerInfos Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_GetServerInfos), isFromPool) as R2C_GetServerInfos;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        [MemoryPackOrder(0)]
        public List<ServerInfoProto> ServerInfosList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ServerInfosList.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.RoleInfoProto)]
    public partial class RoleInfoProto : MessageObject
    {
        public static RoleInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(RoleInfoProto), isFromPool) as RoleInfoProto;
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }

        [MemoryPackOrder(1)]
        public string Name { get; set; }

        [MemoryPackOrder(2)]
        public int State { get; set; }

        [MemoryPackOrder(3)]
        public string Account { get; set; }

        [MemoryPackOrder(4)]
        public long LastLoginTime { get; set; }

        [MemoryPackOrder(5)]
        public long CreateTime { get; set; }

        [MemoryPackOrder(6)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.Name = default;
            this.State = default;
            this.Account = default;
            this.LastLoginTime = default;
            this.CreateTime = default;
            this.ServerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_GetRoles)]
    [ResponseType(nameof(R2C_GetRoles))]
    public partial class C2R_GetRoles : MessageObject, ISessionRequest
    {
        public static C2R_GetRoles Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_GetRoles), isFromPool) as C2R_GetRoles;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public string Token { get; set; }

        [MemoryPackOrder(1)]
        public string Account { get; set; }

        [MemoryPackOrder(2)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Token = default;
            this.Account = default;
            this.ServerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_GetRoles)]
    public partial class R2C_GetRoles : MessageObject, ISessionResponse
    {
        public static R2C_GetRoles Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_GetRoles), isFromPool) as R2C_GetRoles;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(90)]
        public string Message { get; set; }

        [MemoryPackOrder(0)]
        public List<RoleInfoProto> RoleInfo { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RoleInfo.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_CreateRole)]
    [ResponseType(nameof(R2C_CreateRole))]
    public partial class C2R_CreateRole : MessageObject, ISessionRequest
    {
        public static C2R_CreateRole Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_CreateRole), isFromPool) as C2R_CreateRole;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public string Token { get; set; }

        [MemoryPackOrder(1)]
        public string Account { get; set; }

        [MemoryPackOrder(2)]
        public string Name { get; set; }

        [MemoryPackOrder(3)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Token = default;
            this.Account = default;
            this.Name = default;
            this.ServerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_CreateRole)]
    public partial class R2C_CreateRole : MessageObject, ISessionResponse
    {
        public static R2C_CreateRole Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_CreateRole), isFromPool) as R2C_CreateRole;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        [MemoryPackOrder(0)]
        public RoleInfoProto RoleInfo { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RoleInfo = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_DeleteRole)]
    [ResponseType(nameof(R2C_DeleteRole))]
    public partial class C2R_DeleteRole : MessageObject, ISessionRequest
    {
        public static C2R_DeleteRole Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_DeleteRole), isFromPool) as C2R_DeleteRole;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public string Token { get; set; }

        [MemoryPackOrder(1)]
        public string Account { get; set; }

        [MemoryPackOrder(2)]
        public string RoleInfoId { get; set; }

        [MemoryPackOrder(3)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Token = default;
            this.Account = default;
            this.RoleInfoId = default;
            this.ServerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_DeleteRole)]
    public partial class R2C_DeleteRole : MessageObject, ISessionResponse
    {
        public static R2C_DeleteRole Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_DeleteRole), isFromPool) as R2C_DeleteRole;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        [MemoryPackOrder(0)]
        public long DeletedRoleInfoId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.DeletedRoleInfoId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2R_GetRealmKey)]
    [ResponseType(nameof(R2C_GetRealmKey))]
    public partial class C2R_GetRealmKey : MessageObject, ISessionRequest
    {
        public static C2R_GetRealmKey Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_GetRealmKey), isFromPool) as C2R_GetRealmKey;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string Token { get; set; }

        [MemoryPackOrder(2)]
        public string Account { get; set; }

        [MemoryPackOrder(3)]
        public int ServerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Token = default;
            this.Account = default;
            this.ServerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.R2C_GetRealmKey)]
    public partial class R2C_GetRealmKey : MessageObject, ISessionResponse
    {
        public static R2C_GetRealmKey Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_GetRealmKey), isFromPool) as R2C_GetRealmKey;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public string Address { get; set; }

        [MemoryPackOrder(4)]
        public long Key { get; set; }

        [MemoryPackOrder(5)]
        public long GateId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Address = default;
            this.Key = default;
            this.GateId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2G_LoginGameGate)]
    [ResponseType(nameof(G2C_LoginGameGate))]
    public partial class C2G_LoginGameGate : MessageObject, ISessionRequest
    {
        public static C2G_LoginGameGate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_LoginGameGate), isFromPool) as C2G_LoginGameGate;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string AccountName { get; set; }

        [MemoryPackOrder(2)]
        public long Key { get; set; }

        [MemoryPackOrder(3)]
        public long RoleId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.AccountName = default;
            this.Key = default;
            this.RoleId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_LoginGameGate)]
    public partial class G2C_LoginGameGate : MessageObject, ISessionResponse
    {
        public static G2C_LoginGameGate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_LoginGameGate), isFromPool) as G2C_LoginGameGate;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long PlayerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.PlayerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2G_EnterGame)]
    [ResponseType(nameof(G2C_EnterGame))]
    public partial class C2G_EnterGame : MessageObject, ISessionRequest
    {
        public static C2G_EnterGame Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_EnterGame), isFromPool) as C2G_EnterGame;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.G2C_EnterGame)]
    public partial class G2C_EnterGame : MessageObject, ISessionResponse
    {
        public static G2C_EnterGame Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_EnterGame), isFromPool) as G2C_EnterGame;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long MyUnitId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MyUnitId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_GetBagInfo)]
    [ResponseType(nameof(M2C_GetBagInfo))]
    public partial class C2M_GetBagInfo : MessageObject, ILocationRequest
    {
        public static C2M_GetBagInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_GetBagInfo), isFromPool) as C2M_GetBagInfo;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_GetBagInfo)]
    public partial class M2C_GetBagInfo : MessageObject, ILocationResponse
    {
        public static M2C_GetBagInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_GetBagInfo), isFromPool) as M2C_GetBagInfo;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long Money { get; set; }

        [MemoryPackOrder(4)]
        public List<BagInfo> BagInfos { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Money = default;
            this.BagInfos.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_GetShopInfo)]
    [ResponseType(nameof(M2C_GetShopInfo))]
    public partial class C2M_GetShopInfo : MessageObject, ILocationRequest
    {
        public static C2M_GetShopInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_GetShopInfo), isFromPool) as C2M_GetShopInfo;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_GetShopInfo)]
    public partial class M2C_GetShopInfo : MessageObject, ILocationResponse
    {
        public static M2C_GetShopInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_GetShopInfo), isFromPool) as M2C_GetShopInfo;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<ShopInfo> ShopInfos { get; set; } = new();

        [MemoryPackOrder(4)]
        public long Coin { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ShopInfos.Clear();
            this.Coin = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_BuyShop)]
    [ResponseType(nameof(M2C_BuyShop))]
    public partial class C2M_BuyShop : MessageObject, ILocationRequest
    {
        public static C2M_BuyShop Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_BuyShop), isFromPool) as C2M_BuyShop;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int ShopId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ShopId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_BuyShop)]
    public partial class M2C_BuyShop : MessageObject, ILocationResponse
    {
        public static M2C_BuyShop Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_BuyShop), isFromPool) as M2C_BuyShop;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long Money { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Money = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.ShopInfo)]
    public partial class ShopInfo : MessageObject
    {
        public static ShopInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ShopInfo), isFromPool) as ShopInfo;
        }

        [MemoryPackOrder(0)]
        public int id { get; set; }

        [MemoryPackOrder(1)]
        public string name { get; set; }

        [MemoryPackOrder(2)]
        public int cost { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.id = default;
            this.name = default;
            this.cost = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.BagInfo)]
    public partial class BagInfo : MessageObject
    {
        public static BagInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(BagInfo), isFromPool) as BagInfo;
        }

        [MemoryPackOrder(0)]
        public int id { get; set; }

        [MemoryPackOrder(1)]
        public int count { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.id = default;
            this.count = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2Chat_SendChatInfo)]
    [ResponseType(nameof(Chat2C_SendChatInfo))]
    public partial class C2Chat_SendChatInfo : MessageObject, IActorChatRequest
    {
        public static C2Chat_SendChatInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2Chat_SendChatInfo), isFromPool) as C2Chat_SendChatInfo;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string ChatMessage { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ChatMessage = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.Chat2C_SendChatInfo)]
    public partial class Chat2C_SendChatInfo : MessageObject, IActorChatResponse
    {
        public static Chat2C_SendChatInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Chat2C_SendChatInfo), isFromPool) as Chat2C_SendChatInfo;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.Chat2C_NoticeChatInfo)]
    public partial class Chat2C_NoticeChatInfo : MessageObject, IMessage
    {
        public static Chat2C_NoticeChatInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Chat2C_NoticeChatInfo), isFromPool) as Chat2C_NoticeChatInfo;
        }

        [MemoryPackOrder(0)]
        public string Name { get; set; }

        [MemoryPackOrder(1)]
        public string ChatMessage { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Name = default;
            this.ChatMessage = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.ProductionProto)]
    public partial class ProductionProto : MessageObject
    {
        public static ProductionProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ProductionProto), isFromPool) as ProductionProto;
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }

        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }

        [MemoryPackOrder(2)]
        public long TargetTime { get; set; }

        [MemoryPackOrder(3)]
        public long StartTime { get; set; }

        [MemoryPackOrder(4)]
        public int ProductionState { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.ConfigId = default;
            this.TargetTime = default;
            this.StartTime = default;
            this.ProductionState = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_StartProduction)]
    [ResponseType(nameof(M2C_StartProduction))]
    public partial class C2M_StartProduction : MessageObject, ILocationRequest
    {
        public static C2M_StartProduction Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_StartProduction), isFromPool) as C2M_StartProduction;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ConfigId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_StartProduction)]
    public partial class M2C_StartProduction : MessageObject, ILocationResponse
    {
        public static M2C_StartProduction Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_StartProduction), isFromPool) as M2C_StartProduction;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        [MemoryPackOrder(1)]
        public ProductionProto ProductionProto { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ProductionProto = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_NoticeUnitNumeric)]
    public partial class M2C_NoticeUnitNumeric : MessageObject, IMessage
    {
        public static M2C_NoticeUnitNumeric Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_NoticeUnitNumeric), isFromPool) as M2C_NoticeUnitNumeric;
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(0)]
        public int NumericType { get; set; }

        [MemoryPackOrder(1)]
        public long NewValue { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.NumericType = default;
            this.NewValue = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_NoticeNumericMsg)]
    public partial class M2C_NoticeNumericMsg : MessageObject, IMessage
    {
        public static M2C_NoticeNumericMsg Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_NoticeNumericMsg), isFromPool) as M2C_NoticeNumericMsg;
        }

        [MemoryPackOrder(0)]
        public int NumericType { get; set; }

        [MemoryPackOrder(1)]
        public long NewValue { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.NumericType = default;
            this.NewValue = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_NoticeUnitNumericList)]
    public partial class M2C_NoticeUnitNumericList : MessageObject, IMessage
    {
        public static M2C_NoticeUnitNumericList Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_NoticeUnitNumericList), isFromPool) as M2C_NoticeUnitNumericList;
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public List<int> NumericTypeList { get; set; } = new();

        [MemoryPackOrder(2)]
        public List<long> NewValueList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.NumericTypeList.Clear();
            this.NewValueList.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_TestNumericValue)]
    [ResponseType(nameof(M2C_TestNumericValue))]
    public partial class C2M_TestNumericValue : MessageObject, ILocationRequest
    {
        public static C2M_TestNumericValue Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TestNumericValue), isFromPool) as C2M_TestNumericValue;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_TestNumericValue)]
    public partial class M2C_TestNumericValue : MessageObject, ILocationResponse
    {
        public static M2C_TestNumericValue Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TestNumericValue), isFromPool) as M2C_TestNumericValue;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.ItemProto)]
    public partial class ItemProto : MessageObject
    {
        public static ItemProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ItemProto), isFromPool) as ItemProto;
        }

        [MemoryPackOrder(0)]
        public int ConfigId { get; set; }

        [MemoryPackOrder(1)]
        public int ContainerType { get; set; }

        [MemoryPackOrder(2)]
        public long Id { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConfigId = default;
            this.ContainerType = default;
            this.Id = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_UpdateItemInfo)]
    public partial class M2C_UpdateItemInfo : MessageObject
    {
        public static M2C_UpdateItemInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_UpdateItemInfo), isFromPool) as M2C_UpdateItemInfo;
        }

        [MemoryPackOrder(0)]
        public int Op { get; set; }

        [MemoryPackOrder(1)]
        public ItemProto ItemInfo { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Op = default;
            this.ItemInfo = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_GetAllKnapsack)]
    [ResponseType(nameof(M2C_GetAllKnapsack))]
    public partial class C2M_GetAllKnapsack : MessageObject, ILocationRequest
    {
        public static C2M_GetAllKnapsack Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_GetAllKnapsack), isFromPool) as C2M_GetAllKnapsack;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_GetAllKnapsack)]
    public partial class M2C_GetAllKnapsack : MessageObject, ILocationResponse
    {
        public static M2C_GetAllKnapsack Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_GetAllKnapsack), isFromPool) as M2C_GetAllKnapsack;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public List<ItemProto> ItemList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.ItemList.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_AddKnapsackItem)]
    [ResponseType(nameof(M2C_AddKnapsackItem))]
    public partial class C2M_AddKnapsackItem : MessageObject, ILocationRequest
    {
        public static C2M_AddKnapsackItem Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_AddKnapsackItem), isFromPool) as C2M_AddKnapsackItem;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int ContainerType { get; set; }

        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ContainerType = default;
            this.ConfigId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_AddKnapsackItem)]
    public partial class M2C_AddKnapsackItem : MessageObject, ILocationResponse
    {
        public static M2C_AddKnapsackItem Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_AddKnapsackItem), isFromPool) as M2C_AddKnapsackItem;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_RemoveKnapsackItem)]
    [ResponseType(nameof(M2C_RemoveKnapsackItem))]
    public partial class C2M_RemoveKnapsackItem : MessageObject, ILocationRequest
    {
        public static C2M_RemoveKnapsackItem Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_RemoveKnapsackItem), isFromPool) as C2M_RemoveKnapsackItem;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int ContainerType { get; set; }

        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.ContainerType = default;
            this.ConfigId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_RemoveKnapsackItem)]
    public partial class M2C_RemoveKnapsackItem : MessageObject, ILocationResponse
    {
        public static M2C_RemoveKnapsackItem Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_RemoveKnapsackItem), isFromPool) as M2C_RemoveKnapsackItem;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.RankInfoProto)]
    public partial class RankInfoProto : MessageObject
    {
        public static RankInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(RankInfoProto), isFromPool) as RankInfoProto;
        }

        [MemoryPackOrder(0)]
        public long Id { get; set; }

        [MemoryPackOrder(1)]
        public long UnitId { get; set; }

        [MemoryPackOrder(2)]
        public string Name { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.UnitId = default;
            this.Name = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2Rank_GetRanksInfo)]
    [ResponseType(nameof(Rank2C_GetRanksInfo))]
    public partial class C2Rank_GetRanksInfo : MessageObject, IRankInfoRequest
    {
        public static C2Rank_GetRanksInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2Rank_GetRanksInfo), isFromPool) as C2Rank_GetRanksInfo;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.Rank2C_GetRanksInfo)]
    public partial class Rank2C_GetRanksInfo : MessageObject, IRankInfoResponse
    {
        public static Rank2C_GetRanksInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Rank2C_GetRanksInfo), isFromPool) as Rank2C_GetRanksInfo;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        [MemoryPackOrder(0)]
        public List<RankInfoProto> RankInfoProtoList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RankInfoProtoList.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.MailInfoProto)]
    public partial class MailInfoProto : MessageObject
    {
        public static MailInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(MailInfoProto), isFromPool) as MailInfoProto;
        }

        [MemoryPackOrder(0)]
        public long MailId { get; set; }

        [MemoryPackOrder(1)]
        public string Title { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.MailId = default;
            this.Title = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2Mail_GetAllMailList)]
    [ResponseType(nameof(Mail2C_GetAllMailList))]
    public partial class C2Mail_GetAllMailList : MessageObject, IMailInfoRequest
    {
        public static C2Mail_GetAllMailList Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2Mail_GetAllMailList), isFromPool) as C2Mail_GetAllMailList;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.Mail2C_GetAllMailList)]
    public partial class Mail2C_GetAllMailList : MessageObject, IMailInfoResponse
    {
        public static Mail2C_GetAllMailList Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(Mail2C_GetAllMailList), isFromPool) as Mail2C_GetAllMailList;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(91)]
        public string Message { get; set; }

        [MemoryPackOrder(0)]
        public List<MailInfoProto> MailInfoList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MailInfoList.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_AllProductionList)]
    public partial class M2C_AllProductionList : MessageObject, IMessage
    {
        public static M2C_AllProductionList Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_AllProductionList), isFromPool) as M2C_AllProductionList;
        }

        [MemoryPackOrder(0)]
        public List<ProductionProto> ProductionProtoList { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ProductionProtoList.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.BuildingInfoProto)]
    public partial class BuildingInfoProto : MessageObject
    {
        public static BuildingInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(BuildingInfoProto), isFromPool) as BuildingInfoProto;
        }

        [MemoryPackOrder(4)]
        public long Id { get; set; }

        [MemoryPackOrder(0)]
        public int State { get; set; }

        [MemoryPackOrder(1)]
        public long ConfigId { get; set; }

        [MemoryPackOrder(2)]
        public long StartTime { get; set; }

        [MemoryPackOrder(3)]
        public long EndTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Id = default;
            this.State = default;
            this.ConfigId = default;
            this.StartTime = default;
            this.EndTime = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_BuildingEntity)]
    public partial class M2C_BuildingEntity : MessageObject
    {
        public static M2C_BuildingEntity Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_BuildingEntity), isFromPool) as M2C_BuildingEntity;
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(90)]
        public int Error { get; set; }

        [MemoryPackOrder(0)]
        public BuildingInfoProto BuildingEntity { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.BuildingEntity = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_CreateEnemy)]
    [ResponseType(nameof(M2C_CreateEnemy))]
    public partial class C2M_CreateEnemy : MessageObject, ILocationRequest
    {
        public static C2M_CreateEnemy Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_CreateEnemy), isFromPool) as C2M_CreateEnemy;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_CreateEnemy)]
    public partial class M2C_CreateEnemy : MessageObject, ILocationResponse
    {
        public static M2C_CreateEnemy Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateEnemy), isFromPool) as M2C_CreateEnemy;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public UnitInfo Enemy { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Enemy = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_AttackEnemy)]
    [ResponseType(nameof(M2C_AttackEnemy))]
    public partial class C2M_AttackEnemy : MessageObject, ILocationRequest
    {
        public static C2M_AttackEnemy Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_AttackEnemy), isFromPool) as C2M_AttackEnemy;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long EnemyId { get; set; }

        [MemoryPackOrder(2)]
        public int Damage { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.EnemyId = default;
            this.Damage = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_AttackEnemy)]
    public partial class M2C_AttackEnemy : MessageObject, ILocationResponse
    {
        public static M2C_AttackEnemy Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_AttackEnemy), isFromPool) as M2C_AttackEnemy;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long EnemyId { get; set; }

        [MemoryPackOrder(4)]
        public int CurrentHp { get; set; }

        [MemoryPackOrder(5)]
        public bool IsDead { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.EnemyId = default;
            this.CurrentHp = default;
            this.IsDead = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_StartCombat)]
    [ResponseType(nameof(M2C_StartCombat))]
    public partial class C2M_StartCombat : MessageObject, ISessionRequest
    {
        public static C2M_StartCombat Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_StartCombat), isFromPool) as C2M_StartCombat;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long targetId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.targetId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_StartCombat)]
    public partial class M2C_StartCombat : MessageObject, ISessionResponse
    {
        public static M2C_StartCombat Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_StartCombat), isFromPool) as M2C_StartCombat;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_AttackTarget)]
    [ResponseType(nameof(M2C_AttackTarget))]
    public partial class C2M_AttackTarget : MessageObject, ISessionRequest
    {
        public static C2M_AttackTarget Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_AttackTarget), isFromPool) as C2M_AttackTarget;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long targetId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.targetId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_AttackTarget)]
    public partial class M2C_AttackTarget : MessageObject, ISessionResponse
    {
        public static M2C_AttackTarget Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_AttackTarget), isFromPool) as M2C_AttackTarget;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public CombatResultProto result { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.result = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_SwitchControlMode)]
    [ResponseType(nameof(M2C_SwitchControlMode))]
    public partial class C2M_SwitchControlMode : MessageObject, ISessionRequest
    {
        public static C2M_SwitchControlMode Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_SwitchControlMode), isFromPool) as C2M_SwitchControlMode;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 0: Auto, 1: Manual
        /// </summary>
        [MemoryPackOrder(1)]
        public int Mode { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Mode = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_SwitchControlMode)]
    public partial class M2C_SwitchControlMode : MessageObject, ISessionResponse
    {
        public static M2C_SwitchControlMode Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_SwitchControlMode), isFromPool) as M2C_SwitchControlMode;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public int CurrentMode { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.CurrentMode = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_ControlModeChanged)]
    public partial class M2C_ControlModeChanged : MessageObject, IMessage
    {
        public static M2C_ControlModeChanged Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_ControlModeChanged), isFromPool) as M2C_ControlModeChanged;
        }

        [MemoryPackOrder(0)]
        public long UnitId { get; set; }

        [MemoryPackOrder(1)]
        public int NewMode { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.UnitId = default;
            this.NewMode = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 战斗结果
    [MemoryPackable]
    [Message(OuterMessage.CombatResultProto)]
    public partial class CombatResultProto : MessageObject
    {
        public static CombatResultProto Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(CombatResultProto), isFromPool) as CombatResultProto;
        }

        [MemoryPackOrder(0)]
        public long attackerId { get; set; }

        [MemoryPackOrder(1)]
        public long targetId { get; set; }

        [MemoryPackOrder(2)]
        public int damage { get; set; }

        [MemoryPackOrder(3)]
        public int attackerCurrentHp { get; set; }

        [MemoryPackOrder(4)]
        public int targetCurrentHp { get; set; }

        [MemoryPackOrder(5)]
        public bool targetDead { get; set; }

        [MemoryPackOrder(6)]
        public bool attackerDead { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.attackerId = default;
            this.targetId = default;
            this.damage = default;
            this.attackerCurrentHp = default;
            this.targetCurrentHp = default;
            this.targetDead = default;
            this.attackerDead = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ========== 战斗系统 (Battle System) ==========
    [MemoryPackable]
    [Message(OuterMessage.C2M_StartBattle)]
    [ResponseType(nameof(M2C_StartBattle))]
    public partial class C2M_StartBattle : MessageObject, ILocationRequest
    {
        public static C2M_StartBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_StartBattle), isFromPool) as C2M_StartBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 战斗类型: 0=WaveBattle, 1=Dungeon, 2=Boss
        /// </summary>
        [MemoryPackOrder(1)]
        public int battleType { get; set; }

        /// <summary>
        /// 总波数（波次战斗用）
        /// </summary>
        [MemoryPackOrder(2)]
        public int totalWaves { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.battleType = default;
            this.totalWaves = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_StartBattle)]
    public partial class M2C_StartBattle : MessageObject, ILocationResponse
    {
        public static M2C_StartBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_StartBattle), isFromPool) as M2C_StartBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long battleId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.battleId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 战斗结束（服务器主动推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_BattleEnd)]
    public partial class M2C_BattleEnd : MessageObject, IMessage
    {
        public static M2C_BattleEnd Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_BattleEnd), isFromPool) as M2C_BattleEnd;
        }

        [MemoryPackOrder(0)]
        public long battleId { get; set; }

        [MemoryPackOrder(1)]
        public bool success { get; set; }

        /// <summary>
        /// 持续时间（秒）
        /// </summary>
        [MemoryPackOrder(2)]
        public int duration { get; set; }

        /// <summary>
        /// 玩家伤害统计
        /// </summary>
        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(3)]
        public Dictionary<long, int> playerDamage { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.battleId = default;
            this.success = default;
            this.duration = default;
            this.playerDamage.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 波次开始（服务器主动推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_WaveStart)]
    public partial class M2C_WaveStart : MessageObject, IMessage
    {
        public static M2C_WaveStart Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_WaveStart), isFromPool) as M2C_WaveStart;
        }

        [MemoryPackOrder(0)]
        public long battleId { get; set; }

        [MemoryPackOrder(1)]
        public int waveNumber { get; set; }

        [MemoryPackOrder(2)]
        public int totalWaves { get; set; }

        [MemoryPackOrder(3)]
        public int monsterCount { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.battleId = default;
            this.waveNumber = default;
            this.totalWaves = default;
            this.monsterCount = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 波次完成（服务器主动推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_WaveComplete)]
    public partial class M2C_WaveComplete : MessageObject, IMessage
    {
        public static M2C_WaveComplete Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_WaveComplete), isFromPool) as M2C_WaveComplete;
        }

        [MemoryPackOrder(0)]
        public long battleId { get; set; }

        [MemoryPackOrder(1)]
        public int waveNumber { get; set; }

        [MemoryPackOrder(2)]
        public int totalWaves { get; set; }

        /// <summary>
        /// 持续时间（秒）
        /// </summary>
        [MemoryPackOrder(3)]
        public int duration { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.battleId = default;
            this.waveNumber = default;
            this.totalWaves = default;
            this.duration = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 客户端主动退出战斗
    [MemoryPackable]
    [Message(OuterMessage.C2M_ExitBattle)]
    [ResponseType(nameof(M2C_ExitBattle))]
    public partial class C2M_ExitBattle : MessageObject, ILocationRequest
    {
        public static C2M_ExitBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_ExitBattle), isFromPool) as C2M_ExitBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long battleId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.battleId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_ExitBattle)]
    public partial class M2C_ExitBattle : MessageObject, ILocationResponse
    {
        public static M2C_ExitBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_ExitBattle), isFromPool) as M2C_ExitBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 队长开启组队战斗
    [MemoryPackable]
    [Message(OuterMessage.C2M_TeamStartBattle)]
    [ResponseType(nameof(M2C_TeamStartBattle))]
    public partial class C2M_TeamStartBattle : MessageObject, ILocationRequest
    {
        public static C2M_TeamStartBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_TeamStartBattle), isFromPool) as C2M_TeamStartBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 队伍ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long teamId { get; set; }

        /// <summary>
        /// 战斗类型
        /// </summary>
        [MemoryPackOrder(2)]
        public int battleType { get; set; }

        /// <summary>
        /// 总波数（波次战斗用）
        /// </summary>
        [MemoryPackOrder(3)]
        public int totalWaves { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.teamId = default;
            this.battleType = default;
            this.totalWaves = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_TeamStartBattle)]
    public partial class M2C_TeamStartBattle : MessageObject, ILocationResponse
    {
        public static M2C_TeamStartBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TeamStartBattle), isFromPool) as M2C_TeamStartBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        /// <summary>
        /// 实际上是 RoomId
        /// </summary>
        [MemoryPackOrder(3)]
        public long battleId { get; set; }

        /// <summary>
        /// 所有参战成员ID
        /// </summary>
        [MemoryPackOrder(4)]
        public List<long> memberIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.battleId = default;
            this.memberIds.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 队员加入进行中的战斗
    [MemoryPackable]
    [Message(OuterMessage.C2M_JoinTeamBattle)]
    [ResponseType(nameof(M2C_JoinTeamBattle))]
    public partial class C2M_JoinTeamBattle : MessageObject, ILocationRequest
    {
        public static C2M_JoinTeamBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_JoinTeamBattle), isFromPool) as C2M_JoinTeamBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// RoomId
        /// </summary>
        [MemoryPackOrder(1)]
        public long battleId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.battleId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_JoinTeamBattle)]
    public partial class M2C_JoinTeamBattle : MessageObject, ILocationResponse
    {
        public static M2C_JoinTeamBattle Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_JoinTeamBattle), isFromPool) as M2C_JoinTeamBattle;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        /// <summary>
        /// 当前所有成员
        /// </summary>
        [MemoryPackOrder(3)]
        public List<long> memberIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.memberIds.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ========== 技能系统 (Skill System) ==========
    // 释放技能
    [MemoryPackable]
    [Message(OuterMessage.C2M_CastSkill)]
    [ResponseType(nameof(M2C_CastSkill))]
    public partial class C2M_CastSkill : MessageObject, ISessionRequest
    {
        public static C2M_CastSkill Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_CastSkill), isFromPool) as C2M_CastSkill;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 技能ID
        /// </summary>
        [MemoryPackOrder(1)]
        public int skillId { get; set; }

        /// <summary>
        /// 目标ID（可选）
        /// </summary>
        [MemoryPackOrder(2)]
        public long targetId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.skillId = default;
            this.targetId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_CastSkill)]
    public partial class M2C_CastSkill : MessageObject, ISessionResponse
    {
        public static M2C_CastSkill Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CastSkill), isFromPool) as M2C_CastSkill;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        /// <summary>
        /// 冷却结束时间戳
        /// </summary>
        [MemoryPackOrder(3)]
        public long cooldownEnd { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.cooldownEnd = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 技能冷却同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_SkillCooldown)]
    public partial class M2C_SkillCooldown : MessageObject, IMessage
    {
        public static M2C_SkillCooldown Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_SkillCooldown), isFromPool) as M2C_SkillCooldown;
        }

        [MemoryPackOrder(0)]
        public int skillId { get; set; }

        /// <summary>
        /// 冷却结束时间戳
        /// </summary>
        [MemoryPackOrder(1)]
        public long cooldownEnd { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.skillId = default;
            this.cooldownEnd = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ========== 组队系统 (Team System) ==========
    [MemoryPackable]
    [Message(OuterMessage.C2M_CreateTeam)]
    [ResponseType(nameof(M2C_CreateTeam))]
    public partial class C2M_CreateTeam : MessageObject, ISessionRequest
    {
        public static C2M_CreateTeam Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_CreateTeam), isFromPool) as C2M_CreateTeam;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_CreateTeam)]
    public partial class M2C_CreateTeam : MessageObject, ISessionResponse
    {
        public static M2C_CreateTeam Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateTeam), isFromPool) as M2C_CreateTeam;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long teamId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.teamId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_JoinTeam)]
    [ResponseType(nameof(M2C_JoinTeam))]
    public partial class C2M_JoinTeam : MessageObject, ISessionRequest
    {
        public static C2M_JoinTeam Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_JoinTeam), isFromPool) as C2M_JoinTeam;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public long teamId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.teamId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_JoinTeam)]
    public partial class M2C_JoinTeam : MessageObject, ISessionResponse
    {
        public static M2C_JoinTeam Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_JoinTeam), isFromPool) as M2C_JoinTeam;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.C2M_LeaveTeam)]
    [ResponseType(nameof(M2C_LeaveTeam))]
    public partial class C2M_LeaveTeam : MessageObject, ISessionRequest
    {
        public static C2M_LeaveTeam Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_LeaveTeam), isFromPool) as C2M_LeaveTeam;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_LeaveTeam)]
    public partial class M2C_LeaveTeam : MessageObject, ISessionResponse
    {
        public static M2C_LeaveTeam Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_LeaveTeam), isFromPool) as M2C_LeaveTeam;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 队伍更新（服务器主动推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_TeamUpdate)]
    public partial class M2C_TeamUpdate : MessageObject, IMessage
    {
        public static M2C_TeamUpdate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_TeamUpdate), isFromPool) as M2C_TeamUpdate;
        }

        [MemoryPackOrder(0)]
        public long teamId { get; set; }

        [MemoryPackOrder(1)]
        public long leaderId { get; set; }

        [MemoryPackOrder(2)]
        public List<long> memberIds { get; set; } = new();

        /// <summary>
        /// 0=Idle, 1=InCombat, 2=InDungeon
        /// </summary>
        [MemoryPackOrder(3)]
        public int teamState { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.teamId = default;
            this.leaderId = default;
            this.memberIds.Clear();
            this.teamState = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ========== 副本系统 (Dungeon System) ==========
    [MemoryPackable]
    [Message(OuterMessage.C2M_EnterDungeon)]
    [ResponseType(nameof(M2C_EnterDungeon))]
    public partial class C2M_EnterDungeon : MessageObject, ISessionRequest
    {
        public static C2M_EnterDungeon Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_EnterDungeon), isFromPool) as C2M_EnterDungeon;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        /// <summary>
        /// 副本配置ID
        /// </summary>
        [MemoryPackOrder(1)]
        public int dungeonConfigId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.dungeonConfigId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(OuterMessage.M2C_EnterDungeon)]
    public partial class M2C_EnterDungeon : MessageObject, ISessionResponse
    {
        public static M2C_EnterDungeon Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_EnterDungeon), isFromPool) as M2C_EnterDungeon;
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long dungeonInstanceId { get; set; }

        [MemoryPackOrder(4)]
        public long battleId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.dungeonInstanceId = default;
            this.battleId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 副本开始（服务器主动推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_DungeonStart)]
    public partial class M2C_DungeonStart : MessageObject, IMessage
    {
        public static M2C_DungeonStart Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_DungeonStart), isFromPool) as M2C_DungeonStart;
        }

        [MemoryPackOrder(0)]
        public long dungeonInstanceId { get; set; }

        [MemoryPackOrder(1)]
        public long teamId { get; set; }

        [MemoryPackOrder(2)]
        public int dungeonConfigId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.dungeonInstanceId = default;
            this.teamId = default;
            this.dungeonConfigId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 副本完成（服务器主动推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_DungeonComplete)]
    public partial class M2C_DungeonComplete : MessageObject, IMessage
    {
        public static M2C_DungeonComplete Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_DungeonComplete), isFromPool) as M2C_DungeonComplete;
        }

        [MemoryPackOrder(0)]
        public long dungeonInstanceId { get; set; }

        [MemoryPackOrder(1)]
        public bool success { get; set; }

        /// <summary>
        /// 持续时间（秒）
        /// </summary>
        [MemoryPackOrder(2)]
        public int duration { get; set; }

        /// <summary>
        /// 玩家伤害统计
        /// </summary>
        [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]
        [MemoryPackOrder(3)]
        public Dictionary<long, int> playerDamage { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.dungeonInstanceId = default;
            this.success = default;
            this.duration = default;
            this.playerDamage.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    // ========== 战斗表现同步 (Combat Visual Sync) ==========
    // 伤害同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_Damage)]
    public partial class M2C_Damage : MessageObject, IMessage
    {
        public static M2C_Damage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_Damage), isFromPool) as M2C_Damage;
        }

        /// <summary>
        /// 攻击者ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long attackerId { get; set; }

        /// <summary>
        /// 目标ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long targetId { get; set; }

        /// <summary>
        /// 伤害值
        /// </summary>
        [MemoryPackOrder(2)]
        public int damage { get; set; }

        /// <summary>
        /// 是否暴击
        /// </summary>
        [MemoryPackOrder(3)]
        public bool isCrit { get; set; }

        /// <summary>
        /// 目标当前血量
        /// </summary>
        [MemoryPackOrder(4)]
        public int targetCurrentHp { get; set; }

        /// <summary>
        /// 目标最大血量
        /// </summary>
        [MemoryPackOrder(5)]
        public int targetMaxHp { get; set; }

        /// <summary>
        /// 目标是否死亡
        /// </summary>
        [MemoryPackOrder(6)]
        public bool targetDead { get; set; }

        /// <summary>
        /// 伤害类型: 0=普攻, 1=技能, 2=DOT, 3=反伤
        /// </summary>
        [MemoryPackOrder(7)]
        public int damageType { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.attackerId = default;
            this.targetId = default;
            this.damage = default;
            this.isCrit = default;
            this.targetCurrentHp = default;
            this.targetMaxHp = default;
            this.targetDead = default;
            this.damageType = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 治疗同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_Heal)]
    public partial class M2C_Heal : MessageObject, IMessage
    {
        public static M2C_Heal Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_Heal), isFromPool) as M2C_Heal;
        }

        /// <summary>
        /// 施法者ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long casterId { get; set; }

        /// <summary>
        /// 目标ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long targetId { get; set; }

        /// <summary>
        /// 治疗量
        /// </summary>
        [MemoryPackOrder(2)]
        public int healAmount { get; set; }

        /// <summary>
        /// 目标当前血量
        /// </summary>
        [MemoryPackOrder(3)]
        public int targetCurrentHp { get; set; }

        /// <summary>
        /// 目标最大血量
        /// </summary>
        [MemoryPackOrder(4)]
        public int targetMaxHp { get; set; }

        /// <summary>
        /// 治疗类型: 0=技能, 1=HOT, 2=吸血
        /// </summary>
        [MemoryPackOrder(5)]
        public int healType { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.casterId = default;
            this.targetId = default;
            this.healAmount = default;
            this.targetCurrentHp = default;
            this.targetMaxHp = default;
            this.healType = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // Buff添加同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_BuffAdd)]
    public partial class M2C_BuffAdd : MessageObject, IMessage
    {
        public static M2C_BuffAdd Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_BuffAdd), isFromPool) as M2C_BuffAdd;
        }

        /// <summary>
        /// 单位ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long unitId { get; set; }

        /// <summary>
        /// Buff实例ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long buffId { get; set; }

        /// <summary>
        /// Buff配置ID
        /// </summary>
        [MemoryPackOrder(2)]
        public int configId { get; set; }

        /// <summary>
        /// 施法者ID
        /// </summary>
        [MemoryPackOrder(3)]
        public long casterId { get; set; }

        /// <summary>
        /// 效果类型
        /// </summary>
        [MemoryPackOrder(4)]
        public int effectType { get; set; }

        /// <summary>
        /// 持续时间(ms)
        /// </summary>
        [MemoryPackOrder(5)]
        public long duration { get; set; }

        /// <summary>
        /// 层数
        /// </summary>
        [MemoryPackOrder(6)]
        public int stacks { get; set; }

        /// <summary>
        /// 是否增益
        /// </summary>
        [MemoryPackOrder(7)]
        public bool isBuff { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.unitId = default;
            this.buffId = default;
            this.configId = default;
            this.casterId = default;
            this.effectType = default;
            this.duration = default;
            this.stacks = default;
            this.isBuff = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // Buff移除同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_BuffRemove)]
    public partial class M2C_BuffRemove : MessageObject, IMessage
    {
        public static M2C_BuffRemove Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_BuffRemove), isFromPool) as M2C_BuffRemove;
        }

        /// <summary>
        /// 单位ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long unitId { get; set; }

        /// <summary>
        /// Buff实例ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long buffId { get; set; }

        /// <summary>
        /// Buff配置ID
        /// </summary>
        [MemoryPackOrder(2)]
        public int configId { get; set; }

        /// <summary>
        /// 是否自然过期
        /// </summary>
        [MemoryPackOrder(3)]
        public bool isExpired { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.unitId = default;
            this.buffId = default;
            this.configId = default;
            this.isExpired = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // Buff层数更新（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_BuffStackUpdate)]
    public partial class M2C_BuffStackUpdate : MessageObject, IMessage
    {
        public static M2C_BuffStackUpdate Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_BuffStackUpdate), isFromPool) as M2C_BuffStackUpdate;
        }

        /// <summary>
        /// 单位ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long unitId { get; set; }

        /// <summary>
        /// Buff实例ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long buffId { get; set; }

        /// <summary>
        /// Buff配置ID
        /// </summary>
        [MemoryPackOrder(2)]
        public int configId { get; set; }

        /// <summary>
        /// 当前层数
        /// </summary>
        [MemoryPackOrder(3)]
        public int stacks { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.unitId = default;
            this.buffId = default;
            this.configId = default;
            this.stacks = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 单位血量同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_UnitHpSync)]
    public partial class M2C_UnitHpSync : MessageObject, IMessage
    {
        public static M2C_UnitHpSync Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_UnitHpSync), isFromPool) as M2C_UnitHpSync;
        }

        /// <summary>
        /// 单位ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long unitId { get; set; }

        /// <summary>
        /// 当前血量
        /// </summary>
        [MemoryPackOrder(1)]
        public int currentHp { get; set; }

        /// <summary>
        /// 最大血量
        /// </summary>
        [MemoryPackOrder(2)]
        public int maxHp { get; set; }

        /// <summary>
        /// 护盾值
        /// </summary>
        [MemoryPackOrder(3)]
        public long shieldValue { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.unitId = default;
            this.currentHp = default;
            this.maxHp = default;
            this.shieldValue = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 单位死亡同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_UnitDead)]
    public partial class M2C_UnitDead : MessageObject, IMessage
    {
        public static M2C_UnitDead Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_UnitDead), isFromPool) as M2C_UnitDead;
        }

        /// <summary>
        /// 死亡单位ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long unitId { get; set; }

        /// <summary>
        /// 击杀者ID
        /// </summary>
        [MemoryPackOrder(1)]
        public long killerId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.unitId = default;
            this.killerId = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 技能释放同步（服务器推送给周围玩家）
    [MemoryPackable]
    [Message(OuterMessage.M2C_SkillCast)]
    public partial class M2C_SkillCast : MessageObject, IMessage
    {
        public static M2C_SkillCast Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_SkillCast), isFromPool) as M2C_SkillCast;
        }

        /// <summary>
        /// 施法者ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long casterId { get; set; }

        /// <summary>
        /// 技能ID
        /// </summary>
        [MemoryPackOrder(1)]
        public int skillId { get; set; }

        /// <summary>
        /// 目标ID（可选）
        /// </summary>
        [MemoryPackOrder(2)]
        public long targetId { get; set; }

        /// <summary>
        /// 目标位置（AOE技能用）
        /// </summary>
        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 targetPos { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.casterId = default;
            this.skillId = default;
            this.targetId = default;
            this.targetPos = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // 怪物AI状态变化（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_MonsterStateChange)]
    public partial class M2C_MonsterStateChange : MessageObject, IMessage
    {
        public static M2C_MonsterStateChange Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_MonsterStateChange), isFromPool) as M2C_MonsterStateChange;
        }

        /// <summary>
        /// 怪物ID
        /// </summary>
        [MemoryPackOrder(0)]
        public long unitId { get; set; }

        /// <summary>
        /// 状态名称
        /// </summary>
        [MemoryPackOrder(1)]
        public string state { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.unitId = default;
            this.state = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    // Room内资源奖励同步（服务器推送）
    [MemoryPackable]
    [Message(OuterMessage.M2C_RoomRewardSync)]
    public partial class M2C_RoomRewardSync : MessageObject, IMessage
    {
        public static M2C_RoomRewardSync Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_RoomRewardSync), isFromPool) as M2C_RoomRewardSync;
        }

        /// <summary>
        /// 本次增加的金币
        /// </summary>
        [MemoryPackOrder(0)]
        public long AddMoney { get; set; }

        /// <summary>
        /// 当前总金币
        /// </summary>
        [MemoryPackOrder(1)]
        public long TotalMoney { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.AddMoney = default;
            this.TotalMoney = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class OuterMessage
    {
        public const ushort HttpGetRouterResponse = 10002;
        public const ushort RouterSync = 10003;
        public const ushort C2M_TestRequest = 10004;
        public const ushort M2C_TestResponse = 10005;
        public const ushort C2G_EnterMap = 10006;
        public const ushort G2C_EnterMap = 10007;
        public const ushort MoveInfo = 10008;
        public const ushort UnitInfo = 10009;
        public const ushort M2C_CreateUnits = 10010;
        public const ushort M2C_CreateMyUnit = 10011;
        public const ushort M2C_StartSceneChange = 10012;
        public const ushort M2C_RemoveUnits = 10013;
        public const ushort C2M_PathfindingResult = 10014;
        public const ushort C2M_Stop = 10015;
        public const ushort M2C_PathfindingResult = 10016;
        public const ushort M2C_Stop = 10017;
        public const ushort C2G_Ping = 10018;
        public const ushort G2C_Ping = 10019;
        public const ushort G2C_Test = 10020;
        public const ushort C2M_Reload = 10021;
        public const ushort M2C_Reload = 10022;
        public const ushort C2R_Login = 10023;
        public const ushort R2C_Login = 10024;
        public const ushort C2G_LoginGate = 10025;
        public const ushort G2C_LoginGate = 10026;
        public const ushort G2C_TestHotfixMessage = 10027;
        public const ushort C2M_TestRobotCase = 10028;
        public const ushort M2C_TestRobotCase = 10029;
        public const ushort C2M_TestRobotCase2 = 10030;
        public const ushort M2C_TestRobotCase2 = 10031;
        public const ushort C2M_TransferMap = 10032;
        public const ushort M2C_TransferMap = 10033;
        public const ushort C2G_Benchmark = 10034;
        public const ushort G2C_Benchmark = 10035;
        public const ushort C2R_LoginAccount = 10036;
        public const ushort R2C_LoginAccount = 10037;
        public const ushort A2C_Disconnect = 10038;
        public const ushort ServerInfoProto = 10039;
        public const ushort C2R_GetServerInfos = 10040;
        public const ushort R2C_GetServerInfos = 10041;
        public const ushort RoleInfoProto = 10042;
        public const ushort C2R_GetRoles = 10043;
        public const ushort R2C_GetRoles = 10044;
        public const ushort C2R_CreateRole = 10045;
        public const ushort R2C_CreateRole = 10046;
        public const ushort C2R_DeleteRole = 10047;
        public const ushort R2C_DeleteRole = 10048;
        public const ushort C2R_GetRealmKey = 10049;
        public const ushort R2C_GetRealmKey = 10050;
        public const ushort C2G_LoginGameGate = 10051;
        public const ushort G2C_LoginGameGate = 10052;
        public const ushort C2G_EnterGame = 10053;
        public const ushort G2C_EnterGame = 10054;
        public const ushort C2M_GetBagInfo = 10055;
        public const ushort M2C_GetBagInfo = 10056;
        public const ushort C2M_GetShopInfo = 10057;
        public const ushort M2C_GetShopInfo = 10058;
        public const ushort C2M_BuyShop = 10059;
        public const ushort M2C_BuyShop = 10060;
        public const ushort ShopInfo = 10061;
        public const ushort BagInfo = 10062;
        public const ushort C2Chat_SendChatInfo = 10063;
        public const ushort Chat2C_SendChatInfo = 10064;
        public const ushort Chat2C_NoticeChatInfo = 10065;
        public const ushort ProductionProto = 10066;
        public const ushort C2M_StartProduction = 10067;
        public const ushort M2C_StartProduction = 10068;
        public const ushort M2C_NoticeUnitNumeric = 10069;
        public const ushort M2C_NoticeNumericMsg = 10070;
        public const ushort M2C_NoticeUnitNumericList = 10071;
        public const ushort C2M_TestNumericValue = 10072;
        public const ushort M2C_TestNumericValue = 10073;
        public const ushort ItemProto = 10074;
        public const ushort M2C_UpdateItemInfo = 10075;
        public const ushort C2M_GetAllKnapsack = 10076;
        public const ushort M2C_GetAllKnapsack = 10077;
        public const ushort C2M_AddKnapsackItem = 10078;
        public const ushort M2C_AddKnapsackItem = 10079;
        public const ushort C2M_RemoveKnapsackItem = 10080;
        public const ushort M2C_RemoveKnapsackItem = 10081;
        public const ushort RankInfoProto = 10082;
        public const ushort C2Rank_GetRanksInfo = 10083;
        public const ushort Rank2C_GetRanksInfo = 10084;
        public const ushort MailInfoProto = 10085;
        public const ushort C2Mail_GetAllMailList = 10086;
        public const ushort Mail2C_GetAllMailList = 10087;
        public const ushort M2C_AllProductionList = 10088;
        public const ushort BuildingInfoProto = 10089;
        public const ushort M2C_BuildingEntity = 10090;
        public const ushort C2M_CreateEnemy = 10091;
        public const ushort M2C_CreateEnemy = 10092;
        public const ushort C2M_AttackEnemy = 10093;
        public const ushort M2C_AttackEnemy = 10094;
        public const ushort C2M_StartCombat = 10095;
        public const ushort M2C_StartCombat = 10096;
        public const ushort C2M_AttackTarget = 10097;
        public const ushort M2C_AttackTarget = 10098;
        public const ushort C2M_SwitchControlMode = 10099;
        public const ushort M2C_SwitchControlMode = 10100;
        public const ushort M2C_ControlModeChanged = 10101;
        public const ushort CombatResultProto = 10102;
        public const ushort C2M_StartBattle = 10103;
        public const ushort M2C_StartBattle = 10104;
        public const ushort M2C_BattleEnd = 10105;
        public const ushort M2C_WaveStart = 10106;
        public const ushort M2C_WaveComplete = 10107;
        public const ushort C2M_ExitBattle = 10108;
        public const ushort M2C_ExitBattle = 10109;
        public const ushort C2M_TeamStartBattle = 10110;
        public const ushort M2C_TeamStartBattle = 10111;
        public const ushort C2M_JoinTeamBattle = 10112;
        public const ushort M2C_JoinTeamBattle = 10113;
        public const ushort C2M_CastSkill = 10114;
        public const ushort M2C_CastSkill = 10115;
        public const ushort M2C_SkillCooldown = 10116;
        public const ushort C2M_CreateTeam = 10117;
        public const ushort M2C_CreateTeam = 10118;
        public const ushort C2M_JoinTeam = 10119;
        public const ushort M2C_JoinTeam = 10120;
        public const ushort C2M_LeaveTeam = 10121;
        public const ushort M2C_LeaveTeam = 10122;
        public const ushort M2C_TeamUpdate = 10123;
        public const ushort C2M_EnterDungeon = 10124;
        public const ushort M2C_EnterDungeon = 10125;
        public const ushort M2C_DungeonStart = 10126;
        public const ushort M2C_DungeonComplete = 10127;
        public const ushort M2C_Damage = 10128;
        public const ushort M2C_Heal = 10129;
        public const ushort M2C_BuffAdd = 10130;
        public const ushort M2C_BuffRemove = 10131;
        public const ushort M2C_BuffStackUpdate = 10132;
        public const ushort M2C_UnitHpSync = 10133;
        public const ushort M2C_UnitDead = 10134;
        public const ushort M2C_SkillCast = 10135;
        public const ushort M2C_MonsterStateChange = 10136;
        public const ushort M2C_RoomRewardSync = 10137;
    }
}