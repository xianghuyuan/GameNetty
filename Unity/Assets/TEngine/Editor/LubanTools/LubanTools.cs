using UnityEditor;
using UnityEngine;

namespace TEngine.Editor
{
    public static class LubanTools
    {
        [MenuItem("TEngine/Luban/转表-客户端 &X", priority = -100)]
        private static void GenClientConfig()
        {
            string workDir = Application.dataPath + "/../../Tools/Luban";
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            string script = "./GenConfig_Client.sh";
#elif UNITY_EDITOR_WIN
            string script = "GenConfig_Client.bat";
#endif
            Debug.Log($"执行客户端转表，工作目录：{workDir}");
            ShellHelper.Run(script, workDir);
        }

        [MenuItem("TEngine/Luban/转表-服务器", priority = -99)]
        private static void GenServerConfig()
        {
            string workDir = Application.dataPath + "/../../Tools/Luban";
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            string script = "./GenConfig_Server.sh";
#elif UNITY_EDITOR_WIN
            string script = "GenConfig_Server.bat";
#endif
            Debug.Log($"执行服务器转表，工作目录：{workDir}");
            ShellHelper.Run(script, workDir);
        }

        [MenuItem("TEngine/Luban/转表-全部", priority = -98)]
        private static void GenAllConfig()
        {
            GenClientConfig();
            GenServerConfig();
        }
    }
}