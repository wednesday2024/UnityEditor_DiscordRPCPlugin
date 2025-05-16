#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Discord;

[InitializeOnLoad]
public static class DiscordRPCEditor
{
    private static Discord.Discord discord;
    private static ActivityManager activityManager;
    private static Activity activity;
    private static bool discordInitialized;
    private static double nextUpdateTime;
    private static long rpcStartTimestamp;

    private const long applicationId = 1372515579461636126;
    private const string largeImageKey = "default_icon";

    static DiscordRPCEditor()
    {
        EditorApplication.update += Update;
        EditorApplication.quitting += Shutdown;
        TryInitializeDiscord();
    }

    private static void TryInitializeDiscord()
    {
        try
        {
            discord = new Discord.Discord(applicationId, (UInt64)CreateFlags.NoRequireDiscord);
            activityManager = discord.GetActivityManager();

            rpcStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            discordInitialized = true;
            Debug.Log("[DiscordRPCEditor] Discord RPC initialized.");
        }
        catch (Exception e)
        {
            discordInitialized = false;
            Debug.Log("[DiscordRPCEditor] Discord was not detected or failed to initialize. The RPC will not start.\n" + e.Message);
        }
    }

    private static void Update()
    {
        if (!discordInitialized)
            return;

        try
        {
            discord.RunCallbacks();

            if (EditorApplication.timeSinceStartup >= nextUpdateTime)
            {
                UpdateActivity();
                nextUpdateTime = EditorApplication.timeSinceStartup + 15;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DiscordRPCEditor] Exception during update: " + e.Message);
            discordInitialized = false;
        }
    }

    private static void UpdateActivity()
    {
        string unityVersion = Application.unityVersion;
        string sceneName = SceneManager.GetActiveScene().name;
        string platform = Application.platform.ToString();
        string graphicsAPI = SystemInfo.graphicsDeviceType.ToString();

        string projectPath = Application.dataPath;
        string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(projectPath));

        activity = new Activity
        {
            Details = $"Project: {projectName} | Scene: {sceneName}.unity",
            State = $"Unity {unityVersion} | {platform}, {graphicsAPI}",
            Assets =
            {
                LargeImage = largeImageKey,
                LargeText = "Unity Editor Discord RPC | (Made by the GitHub user: wednesday2024)"
            },
            Timestamps =
            {
                Start = rpcStartTimestamp
            }
        };

        activityManager.UpdateActivity(activity, result =>
        {
            if (result != Result.Ok)
            {
                Debug.LogWarning($"[DiscordRPCEditor] Failed to update activity: {result}");
            }
        });
    }

    private static void Shutdown()
    {
        try
        {
            discord?.Dispose();
            Debug.Log("[DiscordRPCEditor] The Discord RPC has shutdown successfully.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DiscordRPCEditor] The Discord RPC failed to shutdown successfully: " + e.Message);
        }
    }
}
#endif
