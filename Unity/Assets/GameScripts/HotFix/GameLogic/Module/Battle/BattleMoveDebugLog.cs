using System;
using System.IO;
using UnityEngine;

namespace ET
{
    public static class BattleMoveDebugLog
    {
        public static string FilePath => Path.Combine("/Users/gxx/Documents/UGit/GameNetty/Logs", "BattleMoveDebug.log");

        public static void StartSession(long battleId, int battleType)
        {
            try
            {
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
            }
            catch (Exception e)
            {
                Log.Error($"BattleMoveDebugLog.Write failed: {e}");
            }
        }
    }
}
