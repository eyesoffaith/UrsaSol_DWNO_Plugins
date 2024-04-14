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

// TODO: Check ParameterCommonSelectWindowMode. There is a WindowType parameter that looks like a clue
// * MaterialChange#
// * LaboratoryItemChange##


[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    
    // public static ConfigEntry<float> resourceRarity;
    // public static ConfigEntry<float> resourceMultiplier;
    // public static ConfigEntry<int> cardProbability;
    // public static ConfigEntry<bool> breakNodeFullInventory;
    // public static ConfigEntry<bool> oneHitNode;

    public override void Load()
    {
        Plugin.Logger = base.Log;
        HarmonyFileLog.Enabled = true;
        
        // Plugin.oneHitNode = base.Config.Bind<bool>("One Hit Resource Node", "enable", true, "Whether the mod should gather all materials from a resource node at once.");
        // Plugin.resourceMultiplier = base.Config.Bind<float>("Resource Multiplier", "multiplier", 1, "Multiplies the number of materials pulled from resource nodes.");
        // Plugin.resourceRarity = base.Config.Bind<float>("Resource Rarity Multiplier", "multiplier", 1, "Multiplies the chance of pulling rare materials from resource nodes.");
        // Plugin.cardProbability = base.Config.Bind<int>("Card Drop Chance", "probability", 10, "Percent chance of a card dropping from resource node per material pull.");
        // Plugin.breakNodeFullInventory = base.Config.Bind<bool>("Break Material Node Toggle", "breakNodeFullInventory", false, "Run card chance and break material node even when inventory is full.");

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