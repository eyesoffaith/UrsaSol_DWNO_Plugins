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

// NOTES:
//   - Got town material inventory
//   - Got player item inventory (needs refinement)
//   - Got selected item when selected!!!
// TODO: Would be nice to check WindowType when item is selected and skip processing if it's not one we're interested in
//   - Currently not sure how to get the WindowType from ParameterCommonSelectWindow or uCommonSelectWindowPanel

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

[HarmonyPatch(typeof(ParameterCommonSelectWindowMode), "GetParam")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) })]
public static class Patch_ParameterCommonSelectWindowMode_GetParam
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, ref dynamic __result) {
        Plugin.Logger.LogInfo($"ParameterCommonSelectWindowMode::GetParam");
        // Plugin.Logger.LogInfo($"window_type {window_type}");
        // Plugin.Logger.LogInfo($"__result {__result}");
        // Plugin.Logger.LogInfo($"m_type {__result.m_type}");
        // Plugin.Logger.LogInfo($"m_blockIndex {__result.m_blockIndex}");
        // Plugin.Logger.LogInfo($"m_bit {__result.m_bit}");
        // Plugin.Logger.LogInfo($"m_dailyQusetPoint {__result.m_dailyQusetPoint}");
        // Plugin.Logger.LogInfo($"m_coin {__result.m_coin}");
    }
}

[HarmonyPatch(typeof(uCommonSelectWindowPanel), "Setup")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) }, new ArgumentType[] { ArgumentType.Ref } )]
public static class Patch_uCommonSelectWindowPanel_Setup
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, uCommonSelectWindowPanel __instance) {
        Plugin.Logger.LogInfo($"uCommonSelectWindowPanel::Setup");
        // Plugin.Logger.LogInfo($"window_type {window_type}");
        // Plugin.Logger.LogInfo($"__instance {__instance}");

        dynamic m_itemPanel = Traverse.Create(__instance).Property("m_itemPanel").GetValue();
        Plugin.Logger.LogInfo($"m_itemPanel {m_itemPanel}");

        TownMaterialDataAccess m_materialData = (TownMaterialDataAccess)Traverse.Create(typeof(StorageData)).Property("m_materialData").GetValue();
        dynamic materialList = Traverse.Create(m_materialData).Property("m_materialDatas").GetValue();
        Plugin.Logger.LogInfo($"GOT TOWN MATERIAL ITEMS");
        // foreach (var material in materialList) {
        //     Plugin.Logger.LogInfo($"{Language.GetString(material.m_id)} [{material.m_id}] {material.m_material_num}");
        // }

        ItemStorageData m_ItemStorageData = (ItemStorageData)Traverse.Create(typeof(StorageData)).Property("m_ItemStorageData").GetValue();
        dynamic m_itemDataListTbl = Traverse.Create(m_ItemStorageData).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogInfo($"GOT PLAYER ITEMS");

        // foreach (var item in m_itemDataListTbl[(int)ItemStorageData.StorageType.PLAYER].ToArray()) {
        //     if (item.m_itemNum == 0) { continue; }
        //     Plugin.Logger.LogInfo($"{Language.GetString(item.m_itemID)} [{item.m_itemID}] {item.m_itemNum}");
        // }
    }
}

// [HarmonyPatch(AccessTools.TypeByName("CScenarioScript"), "CallCmdBlockCommonSelectWindow")]
// [HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindow) }, new ArgumentType[] { ArgumentType.Ref } )]
[HarmonyPatch]
public static class Test
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony instance) {
        Type type = AccessTools.TypeByName("CScenarioScript");

        return AccessTools.Method(type, "CallCmdBlockCommonSelectWindow");
    }

    public static void Postfix(ParameterCommonSelectWindow _param, dynamic __instance) {
        Plugin.Logger.LogInfo($"CScenarioScript::CallCmdBlockCommonSelectWindow");
        Plugin.Logger.LogInfo($"__instance #{__instance}");
        Plugin.Logger.LogInfo($"_param #{_param}");
        
        dynamic item = Traverse.Create(_param).Property("m_select_item1").GetValue();
        Plugin.Logger.LogInfo($"{Language.GetString(item)} [{item}]");
    }
}