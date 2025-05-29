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

    private const long defaultApplicationId = 1372515579461636126;
    private const string largeImageKey = "default_icon";

    private const string EnabledKey = "DiscordRPC_Enabled";
    private const string CustomAppIDKey = "DiscordRPC_CustomAppID";
    private const string ShowProjectNameKey = "DiscordRPC_ShowProjectName";
    private const string ShowSceneNameKey = "DiscordRPC_ShowSceneName";
    private const string ShowUnityVersionKey = "DiscordRPC_ShowUnityVersion";
    private const string ShowPlatformKey = "DiscordRPC_ShowPlatform";
    private const string ShowGraphicsAPIKey = "DiscordRPC_ShowGraphicsAPI";
    private const string ShowStartTimeKey = "DiscordRPC_ShowStartTime";

    static DiscordRPCEditor()
    {
        // Set default preferences
        if (!EditorPrefs.HasKey(EnabledKey)) EditorPrefs.SetBool(EnabledKey, true);
        if (!EditorPrefs.HasKey(ShowProjectNameKey)) EditorPrefs.SetBool(ShowProjectNameKey, true);
        if (!EditorPrefs.HasKey(ShowSceneNameKey)) EditorPrefs.SetBool(ShowSceneNameKey, true);
        if (!EditorPrefs.HasKey(ShowUnityVersionKey)) EditorPrefs.SetBool(ShowUnityVersionKey, true);
        if (!EditorPrefs.HasKey(ShowPlatformKey)) EditorPrefs.SetBool(ShowPlatformKey, true);
        if (!EditorPrefs.HasKey(ShowGraphicsAPIKey)) EditorPrefs.SetBool(ShowGraphicsAPIKey, true);
        if (!EditorPrefs.HasKey(ShowStartTimeKey)) EditorPrefs.SetBool(ShowStartTimeKey, true);

        if (EditorPrefs.GetBool(EnabledKey))
        {
            EditorApplication.update += Update;
            EditorApplication.quitting += Shutdown;
            TryInitializeDiscord();
        }
    }

    private static void TryInitializeDiscord()
    {
        try
        {
            long appId = defaultApplicationId;
            string customIdStr = EditorPrefs.GetString(CustomAppIDKey, "");

            if (!string.IsNullOrEmpty(customIdStr) && long.TryParse(customIdStr, out long parsedId))
                appId = parsedId;

            discord = new Discord.Discord(appId, (UInt64)CreateFlags.NoRequireDiscord);
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
        if (!discordInitialized || !EditorPrefs.GetBool(EnabledKey))
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
        string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));

        string details = "";
        if (EditorPrefs.GetBool(ShowProjectNameKey)) details += $"Project: {projectName}";
        if (EditorPrefs.GetBool(ShowSceneNameKey))
        {
            if (!string.IsNullOrEmpty(details)) details += " | ";
            details += $"Scene: {sceneName}.unity";
        }

        string state = "";
        if (EditorPrefs.GetBool(ShowUnityVersionKey)) state += $"Unity {unityVersion}";
        if (EditorPrefs.GetBool(ShowPlatformKey)) state += (string.IsNullOrEmpty(state) ? "" : " | ") + platform;
        if (EditorPrefs.GetBool(ShowGraphicsAPIKey)) state += (string.IsNullOrEmpty(state) ? "" : ", ") + graphicsAPI;

        activity = new Activity
        {
            Details = details,
            State = state,
            Assets =
            {
                LargeImage = largeImageKey,
                LargeText = "Unity Editor Discord RPC | (Made by the GitHub user: wednesday2024)"
            }
        };

        if (EditorPrefs.GetBool(ShowStartTimeKey))
        {
            activity.Timestamps = new ActivityTimestamps
            {
                Start = rpcStartTimestamp
            };
        }

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

    [MenuItem("Project/Editor/Unity Editor Discord RPC Settings")]
    private static void ShowSettings()
    {
        EditorWindow.GetWindow<DiscordRPCSettingsWindow>("Discord RPC Settings");
    }

    public class DiscordRPCSettingsWindow : EditorWindow
    {
        private string customAppId;

        private void OnEnable()
        {
            customAppId = EditorPrefs.GetString(CustomAppIDKey, "");
        }

        private void OnGUI()
        {
            GUILayout.Label("Discord RPC Settings", EditorStyles.boldLabel);
            EditorPrefs.SetBool(EnabledKey, EditorGUILayout.Toggle("Enable Discord RPC", EditorPrefs.GetBool(EnabledKey)));

            GUILayout.Space(10);
            GUILayout.Label("Display Options", EditorStyles.boldLabel);
            EditorPrefs.SetBool(ShowProjectNameKey, EditorGUILayout.Toggle("Show Project Name", EditorPrefs.GetBool(ShowProjectNameKey)));
            EditorPrefs.SetBool(ShowSceneNameKey, EditorGUILayout.Toggle("Show Scene Name", EditorPrefs.GetBool(ShowSceneNameKey)));
            EditorPrefs.SetBool(ShowUnityVersionKey, EditorGUILayout.Toggle("Show Unity Version", EditorPrefs.GetBool(ShowUnityVersionKey)));
            EditorPrefs.SetBool(ShowPlatformKey, EditorGUILayout.Toggle("Show Platform", EditorPrefs.GetBool(ShowPlatformKey)));
            EditorPrefs.SetBool(ShowGraphicsAPIKey, EditorGUILayout.Toggle("Show Graphics API", EditorPrefs.GetBool(ShowGraphicsAPIKey)));
            EditorPrefs.SetBool(ShowStartTimeKey, EditorGUILayout.Toggle("Show Start Time", EditorPrefs.GetBool(ShowStartTimeKey)));

            GUILayout.Space(10);
            GUILayout.Label("Application ID Override", EditorStyles.boldLabel);
            customAppId = EditorGUILayout.TextField("Custom App ID", customAppId);
            if (GUILayout.Button("Save App ID"))
            {
                EditorPrefs.SetString(CustomAppIDKey, customAppId);
                Debug.Log("[DiscordRPCEditor] Custom Application ID saved. Restart the Editor to apply changes.");
            }
        }
    }
}
#endif
