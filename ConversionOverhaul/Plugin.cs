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
//   - It looks like the m_scriptCommand from ParameterCommonSelectWindow seems to be unique per NPC window and can be used kinda like window_type

namespace ConversionOverhaul;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static Type CScenarioScript;
    public static List<ParameterCommonSelectWindowMode.WindowType> town_material_window_types = new List<ParameterCommonSelectWindowMode.WindowType> {
        ParameterCommonSelectWindowMode.WindowType.MaterialChange01,
        ParameterCommonSelectWindowMode.WindowType.MaterialChange02,
        ParameterCommonSelectWindowMode.WindowType.MaterialChange03,
        ParameterCommonSelectWindowMode.WindowType.MaterialChange04,
        ParameterCommonSelectWindowMode.WindowType.AdventureInfo
    }; 
    public static List<ParameterCommonSelectWindowMode.WindowType> player_inventory_window_types = new List<ParameterCommonSelectWindowMode.WindowType> {
        ParameterCommonSelectWindowMode.WindowType._03
    };
    public static List<ParameterCommonSelectWindowMode.WindowType> windows_we_care_about = Plugin.town_material_window_types.Concat(Plugin.player_inventory_window_types).ToList();
    public static Dictionary<string, int> town_materials = new Dictionary<string, int>();
    public static Dictionary<string, int> player_items = new Dictionary<string, int>();

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
[C024]
MaterialChange01 = Gostumon (metal)
MaterialChange02 = Gostumon (stone)
                   Gostumon (special)
[D021]
MaterialChange03 = Tyrannomon (wood)
AdventureInfo = Tyrannomon (liquid)
                Tyrannomon (special)
[D034]
window_type _03 = Gaurdromon (lab item creation)
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
        // Il2CppSystem.Collections.Generic.Dictionary<string, Csvb<Language.LanguageCSVB>> languages = (Il2CppSystem.Collections.Generic.Dictionary<string, Csvb<Language.LanguageCSVB>>)Traverse.Create(typeof(Language)).Property("m_language").GetValue();
        // foreach (string key in languages.Keys) {
        //     Plugin.Logger.LogInfo($"{key}");
        // }
        
        Plugin.Logger.LogInfo($"uCommonSelectWindowPanel::Setup");
        // Plugin.Logger.LogInfo($"window_type {window_type}");
        // Plugin.Logger.LogInfo($"__instance {__instance}");

        dynamic m_itemPanel = Traverse.Create(__instance).Property("m_itemPanel").GetValue();
        Plugin.Logger.LogInfo($"m_itemPanel {m_itemPanel}");

        if (Plugin.town_material_window_types.Contains(window_type)) {
            TownMaterialDataAccess m_materialData = (TownMaterialDataAccess)Traverse.Create(typeof(StorageData)).Property("m_materialData").GetValue();
            dynamic materialList = Traverse.Create(m_materialData).Property("m_materialDatas").GetValue();
            Plugin.Logger.LogInfo($"GOT TOWN MATERIAL ITEMS");
            Plugin.town_materials.Clear();
            foreach (var material in materialList) {
                string key = Language.GetString(material.m_id);
                if (!string.IsNullOrEmpty(key))
                    Plugin.town_materials[key] = material.m_material_num;
            }
            foreach(var item in Plugin.town_materials) {
                Plugin.Logger.LogInfo($"{item.Key} {item.Value}");    
            }
        }

        if (Plugin.player_inventory_window_types.Contains(window_type)) {
            ItemStorageData m_ItemStorageData = (ItemStorageData)Traverse.Create(typeof(StorageData)).Property("m_ItemStorageData").GetValue();
            dynamic m_itemDataListTbl = Traverse.Create(m_ItemStorageData).Property("m_itemDataListTbl").GetValue();
            Plugin.Logger.LogInfo($"GOT PLAYER ITEMS");
            Plugin.player_items.Clear();
            foreach (var item in m_itemDataListTbl[(int)ItemStorageData.StorageType.PLAYER].ToArray()) {
                string key = Language.GetString(item.m_itemID);
                if (!string.IsNullOrEmpty(key))
                    Plugin.player_items[key] = item.m_itemNum;
            }
            foreach(var item in Plugin.player_items) {
                Plugin.Logger.LogInfo($"{item.Key} {item.Value}");    
            }
        }

        Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow> window_list = (Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow>)Traverse.Create(__instance).Property("m_paramCommonSelectWindowList").GetValue();
    }
}

// [HarmonyPatch(typeof(uCommonSelectWindowPanel), "Update")]
// public static class Patch_uCommonSelectWindowPanel_Update
// {
//     public static void Postfix(uCommonSelectWindowPanel __instance) {
//         ParameterCommonSelectWindowMode.WindowType window_type = (ParameterCommonSelectWindowMode.WindowType)Traverse.Create(__instance).Property("m_windowType").GetValue();
//         if (!Plugin.windows_we_care_about.Contains(window_type))
//             return;

//         Plugin.Logger.LogInfo($"window_type {window_type}");

//         Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow> window_list = (Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow>)Traverse.Create(__instance).Property("m_paramCommonSelectWindowList").GetValue();
//         Plugin.Logger.LogInfo($"window_list.Count {window_list.Count}");
//         int i = 1;
//         foreach (var window in window_list) {
//             string scriptCommand = (string)Traverse.Create(window).Property("m_scriptCommand").GetValue();
//             Plugin.Logger.LogInfo($"scriptCommand #{i} {scriptCommand}");
//             i++;
//         }
//     }
// }

// [HarmonyPatch(AccessTools.TypeByName("CScenarioScript"), "CallCmdBlockCommonSelectWindow")]
// [HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindow) }, new ArgumentType[] { ArgumentType.Ref } )]
[HarmonyPatch]
public static class Test
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony instance) {
        Plugin.CScenarioScript = AccessTools.TypeByName("CScenarioScript");

        return AccessTools.Method(Plugin.CScenarioScript, "CallCmdBlockCommonSelectWindow");
    }

    public static void Postfix(ParameterCommonSelectWindow _param, dynamic __instance) {
        Plugin.Logger.LogInfo($"CScenarioScript::CallCmdBlockCommonSelectWindow");
        Plugin.Logger.LogInfo($"__instance #{__instance}");
        Plugin.Logger.LogInfo($"_param #{_param}");
        
        dynamic item = Traverse.Create(_param).Property("m_select_item1").GetValue();
        Plugin.Logger.LogInfo($"{Language.GetString(item)} [{item}]");

        string command = (string)Traverse.Create(_param).Property("m_scriptCommand").GetValue();
        string parameters = "";
        for (int i = 1; i < 9; i ++) {
            parameters += $"\t{(string)Traverse.Create(_param).Property($"m_scriptCommandParam{i}").GetValue()}";
        }
        Plugin.Logger.LogInfo($"command #{command}");
        Plugin.Logger.LogInfo($"parameters #{parameters}");

        // Plugin.Logger.LogInfo($"METHODS");
        // foreach (var method in _param.GetType().GetMethods()) {
        //     string parameterDescriptions = string.Join(", ", ((MethodInfo)method).GetParameters().Select(x => $"{x.ParameterType} {x.Name}").ToArray());
        //     Plugin.Logger.LogInfo($"{method.ReturnType} {method.Name} {parameterDescriptions}");    
        // }

        // Plugin.Logger.LogInfo($"PROPERTIES");
        // foreach (var property in _param.GetType().GetProperties()) {
        //     Plugin.Logger.LogInfo($"{property}");    
        // }
    }
}