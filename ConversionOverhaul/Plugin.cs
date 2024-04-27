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
using Il2CppArrays = Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.UI;

/*
TODO:
    - What code handles disabling panel options and turning their text red in Gotsumon/Tyrannomon trades [uItemBase.SetItemContent???]
        - Ultimately handled by ParameterCommonSelectWindow.isCheckFormat. Approach to convert Guardromon:
            - set ParameterCommonSelectWindow to active 
            - set checkDataList (must pass GetCheckDataList). Can be done by setting m_select_mode, m_select_format, m_select_value1, m_select_item, m_select_digimon (set m_select_mode to 8)
            - hook PostFix on isListCheckDataSelectModeActive, check for m_selet_mode = 8, and implement a new this.isPlayerHaveItem check
    
    Look into generating new panel to the right of screen kind of like a shop
    - Tweak Guardrmon exchange so that it works like the Gutsumon and Tyranmon (change panel item text to list ingredients)
        - !UtilityScript.IsListActiveRef<ItemData>(ref this.m_itemList, ref this.m_selectNo) check this method call. Maybe useful to "disable" options
    - Include thumbstick for +/- num_exchange (currently on DPad and arrow keys)
    - Include keyboard equivalent of gamepad Square for maxing num_exchange

***PICK UP HERE***
I think all exchanges can be handled via csvbId + blockId format. Mapping them here: [csvbId = Scenario04] for all
    - Gaurdromon    D034_*
        - initial option selection is Scenario04/D034_MENU0X where confirmation is SubScenario/D034_MENUX0
    - Gotsumon      C024_*
    - Tyrannomon    D021_*

[C024]
MaterialChange01 = Gostumon (metal)
MaterialChange02 = Gostumon (stone)
TreasureMaterial = Gostumon (special)

[D021]
MaterialChange03 = Tyrannomon (wood)
AdventureInfo = Tyrannomon (liquid)
MaterialChange04 = Tyrannomon (special)

[D034]
window_type _03 = Gaurdromon (lab item creation)
 - Since there's an in-between dialog option, cut this out and replace the confirmation (are you sure?) with a confirmation (exchange complete)
 - Front-load item requirements to the menu by red-lining the options and disabling them if required item's aren't present
*/

namespace ConversionOverhaul;

public class SelectedItem
{
    public SelectedItem(ItemType type, uint id) {
        this.item_type = type;
        this.item_name = Language.GetString(id);
        this.item_id = id;
    }

    public int num_exchanges = 1;
    public ItemType item_type;
    public uint item_id;
    public string item_name;
    public int item_count { get { 
        if (this.item_type == ItemType.Material) {
            return Plugin.town_materials[item_name];
        }
        if (this.item_type == ItemType.Item) {
            return Plugin.player_items[item_name];
        }
        return 0;
    } }

    public enum ItemType
	{
        Material,
        Item
    }
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static Type CScenarioScript;

    public static ParameterCommonSelectWindowMode.WindowType[] sunday_trade_window_types = new [] {
        ParameterCommonSelectWindowMode.WindowType.MaterialChange04,
        ParameterCommonSelectWindowMode.WindowType.TreasureMaterial
    };
    public static ParameterCommonSelectWindowMode.WindowType[] town_material_window_types = new [] {
        ParameterCommonSelectWindowMode.WindowType.MaterialChange01,
        ParameterCommonSelectWindowMode.WindowType.MaterialChange02,
        ParameterCommonSelectWindowMode.WindowType.MaterialChange03,
        ParameterCommonSelectWindowMode.WindowType.AdventureInfo,
    }.Concat(sunday_trade_window_types).ToArray();
    public static ParameterCommonSelectWindowMode.WindowType[] player_inventory_window_types = new [] {
        ParameterCommonSelectWindowMode.WindowType._03
    };

    public static string[] town_material_script_types = new [] { "C024", "D021" };
    public static string[] player_inventory_script_types = new [] { "D034" };
    public static string[] script_types_we_care_about = town_material_script_types.Concat(player_inventory_script_types).ToArray();

