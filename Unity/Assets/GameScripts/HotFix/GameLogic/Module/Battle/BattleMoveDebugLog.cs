using System;
using System.IO;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    public static class BattleMoveDebugLog
    {
        // 日志文件路径
        private static string _filePath;

        // 移动速度追踪字典 - 使用战斗ID作为键的嵌套字典
        private static readonly Dictionary<long, Dictionary<long, MovementTracker>> _battleMovementTrackers = new Dictionary<long, Dictionary<long, MovementTracker>>();

        public static string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    // 将日志放在项目根目录的 Logs 文件夹下
                    _filePath = Path.Combine(Application.dataPath, "../Logs/BattleMoveDebug.log");
                    // 确保目录存在
                    var directory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                return _filePath;
            }
        }

        public static void StartSession(long battleId, int battleType)
        {
            try
            {
                // 清理该战斗的追踪器
                if (_battleMovementTrackers.ContainsKey(battleId))
                {
                    _battleMovementTrackers[battleId].Clear();
                }
                else
                {
                    _battleMovementTrackers[battleId] = new Dictionary<long, MovementTracker>();
                }

                File.AppendAllText(FilePath,
                    $"\n========== {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} BattleId={battleId} Type={battleType} ==========\n");
                Log.Info($"BattleMoveDebugLog: {FilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.StartSession failed: {e}");
            }
        }

        public static void Write(string message)
        {
            try
            {
                File.AppendAllText(FilePath, $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
                // 同时输出到控制台
                Debug.Log($"[BattleMoveDebug] {message}");
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.Write failed: {e}");
            }
        }

        /// <summary>
        /// 记录移动开始
        /// </summary>
        public static void RecordMoveStart(long battleId, long unitId, float3 fromPos, float3 toPos, float speed, float duration)
        {
            try
            {
                if (!_battleMovementTrackers.TryGetValue(battleId, out var trackers))
                {
                    trackers = new Dictionary<long, MovementTracker>();
                    _battleMovementTrackers[battleId] = trackers;
                }

                if (!trackers.ContainsKey(unitId))
                {
                    trackers[unitId] = new MovementTracker();
                }

                var tracker = trackers[unitId];
                tracker.StartMove(fromPos, toPos, speed, duration, Time.time);

                // 计算2D距离（忽略Y轴）
                float distance = math.distance(new float2(fromPos.x, fromPos.z), new float2(toPos.x, toPos.z));
                string message = $"MOVE_START Unit={unitId} From=({fromPos.x:F2},{fromPos.z:F2}) To=({toPos.x:F2},{toPos.z:F2}) Speed={speed:F2} Duration={duration:F2}s Distance={distance:F2}";
                Write(message);
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.RecordMoveStart failed: {e}");
            }
        }

        /// <summary>
        /// 记录位置更新
        /// </summary>
        public static void RecordPositionUpdate(long battleId, long unitId, float3 currentPos, float deltaTime)
        {
            try
            {
                if (_battleMovementTrackers.TryGetValue(battleId, out var trackers) && trackers.TryGetValue(unitId, out var tracker))
                {
                    tracker.UpdatePosition(currentPos, Time.time, deltaTime);

                    // 计算实际速度
                    float actualSpeed = tracker.GetActualSpeed(Time.time);
                    string message = $"POS_UPDATE Unit={unitId} Pos=({currentPos.x:F2},{currentPos.z:F2}) ActualSpeed={actualSpeed:F2} ExpectedSpeed={tracker.ExpectedSpeed:F2}";
                    Write(message);

                    // 如果速度差异超过20%，记录警告
                    if (math.abs(actualSpeed - tracker.ExpectedSpeed) > tracker.ExpectedSpeed * 0.2f)
                    {
                        string warning = $"SPEED_WARNING Unit={unitId} Actual={actualSpeed:F2} Expected={tracker.ExpectedSpeed:F2} Diff={math.abs(actualSpeed - tracker.ExpectedSpeed):F2}";
                        Write(warning);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.RecordPositionUpdate failed: {e}");
            }
        }

        /// <summary>
        /// 记录移动结束
        /// </summary>
        public static void RecordMoveEnd(long battleId, long unitId, float3 finalPos)
        {
            try
            {
                if (_battleMovementTrackers.TryGetValue(battleId, out var trackers) && trackers.TryGetValue(unitId, out var tracker))
                {
                    tracker.EndMove(finalPos, Time.time);

                    float totalDistance = tracker.TotalDistance;
                    float totalTime = tracker.TotalTime;
                    float avgSpeed = totalTime > 0 ? totalDistance / totalTime : 0;

                    string message = $"MOVE_END Unit={unitId} FinalPos=({finalPos.x:F2},{finalPos.z:F2}) TotalDistance={totalDistance:F2} TotalTime={totalTime:F2}s AvgSpeed={avgSpeed:F2}";
                    Write(message);

                    trackers.Remove(unitId);
                }
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.RecordMoveEnd failed: {e}");
            }
        }

        /// <summary>
        /// 记录应用移动指令
        /// </summary>
        public static void RecordApplyMoveCommand(long unitId, float3 targetPos, float moveSpeed, float duration, float moveCoefficient)
        {
            string message = $"APPLY_MOVE_CMD Unit={unitId} Target=({targetPos.x:F2},{targetPos.z:F2}) Speed={moveSpeed:F2} Duration={duration:F2} Coeff={moveCoefficient:F2}";
            Write(message);
        }

        /// <summary>
        /// 记录收到移动指令
        /// </summary>
        public static void RecordRecvMoveCmd(long unitId, bool isMoving, float3 targetPos, float moveSpeed, float duration)
        {
            string message = $"RECV_MOVE_CMD Unit={unitId} IsMoving={isMoving} Target=({targetPos.x:F2},{targetPos.z:F2}) Speed={moveSpeed:F2} Duration={duration:F2}";
            Write(message);
        }

        /// <summary>
        /// 记录服务端位置同步
        /// </summary>
        public static void RecordServerPosSync(long unitId, float3 serverPos)
        {
            string message = $"SERVER_POS_SYNC Unit={unitId} ServerPos=({serverPos.x:F2},{serverPos.z:F2})";
            Write(message);
        }

        /// <summary>
        /// 记录客户端位置同步
        /// </summary>
        public static void RecordClientPosSync(long unitId, float3 clientPos, long battleId)
        {
            string message = $"CLIENT_POS_SYNC Unit={unitId} ClientPos=({clientPos.x:F2},{clientPos.z:F2}) BattleId={battleId}";
            Write(message);
        }

        /// <summary>
        /// 清理战斗相关的所有追踪器
        /// </summary>
        public static void CleanupBattle(long battleId)
        {
            try
            {
                if (_battleMovementTrackers.TryGetValue(battleId, out var trackers))
                {
                    int trackerCount = trackers.Count;
                    trackers.Clear();
                    _battleMovementTrackers.Remove(battleId);

                    string message = $"CLEANUP_BATTLE BattleId={battleId} RemovedTrackers={trackerCount}";
                    Write(message);
                }
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.CleanupBattle failed: {e}");
            }
        }

        private class MovementTracker
        {
            public float3 StartPosition { get; private set; }
            public float3 TargetPosition { get; private set; }
            public float ExpectedSpeed { get; private set; }
            public float Duration { get; private set; }
            public float StartTime { get; private set; }
            public float3 LastPosition { get; private set; }
            public float LastUpdateTime { get; private set; }

            public float TotalDistance { get; private set; }
            public float TotalTime { get; private set; }

            public void StartMove(float3 startPos, float3 targetPos, float speed, float duration, float currentTime)
            {
                StartPosition = startPos;
                TargetPosition = targetPos;
                ExpectedSpeed = speed;
                Duration = duration;
                StartTime = currentTime;
                LastPosition = startPos;
                LastUpdateTime = currentTime;
                TotalDistance = 0;
                TotalTime = 0;
            }

            public void UpdatePosition(float3 currentPos, float currentTime, float deltaTime)
            {
                if (deltaTime > 0)
                {
                    // 计算2D距离（忽略Y轴）
                    float distanceThisFrame = math.distance(new float2(LastPosition.x, LastPosition.z), new float2(currentPos.x, currentPos.z));
                    TotalDistance += distanceThisFrame;
                    TotalTime += deltaTime;

                    LastPosition = currentPos;
                    LastUpdateTime = currentTime;
                }
            }

            public void EndMove(float3 finalPos, float endTime)
            {
                // 计算最终总距离
                float finalDistance = math.distance(new float2(StartPosition.x, StartPosition.z), new float2(finalPos.x, finalPos.z));
                TotalDistance = finalDistance;
                TotalTime = endTime - StartTime;
            }

            public float GetActualSpeed(float currentTime)
            {
                if (TotalTime > 0)
                    return TotalDistance / TotalTime;

                if (LastUpdateTime > StartTime)
                {
                    float elapsed = currentTime - StartTime;
                    float covered = math.distance(new float2(StartPosition.x, StartPosition.z), new float2(LastPosition.x, LastPosition.z));
                    return elapsed > 0 ? covered / elapsed : 0;
                }

                return 0;
            }
        }
    }
}