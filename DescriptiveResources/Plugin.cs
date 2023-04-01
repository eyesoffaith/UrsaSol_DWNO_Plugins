using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace DescriptiveResources;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static MethodInfo original;
    
    public override void Load()
    {
        Plugin.Logger = base.Log;
        HarmonyFileLog.Enabled = true;
            
        // Plugin startup logic
        Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        foreach (var method in typeof(Action).GetMethods()) {
            Plugin.Logger.LogMessage($"{method}");
        }
        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("DescriptiveResources");
        harmony.PatchAll();
    }
}

// [HarmonyPatch(typeof(uItemPickPanelResult), "GetResultMessage")]
// public static class Patch_GetResultMessage
// {
//     // public static bool Prefix() {
//     //     return false;
//     // }

//     public static void Postfix(uint[] itemIds, ref dynamic __result, uItemPickPanelResult __instance) {
//         Plugin.Logger.LogMessage($"[Patch_GetResultMessage::Postfix]");
//         Plugin.Logger.LogMessage($"__instance = {__instance}");
//         Plugin.Logger.LogMessage($"__result = {__result}");
//         Plugin.Logger.LogMessage($"itemIds.Length = {itemIds.Length}");

//         // var a = AppMainScript.parameterManager.itemData;
//         // Plugin.Logger.LogMessage($"a = {a}");
//         // Plugin.Logger.LogMessage($"a.GetType() = {a.GetType()}");
//         // var b = a.GetParams();
//         // Plugin.Logger.LogMessage($"b = {b}");
//         // Plugin.Logger.LogMessage($"b.GetType() = {b.GetType()}");

//         // foreach (var parameterItemData in @params) {
//         //     Plugin.Logger.LogMessage($"{parameterItemData.GetName()}");
//         // }
//     }
// }

[HarmonyPatch(typeof(ItemPickPointTimeRebirth), "SetItemPickPointData")]
public static class Patch_SetItemPickPointData
{
    public static void Postfix(ParameterMaterialPickPointData parameterMaterialPickPointData, uItemPickPanelCommand __instance) {
        Plugin.Logger.LogMessage($"[Patch_SetItemPickPointData::Postfix]");
        Plugin.Logger.LogMessage($"__instance = {__instance}");

        var parameterMaterialPickPointData_id = Traverse.Create(parameterMaterialPickPointData).Property("m_id").GetValue();
        Plugin.Logger.LogMessage($"id = {parameterMaterialPickPointData_id}");
        var itemLength = parameterMaterialPickPointData.GetItemDataLength();
        for (int i =  0; i < itemLength; i++) {
            var itemData = parameterMaterialPickPointData.GetItemData(i);
            Plugin.Logger.LogMessage($"\titem_id = {itemData.id}");
            Plugin.Logger.LogMessage($"\titem_probability = {itemData.probability}");
        }
    }
}

[HarmonyPatch(typeof(uItemPickPanelCommand), "_GetMaterialPickPointMaterialKind")]
public static class Patch__GetMaterialPickPointMaterialKind
{
    public static void Postfix(ItemPickPointTimeRebirth _materialPickPoint, uItemPickPanelCommand __instance) {
        Plugin.Logger.LogMessage($"[Patch__GetMaterialPickPointMaterialKind::Postfix]");
        Plugin.Logger.LogMessage($"__instance = {__instance}");

        var parameterMaterialPickPointData = Traverse.Create(_materialPickPoint).Property("m_parameterMaterialPickPointData").GetValue();
        var parameterMaterialPickPointData_id = Traverse.Create(parameterMaterialPickPointData).Property("m_id").GetValue();
        Plugin.Logger.LogMessage($"parameterMaterialPickPointData = {parameterMaterialPickPointData}");
        Plugin.Logger.LogMessage($"id = {parameterMaterialPickPointData_id}");
    }
}

[HarmonyPatch(typeof(uItemPickPanelCommand), "OpenMessageWindow")]
public static class Patch_OpenMessageWindow
{
    // public static bool Prefix() {
    //     return false;
    // }

    public static void Postfix(Action callback, uItemPickPanelCommand __instance) {
        Plugin.Logger.LogMessage($"[Patch_OpenMessageWindow::Postfix]");
        Plugin.Logger.LogMessage($"__instance = {__instance}");
        Plugin.Logger.LogMessage($"callback = {callback}");

        uCommonMessageWindow center = MainGameManager.Ref.MessageManager.GetCenter();
        // center.SetMessage("Beep Beep I'm A Sheep!", uCommonMessageWindow.Pos.Center);

        // var a = AppMainScript.parameterManager.itemData;
        // Plugin.Logger.LogMessage($"a = {a}");
        // Plugin.Logger.LogMessage($"a.GetType() = {a.GetType()}");
        // var b = a.GetParams();
        // Plugin.Logger.LogMessage($"b = {b}");
        // Plugin.Logger.LogMessage($"b.GetType() = {b.GetType()}");

        // foreach (var parameterItemData in @params) {
        //     Plugin.Logger.LogMessage($"{parameterItemData.GetName()}");
        // }
    }
}