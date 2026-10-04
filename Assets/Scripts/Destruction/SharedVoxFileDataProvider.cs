using System;
using System.Collections.Generic;
using System.IO;
using Better.StreamingAssets;
using UnityEngine;
using VoxelDestructionPro.Data;
using VoxelDestructionPro.VoxDataProviders;
using VoxReader.Interfaces;

/// <summary>
/// Loads one model from a multi-model VOX file while sharing the parsed file
/// between every part of the building. This avoids reading the New York VOX
/// file once for each of its 178 models.
/// </summary>
public sealed class SharedVoxFileDataProvider : VoxDataProvider
{
    [Tooltip("Path relative to StreamingAssets, without the .vox extension.")]
    public string modelPath;

    [Min(0)] public int modelIndex;

    private static readonly Dictionary<string, IVoxFile> FileCache = new();
    private static bool streamingAssetsInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        FileCache.Clear();
        streamingAssetsInitialized = false;
    }

    public override void Load(bool editorMode)
    {
        base.Load(editorMode);

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            Debug.LogWarning("Model path is empty.", this);
            return;
        }

        IVoxFile file = GetFile(editorMode);
        if (file == null || modelIndex < 0 || modelIndex >= file.Models.Length)
        {
            Debug.LogError(
                $"Model index {modelIndex} is not present in '{modelPath}.vox'.",
                this);
            return;
        }

        targetObj.AssignVoxelData(new VoxelData(file.Models[modelIndex]), editorMode);
    }

    private IVoxFile GetFile(bool editorMode)
    {
        string relativePath = NormalizePath(modelPath) + ".vox";
        string cacheKey;

        if (editorMode)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            cacheKey = "editor:" + Path.GetFullPath(fullPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"VOX file not found: {fullPath}", this);
                return null;
            }

            if (!FileCache.TryGetValue(cacheKey, out IVoxFile editorFile))
            {
                editorFile = VoxReader.VoxReader.Read(fullPath, false);
                FileCache.Add(cacheKey, editorFile);
            }

            return editorFile;
        }

        cacheKey = "runtime:" + relativePath;
        if (!FileCache.TryGetValue(cacheKey, out IVoxFile runtimeFile))
        {
            if (!streamingAssetsInitialized)
            {
                BetterStreamingAssets.Initialize();
                streamingAssetsInitialized = true;
            }

            if (!BetterStreamingAssets.FileExists(relativePath))
            {
                Debug.LogError($"VOX file not found in StreamingAssets: {relativePath}", this);
                return null;
            }

            runtimeFile = VoxReader.VoxReader.Read(relativePath, true);
            FileCache.Add(cacheKey, runtimeFile);
        }

        return runtimeFile;
    }

    private static string NormalizePath(string value)
    {
        string normalized = value.Replace('\\', '/').TrimStart('/');
        return normalized.EndsWith(".vox", StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(0, normalized.Length - 4)
            : normalized;
    }
}
