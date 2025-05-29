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
    private const string StartTimestampKey = "DiscordRPC_StartTimestamp";

    private static string lastSceneName = "";

    static DiscordRPCEditor()
    {
        if (!EditorPrefs.HasKey(EnabledKey)) EditorPrefs.SetBool(EnabledKey, true);
        if (!EditorPrefs.HasKey(ShowProjectNameKey)) EditorPrefs.SetBool(ShowProjectNameKey, true);
        if (!EditorPrefs.HasKey(ShowSceneNameKey)) EditorPrefs.SetBool(ShowSceneNameKey, true);
        if (!EditorPrefs.HasKey(ShowUnityVersionKey)) EditorPrefs.SetBool(ShowUnityVersionKey, true);
        if (!EditorPrefs.HasKey(ShowPlatformKey)) EditorPrefs.SetBool(ShowPlatformKey, true);
        if (!EditorPrefs.HasKey(ShowGraphicsAPIKey)) EditorPrefs.SetBool(ShowGraphicsAPIKey, true);
        if (!EditorPrefs.HasKey(ShowStartTimeKey)) EditorPrefs.SetBool(ShowStartTimeKey, true);

        if (EditorPrefs.GetBool(EnabledKey))
        {
            EnableRPC();
        }
    }

    private static void EnableRPC()
    {
        if (discordInitialized)
            return;
        EditorApplication.update += Update;
        EditorApplication.quitting += Shutdown;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        TryInitializeDiscord();
        lastSceneName = SceneManager.GetActiveScene().name;
    }

    private static void DisableRPC()
    {
        if (!discordInitialized)
            return;
        Shutdown();
        EditorApplication.update -= Update;
        EditorApplication.quitting -= Shutdown;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        discordInitialized = false;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastSceneName = scene.name;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        lastSceneName = "";
    }

    private static void TryInitializeDiscord()
    {
        try
        {
            long appId = defaultApplicationId;
            string customIdStr = EditorPrefs.GetString(CustomAppIDKey, "");

            if (!string.IsNullOrEmpty(customIdStr) && long.TryParse(customIdStr, out long parsedId))
                appId = parsedId;

            try
            {
                discord = new Discord.Discord(appId, (UInt64)CreateFlags.NoRequireDiscord);
            }
            catch
            {
                discordInitialized = false;
                return;
            }

            try
            {
                activityManager = discord.GetActivityManager();
            }
            catch
            {
                discordInitialized = false;
                return;
            }

            if (!EditorPrefs.HasKey(StartTimestampKey) || EditorPrefs.GetInt(StartTimestampKey) == 0)
            {
                int unixTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                EditorPrefs.SetInt(StartTimestampKey, unixTime);
            }

            discordInitialized = true;
        }
        catch
        {
            discordInitialized = false;
        }
    }

    private static void Update()
    {
        if (!discordInitialized || !EditorPrefs.GetBool(EnabledKey))
        {
            if (discordInitialized)
            {
                DisableRPC();
            }
            return;
        }

        try
        {
            discord.RunCallbacks();

            if (EditorApplication.timeSinceStartup >= nextUpdateTime)
            {
                UpdateActivity();
                nextUpdateTime = EditorApplication.timeSinceStartup + 15;
            }
        }
        catch
        {
            discordInitialized = false;
        }
    }

    private static void UpdateActivity()
    {
        string unityVersion = Application.unityVersion;
        string platform = Application.platform.ToString();
        string graphicsAPI = SystemInfo.graphicsDeviceType.ToString();
        string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));

        bool showScene = EditorPrefs.GetBool(ShowSceneNameKey);
        string sceneName = SceneManager.GetActiveScene().name;

        if (showScene)
        {
            if (sceneName != lastSceneName)
                lastSceneName = sceneName;
        }
        else
        {
            lastSceneName = "";
        }

        string details = "";
        if (EditorPrefs.GetBool(ShowProjectNameKey)) details += $"Project: {projectName}";
        if (showScene && !string.IsNullOrEmpty(lastSceneName))
        {
            if (!string.IsNullOrEmpty(details)) details += " | ";
            details += $"Scene: {lastSceneName}.unity";
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
            int rpcStartTimestamp = EditorPrefs.GetInt(StartTimestampKey, 0);

            if (rpcStartTimestamp == 0)
            {
                rpcStartTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                EditorPrefs.SetInt(StartTimestampKey, rpcStartTimestamp);
            }

            activity.Timestamps = new ActivityTimestamps
            {
                Start = rpcStartTimestamp
            };
        }
        else
        {
            EditorPrefs.DeleteKey(StartTimestampKey);
        }

        try
        {
            activityManager.UpdateActivity(activity, result => { });
        }
        catch
        {
        }
    }

    private static void Shutdown()
    {
        try
        {
            discord?.Dispose();
        }
        catch
        {
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

            bool enabled = EditorPrefs.GetBool(EnabledKey);
            bool newEnabled = EditorGUILayout.Toggle("Enable Discord RPC", enabled);

            if (newEnabled != enabled)
            {
                EditorPrefs.SetBool(EnabledKey, newEnabled);
                if (newEnabled)
                {
                    EnableRPC();
                }
                else
                {
                    DisableRPC();
                    EditorPrefs.DeleteKey(StartTimestampKey);
                }
            }

            EditorGUI.BeginDisabledGroup(!EditorPrefs.GetBool(EnabledKey));
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
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
