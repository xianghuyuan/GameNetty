using System;
using System.IO;
using UnityEngine;
using YooAsset;

namespace ET
{
    /// <summary>
    /// 配置加载器 - 负责从不同来源加载配置文件
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// 配置文件加载模式
        /// </summary>
        public enum LoadMode
        {
            /// <summary>
            /// 从 Resources 加载（开发模式）
            /// </summary>
            Resources,
            
            /// <summary>
            /// 从 StreamingAssets 加载
            /// </summary>
            StreamingAssets,
            
            /// <summary>
            /// 从 AssetBundle 加载（正式环境）
            /// </summary>
            AssetBundle,
            
            /// <summary>
            /// 从持久化目录加载（热更新后的配置）
            /// </summary>
            PersistentData
        }
        
        /// <summary>
        /// 当前加载模式
        /// </summary>
        public static LoadMode CurrentMode { get; set; } = LoadMode.AssetBundle;
        
        /// <summary>
        /// 配置文件目录名
        /// </summary>
        public static string ConfigDirectory { get; set; } = "Configs";
        
        /// <summary>
        /// YooAsset 资源包名称
        /// </summary>
        public static string PackageName { get; set; } = "DefaultPackage";
        
        /// <summary>
        /// 加载配置字节数据
        /// </summary>
        public static Luban.ByteBuf LoadByteBuf(string fileName)
        {
            byte[] bytes = null;
            
            try
            {
                switch (CurrentMode)
                {
                    case LoadMode.Resources:
                        bytes = LoadFromResources(fileName);
                        break;
                        
                    case LoadMode.StreamingAssets:
                        bytes = LoadFromStreamingAssets(fileName);
                        break;
                        
                    case LoadMode.AssetBundle:
                        bytes = LoadFromAssetBundle(fileName);
                        break;
                        
                    case LoadMode.PersistentData:
                        bytes = LoadFromPersistentData(fileName);
                        break;
                }
                
                if (bytes == null || bytes.Length == 0)
                {
                    Log.Error($"配置文件加载失败或为空: {fileName}");
                    return new Luban.ByteBuf(Array.Empty<byte>());
                }
                
                return new Luban.ByteBuf(bytes);
            }
            catch (Exception e)
            {
                Log.Error($"加载配置文件异常: {fileName}, Error: {e}");
                return new Luban.ByteBuf(Array.Empty<byte>());
            }
        }
        
        /// <summary>
        /// 从 Resources 加载
        /// </summary>
        private static byte[] LoadFromResources(string fileName)
        {
            var path = $"{ConfigDirectory}/{fileName}";
            var textAsset = UnityEngine.Resources.Load<TextAsset>(path);
            
            if (textAsset == null)
            {
                Log.Warning($"Resources 中未找到配置文件: {path}");
                return null;
            }
            
            return textAsset.bytes;
        }
        
        /// <summary>
        /// 从 StreamingAssets 加载
        /// </summary>
        private static byte[] LoadFromStreamingAssets(string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, ConfigDirectory, $"{fileName}.bytes");
            
            if (!File.Exists(path))
            {
                Log.Warning($"StreamingAssets 中未找到配置文件: {path}");
                return null;
            }
            
            return File.ReadAllBytes(path);
        }
        
        /// <summary>
        /// 从 AssetBundle 加载（使用 YooAsset）
        /// </summary>
        private static byte[] LoadFromAssetBundle(string fileName)
        {
            try
            {
                // 获取资源包
                var package = YooAssets.GetPackage(PackageName);
                if (package == null)
                {
                    Log.Error($"YooAsset 资源包不存在: {PackageName}，请先初始化 YooAsset");
                    return null;
                }
                
                // 检查包是否初始化完成
                if (package.InitializeStatus != EOperationStatus.Succeed)
                {
                    Log.Error($"YooAsset 资源包未初始化完成: {PackageName}, Status: {package.InitializeStatus}");
                    Log.Error("请确保在加载配置前完成 YooAsset 初始化，或使用 StreamingAssets 模式");
                    return null;
                }
                
                // 构建资源路径（使用可寻址名称，不需要完整路径）
                string assetPath = $"{fileName}";
                
                // 同步加载资源
                var handle = package.LoadAssetSync<TextAsset>(assetPath);
                
                if (handle.Status != EOperationStatus.Succeed)
                {
                    Log.Error($"YooAsset 加载配置文件失败: {assetPath}, Status: {handle.Status}");
                    Log.Error($"请检查: 1) 文件是否存在 2) 是否设置为可寻址 3) 是否已打包");
                    handle.Release();
                    return null;
                }
                
                TextAsset textAsset = handle.AssetObject as TextAsset;
                if (textAsset == null)
                {
                    Log.Error($"YooAsset 加载的资源不是 TextAsset: {assetPath}");
                    handle.Release();
                    return null;
                }
                
                // 复制字节数据（因为 handle 会被释放）
                byte[] bytes = new byte[textAsset.bytes.Length];
                Array.Copy(textAsset.bytes, bytes, textAsset.bytes.Length);
                
                // 释放资源句柄
                handle.Release();
                
                Log.Debug($"YooAsset 加载配置文件成功: {assetPath}, Size: {bytes.Length} bytes");
                
                return bytes;
            }
            catch (Exception e)
            {
                Log.Error($"YooAsset 加载配置文件异常: {fileName}, Error: {e}");
                return null;
            }
        }
        
        /// <summary>
        /// 从持久化目录加载（热更新配置）
        /// </summary>
        private static byte[] LoadFromPersistentData(string fileName)
        {
            var path = Path.Combine(Application.persistentDataPath, ConfigDirectory, $"{fileName}.bytes");
            
            if (!File.Exists(path))
            {
                Log.Warning($"PersistentData 中未找到配置文件: {path}，尝试从 StreamingAssets 加载");
                return LoadFromStreamingAssets(fileName);
            }
            
            return File.ReadAllBytes(path);
        }
    }
}