    public static List<ParameterCommonSelectWindowMode.WindowType> windows_we_care_about = Plugin.town_material_window_types.Concat(Plugin.player_inventory_window_types).ToList();
    public static Dictionary<string, int> town_materials = new Dictionary<string, int>();
    public static Dictionary<string, int> player_items = new Dictionary<string, int>();
    public static List<(string, uint)> selected_recipe = new List<(string, uint)>();
    public static int selected_option;
    public static int num_exchanges;
    public static MethodInfo original_CallCmdBlockCommonSelectWindow;

    public static (string, (string, int)[])[] lab_recipes = new [] {
        ( "Double Disk", new [] { ("MP Disk", 2), ("Recovery Disk", 2) } ),
        ( "Large Double Disk", new [] { ("Medium MP Disk", 3), ("Medium Recovery Disk", 3) } ),
        ( "Super Double Disk", new [] { ("Large MP Disk", 2), ("Large Recovery Disk", 2) } ),
        ( "Large Recovery Disk", new [] { ("Medium Recovery Disk", 5) } ),
        ( "Large MP Disk", new [] { ("Medium MP Disk", 5) } ),
        ( "Full Remedy Disk", new [] { ("Remedy Disk", 10) } ),
        ( "Super Regen Disk", new [] { ("Regen Disk", 10) } ),
        ( "Medicine", new [] { ("Bandage", 5), ("Recovery Disk", 3) } )
    };

    public static Dictionary<string, uint> ITEM_LOOKUP = new Dictionary<string, uint>();

    public static MethodInfo GetOriginalMethod(string className, string methodName)
    {
        return AccessTools.Method(AccessTools.TypeByName(className), methodName);
    }

    public override void Load()
    {
        Plugin.Logger = base.Log;
        HarmonyFileLog.Enabled = true;

        // Plugin startup logic
        //Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Plugin.original_CallCmdBlockCommonSelectWindow = harmony.Patch(GetOriginalMethod("CScenarioScript", "CallCmdBlockCommonSelectWindow"));
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(AppMainScript), "_FinishedParameterLoad")]
public static class Patch_AppMainScript__FinishedParameterLoad
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        Plugin.ITEM_LOOKUP.Clear();
        foreach (var item in AppMainScript.Ref.m_parameters.m_csvbItemDataOther.m_params) {
            Plugin.ITEM_LOOKUP[Language.GetString(item.id)] = item.id;
            // //Plugin.Logger.LogInfo($"{Language.GetString(item.m_id)}\t{item.m_id}\t{Language.GetString(item.m_description_code)}\t{item.m_description_code}");
        }
    }
}

[HarmonyPatch(typeof(uCommonSelectWindowPanel), "Setup")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) }, new ArgumentType[] { ArgumentType.Ref } )]
public static class Patch_uCommonSelectWindowPanel_Setup
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, uCommonSelectWindowPanel __instance) {
        Plugin.selected_recipe.Clear();
        Plugin.selected_option = -1;

        if (Plugin.town_material_window_types.Contains(window_type)) {
            Plugin.town_materials.Clear();
            foreach (var material in StorageData.m_materialData.m_materialDatas) {
                string key = Language.GetString(material.m_id);
                if (!string.IsNullOrEmpty(key))
                    Plugin.town_materials[key] = material.m_material_num;
            }
        }

        if (Plugin.player_inventory_window_types.Contains(window_type)) {
            dynamic itemList = StorageData.m_ItemStorageData.m_itemDataListTbl[(int)ItemStorageData.StorageType.PLAYER].ToArray();
            
            Plugin.player_items.Clear();
            foreach (var item in itemList) {
                string key = Language.GetString(item.m_itemID);
                if (!string.IsNullOrEmpty(key))
                    Plugin.player_items[key] = item.m_itemNum;
            }
        }
    }
}

