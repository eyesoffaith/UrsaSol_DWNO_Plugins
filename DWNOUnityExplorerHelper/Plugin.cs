using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityExplorer.UI;

namespace DWNOUnityExplorerHelper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.sinai.unityexplorer", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;

    public static bool TrashInput()
    {
        return !UIManager.ShowMenu;
    }

    public override void Load()
    {
        Plugin.Logger = base.Log;
        HarmonyFileLog.Enabled = true;

        // Plugin startup logic
        Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(PadManager), "IsRepeat")]
public static class Patch_PadManager_IsRepeat
{
    public static bool Prefix()
    {
        return Plugin.TrashInput();
    }
}
 
[HarmonyPatch(typeof(PadManager), "IsTrigger")]
public static class Patch_PadManager_IsTrigger
{
    public static bool Prefix()
    {
        return Plugin.TrashInput();
    }
}

[HarmonyPatch(typeof(PadManager), "IsInput")]
public static class Patch_PadManager_IsInput
{
    public static bool Prefix()
    {
        return Plugin.TrashInput();
    }
}

[HarmonyPatch(typeof(PadManager), "IsRelease")]
public static class Patch_PadManager_IsRelease
{
    public static bool Prefix()
    {
        return Plugin.TrashInput();
    }
}

[HarmonyPatch(typeof(PadManager), "GetLeftStick")]
public static class Patch_PadManager_GetLeftStick
{
    public static bool Prefix()
    {
        return Plugin.TrashInput();
    }
}

[HarmonyPatch(typeof(PadManager), "GetRightStick")]
public static class Patch_PadManager_GetRightStick
{
    public static bool Prefix()
    {
        return Plugin.TrashInput();
    }
}