using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.UI;

namespace ConversionOverhaul;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    
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

/*
MaterialChange01 = Gostumon (metal)
MaterialChange02 = Gostumon (stone)
MaterialChange03 = Tyrannomon (wood)
AdventureInfo = Tyrannomon (liquid)
window_type _03 = Gaurdromon (lab item creation)
LaboratoryItemChange01 = Zudomon (liquid/stone hunt)
LaboratoryItemChange02 = Haguromon (metal/wood hunt)
*/

[HarmonyPatch(typeof(MainGameManager), "enableCommonSelectWindowUI")]
public static class Patch_MainGameManager_enableCommonSelectWindowUI
{
    public static void Postfix(bool enable, ParameterCommonSelectWindowMode.WindowType window_type, MainGameManager __instance) {
        Plugin.Logger.LogInfo($"MainGameManager::enableCommonSelectWindowUI");
        Plugin.Logger.LogInfo($"enable {enable}");
        Plugin.Logger.LogInfo($"window_type {window_type}");
    }
}

// TODO: Investigate this method further. This was the one that triggered during investigation!
[HarmonyPatch(typeof(ParameterCommonSelectWindowMode), "GetParam")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) })]
public static class Patch_ParameterCommonSelectWindowMode_GetParam
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, ref dynamic __result, ParameterCommonSelectWindowMode __instance) {
        Plugin.Logger.LogInfo($"ParameterCommonSelectWindowMode::GetParam");
        Plugin.Logger.LogInfo($"window_type {window_type}");
        Plugin.Logger.LogInfo($"__result {__result}");
        Plugin.Logger.LogInfo($"__instance {__instance}");
    }
}

[HarmonyPatch(typeof(ParameterCommonSelectWindow), "GetParam")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType), typeof(int) })]
public static class Patch_ParameterCommonSelectWindow_GetParam
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, int record_index, ref dynamic __result, ParameterCommonSelectWindow __instance) {
        Plugin.Logger.LogInfo($"ParameterCommonSelectWindow::GetParam");
        Plugin.Logger.LogInfo($"window_type {window_type}");
        Plugin.Logger.LogInfo($"record_index {record_index}");
        Plugin.Logger.LogInfo($"__result {__result}");
        Plugin.Logger.LogInfo($"__instance {__instance}");
    }
}