[HarmonyPatch(typeof(uCommonSelectWindowPanel), "Update")]
public static class Patch_uCommonSelectWindowPanel_Update
{
    public static void SetCaptionText(uCommonSelectWindowPanelCaption captionPanel, string replace, string with) {
        Text captionText = captionPanel.m_text;
        UtilityScript.SetLangButtonText(ref captionText, "cw_caption_0");
        captionText.text = captionText.text.Replace(replace, with);
    }

    public static void Postfix(uCommonSelectWindowPanel __instance) {
        dynamic window_type = __instance.m_windowType;
        if (!Plugin.windows_we_care_about.Contains(window_type))
            return;

        int selected_option = __instance.m_itemPanel.m_selectNo;
        // Weird special-case during Sunday trades, the window_list has an extra element in the middle of the list throwing off the index
        // Temp-solution: +1 to "selected_option" so we can pull the correct item
        if (Array.Exists(Plugin.sunday_trade_window_types, x => x == window_type) && selected_option > 2)
            selected_option += 1;

        if (Plugin.selected_recipe.Any()) {
            var itemCollection = Array.Exists(Plugin.player_inventory_window_types, x => x == window_type) ? Plugin.player_items : Plugin.town_materials;
            int max_num_exchanges = Plugin.selected_recipe.Select(x => itemCollection[(string)x.Item1] / (int)x.Item2).Min();;
            int num_exchanges = Plugin.num_exchanges;
            if (PadManager.IsTrigger(PadManager.BUTTON.bSquare))
                num_exchanges = max_num_exchanges;
            if (PadManager.IsRepeat(PadManager.BUTTON.dLeft)) 
                num_exchanges--;
            if (PadManager.IsRepeat(PadManager.BUTTON.dRight))
                num_exchanges++;
            if (num_exchanges > max_num_exchanges)
                num_exchanges = max_num_exchanges;
            if (num_exchanges < 1)
                num_exchanges = 1;
            if (num_exchanges != Plugin.num_exchanges)
                CriSoundManager.PlayCommonSe("S_005");
            Plugin.num_exchanges = num_exchanges;
            SetCaptionText(__instance.m_captionPanel, "OK", $"Exchange x{num_exchanges}");
        }

        if (selected_option == Plugin.selected_option)
            return;

        Plugin.selected_option = selected_option;
        Plugin.num_exchanges = 1;
        //Plugin.Logger.LogInfo($"selected_option {Plugin.selected_option}");

        dynamic window = __instance.m_paramCommonSelectWindowList[selected_option];
        //Plugin.Logger.LogInfo($"window {window}");
        string scriptCommand = window.m_scriptCommand;
        //Plugin.Logger.LogInfo($"scriptCommand {scriptCommand}");
        string scriptType = scriptCommand.Split("_")[0];
        //Plugin.Logger.LogInfo($"scriptType {scriptType}");
        
        Plugin.selected_recipe.Clear();
        //Plugin.Logger.LogInfo($"selected_recipe {Plugin.selected_recipe}");
        //Plugin.Logger.LogInfo($"window_type {window_type}");
        if (Array.Exists(Plugin.town_material_window_types, x => x == window_type)) {
            uint item_id = window.m_select_item1;
            //Plugin.Logger.LogInfo($"item_id {item_id}");
            Plugin.selected_recipe.Add((Language.GetString(item_id), 5));
            //Plugin.Logger.LogInfo($"selected_recipe {Plugin.selected_recipe}");
        }
        if (Array.Exists(Plugin.player_inventory_window_types, x => x == window_type)) {
            //Plugin.Logger.LogInfo($"TEST {window.m_select_item1}");
            (string, (string, int)[] Input) recipe = Plugin.lab_recipes[selected_option];
            //Plugin.Logger.LogInfo($"recipe {recipe}");
            foreach ((string name, int count) item in recipe.Input) {
                Plugin.selected_recipe.Add((item.name, (uint)item.count));
            }
            //Plugin.Logger.LogInfo($"selected_recipe {Plugin.selected_recipe}");
        }
    }
}

