using System;
using System.IO;

namespace ET.Server
{
    /// <summary>
    /// 战斗调试日志工具类，输出到文档中便于排查问题。
    /// 日志文件位于 Logs/BattleDebug.log
    /// </summary>
    public static class LogDebugHelper
    {
        [StaticField]
        private static readonly object LockObj = new object();
        [StaticField]
        private static string LogPath;
        [StaticField]
        private static bool Initialized;

        public static void Init()
        {
            if (Initialized)
            {
                return;
            }

            lock (LockObj)
            {
                if (Initialized)
                {
                    return;
                }

                string logDir = "/Users/gxx/Documents/UGit/GameNetty/Logs";
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                LogPath = Path.Combine(logDir, "BattleDebug.log");
                File.WriteAllText(LogPath, $"=== Battle Debug Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                Initialized = true;
            }
        }

        public static void Log(string message)
        {
            if (!Initialized)
            {
                Init();
            }

            lock (LockObj)
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
                File.AppendAllText(LogPath, line);
            }
        }

        public static string GetUnitName(BattleUnit unit)
        {
            if (unit == null)
            {
                return "null";
            }

            return $"Unit{unit.Id}";
        }
    }
}
