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

/*
Transmission = Birdramon
*/

// TODO: Check out CScenarioScript class and it's CallCmdBlockCommonSelectWindow() method
[HarmonyPatch(typeof(ParameterCommonSelectWindowMode), "GetParam")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) })]
public static class Patch_ParameterCommonSelectWindowMode_GetParam
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, ref dynamic __result) {
        Plugin.Logger.LogInfo($"ParameterCommonSelectWindowMode::GetParam");
        Plugin.Logger.LogInfo($"window_type {window_type}");
        Plugin.Logger.LogInfo($"__result {__result}");
        Plugin.Logger.LogInfo($"m_type {__result.m_type}");
        Plugin.Logger.LogInfo($"m_blockIndex {__result.m_blockIndex}");
        Plugin.Logger.LogInfo($"m_bit {__result.m_bit}");
        Plugin.Logger.LogInfo($"m_dailyQusetPoint {__result.m_dailyQusetPoint}");
        Plugin.Logger.LogInfo($"m_coin {__result.m_coin}");
    }
}

[HarmonyPatch(typeof(ParameterCommonSelectWindowMode), "GetCommonSelectWindowCsvb")]
public static class Patch_ParameterCommonSelectWindowMode_GetCommonSelectWindowCsvb
{
    public static void Postfix(ref dynamic __result, ParameterCommonSelectWindow __instance) {
        Plugin.Logger.LogInfo($"ParameterCommonSelectWindowMode::GetParam");
        Plugin.Logger.LogInfo($"__result {__result}");
        Plugin.Logger.LogInfo($"__instance {__instance}");

        Plugin.Logger.LogInfo($"METHODS");
        foreach (var info in __result.GetType().GetMethods()) {
            Plugin.Logger.LogInfo(info);
        }
        Plugin.Logger.LogInfo($"PROPERTIES");
        foreach (var info in __result.GetType().GetProperties()) {
            Plugin.Logger.LogInfo(info);
        }
    }
}