[HarmonyPatch]
public static class Patch_CScenarioScript_CallCmdBlockCommonSelectWindow
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony instance) {
        return Plugin.GetOriginalMethod("CScenarioScript", "CallCmdBlockCommonSelectWindow");
    }

    public static void Postfix(ParameterCommonSelectWindow _param, dynamic __instance) {
        for (int i = 0; i < Plugin.num_exchanges - 1; i++) {
            Plugin.original_CallCmdBlockCommonSelectWindow.Invoke(__instance, new object[] { __instance, _param });
        }
    }
}

[HarmonyPatch(typeof(uItemBase), "SetItemContent")]
[HarmonyPatch(new Type[] { typeof(uItemParts), typeof(ItemData), typeof(ParameterItemData) }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref } )]
public static class Patch_UtilityScript_IsListActiveRef
{
    // [HarmonyTargetMethod]
    // public static MethodInfo TargetMethod(Harmony instance) {
    //     return Plugin.GetOriginalMethod("uItemBase", "SetItemContent");
    // }

    public static void Postfix(uItemParts item, ItemData item_data, ParameterItemData param_item_data, dynamic __instance) {
        Plugin.Logger.LogInfo($"[{item_data.GetState()}] \"{item.m_name.text}\" unavailable?: {__instance.UnavailableItem(ref item_data, ref param_item_data)}");

        var param_common = item_data.m_paramCommonSelectWindowData;
        Plugin.Logger.LogInfo($"IsSelectModeActive {param_common.IsSelectModeActive()}");
        Plugin.Logger.LogInfo($"m_select_1 {param_common.m_select_mode1} {param_common.m_select_format1} {param_common.m_select_value1} {param_common.m_select_item1} {param_common.m_select_digimon1}");
        Plugin.Logger.LogInfo($"m_select_2 {param_common.m_select_mode2} {param_common.m_select_format2} {param_common.m_select_value2} {param_common.m_select_item2} {param_common.m_select_digimon2}");
        Plugin.Logger.LogInfo($"");
    }
}


// [HarmonyPatch]
// public static class Patch_CScenarioScriptBase_CallAllCsvbBlock
// {
//     [HarmonyTargetMethod]
//     public static MethodBase TargetMethod(Harmony instance) {
//         return Plugin.GetOriginalMethod("CScenarioScriptBase", "CallAllCsvbBlock");
//     }

//     public static void Postfix(string _blockId, dynamic __instance) {
//         // if (string.IsNullOrEmpty(_blockId))
//         //     return;
//         // //Plugin.Logger.LogInfo($"Patch_CScenarioScriptBase__CallCsvbBlock");
//         // //Plugin.Logger.LogInfo($"__instance {__instance}");
//         // //Plugin.Logger.LogInfo($"_blockId {_blockId}");
//     }
// }

// [HarmonyPatch]
// public static class Patch_CScenarioScriptBase_CallCsvbBlock
// {
//     [HarmonyTargetMethod]
//     public static MethodBase TargetMethod(Harmony instance) {
//         return Plugin.GetOriginalMethod("CScenarioScriptBase", "CallCsvbBlock");
//     }

//     public static void Postfix(string _csvbId, string _blockId, dynamic __instance) {
//         // for (int i = 0; i < Plugin.selected_item.num_exchanges - 1; i++) {
//         //     Plugin.original_CallCmdBlockCommonSelectWindow.Invoke(__instance, new object[] { __instance, _param });
//         // }
//         // //Plugin.Logger.LogInfo($"Patch_CScenarioScriptBase_CallCsvbBlock");
//         // //Plugin.Logger.LogInfo($"__instance {__instance}");
//         // //Plugin.Logger.LogInfo($"_csvbId {_csvbId}");
//         // //Plugin.Logger.LogInfo($"_blockId {_blockId}");
//     }
// }
