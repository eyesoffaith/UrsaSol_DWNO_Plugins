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


// TODO:
//  - Tweak Guardrmon exchange so that it works like the Gutsumon and Tyranmon (change panel item text to list ingredients)
//      - Need to find what function triggers to give the item to the player (after dialog) so I can shortcut it
//  - Include thumbstick for +/- num_exchange (currently on DPad and arrow keys)
//  - Include keyboard equivalent of gamepad Square for maxing num_exchange

// [C024]
// MaterialChange01 = Gostumon (metal)
// MaterialChange02 = Gostumon (stone)
// TreasureMaterial = Gostumon (special)

// [D021]
// MaterialChange03 = Tyrannomon (wood)
// AdventureInfo = Tyrannomon (liquid)
// MaterialChange04 = Tyrannomon (special)

// [D034]
// window_type _03 = Gaurdromon (lab item creation)
//  - Since there's an in-between dialog option, cut this out and replace the confirmation (are you sure?) with a confirmation (exchange complete)
//  - Front-load item requirements to the menu by red-lining the options and disabling them if required item's aren't present

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

        Plugin.Logger.LogInfo("Shit");

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
    public static List<ParameterCommonSelectWindowMode.WindowType> windows_we_care_about = Plugin.town_material_window_types.Concat(Plugin.player_inventory_window_types).ToList();
    public static Dictionary<string, int> town_materials = new Dictionary<string, int>();
    public static Dictionary<string, int> player_items = new Dictionary<string, int>();
    public static SelectedItem selected_item;
    public static MethodInfo original_CallCmdBlockCommonSelectWindow;
    public static Dictionary<string, (string, int)[]> lab_recipes = new Dictionary<string, (string name, int count)[]>() {
        { "Double Disk", new [] { ("MP Disk", 2), ("Recovery Disk", 2) } },
        { "Large Double Disk", new [] { ("Medium MP Disk", 3), ("Medium Recovery Disk", 3) } },
        { "Super Double Disk", new [] { ("Large MP Disk", 2), ("Large Recovery Disk", 2) } },
        { "Large Recovery Disk", new [] { ("Medium Recovery Disk", 5) } },
        { "Large MP Disk", new [] { ("Medium MP Disk", 5) } },
        { "Full Remedy Disk", new [] { ("Remedy Disk", 10) } },
        { "Super Regen Disk", new [] { ("Regen Disk", 10) } },
        { "Medicine", new [] { ("Bandage", 5), ("Recovery Disk", 3) } }
    };

    public static MethodInfo GetOriginalMethod(string className, string methodName)
    {
        return AccessTools.Method(AccessTools.TypeByName(className), methodName);
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
        Plugin.original_CallCmdBlockCommonSelectWindow = harmony.Patch(GetOriginalMethod("CScenarioScript", "CallCmdBlockCommonSelectWindow"));
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(uCommonSelectWindowPanel), "Setup")]
[HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) }, new ArgumentType[] { ArgumentType.Ref } )]
public static class Patch_uCommonSelectWindowPanel_Setup
{
    public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, uCommonSelectWindowPanel __instance) {
        Plugin.selected_item = null;

        if (Plugin.town_material_window_types.Contains(window_type)) {
            TownMaterialDataAccess m_materialData = (TownMaterialDataAccess)Traverse.Create(typeof(StorageData)).Property("m_materialData").GetValue();
            dynamic materialList = Traverse.Create(m_materialData).Property("m_materialDatas").GetValue();
            
            Plugin.town_materials.Clear();
            foreach (var material in materialList) {
                string key = Language.GetString(material.m_id);
                if (!string.IsNullOrEmpty(key))
                    Plugin.town_materials[key] = material.m_material_num;
            }
        }

        if (Plugin.player_inventory_window_types.Contains(window_type)) {
            ItemStorageData m_ItemStorageData = (ItemStorageData)Traverse.Create(typeof(StorageData)).Property("m_ItemStorageData").GetValue();
            dynamic m_itemDataListTbl = Traverse.Create(m_ItemStorageData).Property("m_itemDataListTbl").GetValue();
            
            Plugin.player_items.Clear();
            foreach (var item in m_itemDataListTbl[(int)ItemStorageData.StorageType.PLAYER].ToArray()) {
                string key = Language.GetString(item.m_itemID);
                if (!string.IsNullOrEmpty(key))
                    Plugin.player_items[key] = item.m_itemNum;
            }
        }

        // dynamic windowModeTbl = Traverse.Create(__instance).Property("m_uCommonSelectWindowModeTbl").GetValue();
        // foreach (uCommonSelectWindowPanelMode panelMode in windowModeTbl) {
        //     ((Text)Traverse.Create(panelMode).Property("m_titleText").GetValue()).text = "Shit";
        //     ((Text)Traverse.Create(panelMode).Property("m_valueText").GetValue()).text = "Shit2";
        // }

        // Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow> window_list = (Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow>)Traverse.Create(__instance).Property("m_paramCommonSelectWindowList").GetValue();
    }
}

[HarmonyPatch(typeof(uCommonSelectWindowPanel), "Update")]
public static class Patch_uCommonSelectWindowPanel_Update
{
    public static void SetCaptionText(uCommonSelectWindowPanelCaption captionPanel, string replace, string with) {
        Text captionText = (Text)Traverse.Create(captionPanel).Property("m_text").GetValue();
        UtilityScript.SetLangButtonText(ref captionText, "cw_caption_0");
        captionText.text = captionText.text.Replace(replace, with);
    }

    public static void Postfix(uCommonSelectWindowPanel __instance) {
        ParameterCommonSelectWindowMode.WindowType window_type = (ParameterCommonSelectWindowMode.WindowType)Traverse.Create(__instance).Property("m_windowType").GetValue();
        if (!Plugin.windows_we_care_about.Contains(window_type))
            return;
        uCommonSelectWindowPanelCaption captionPanel = (uCommonSelectWindowPanelCaption)Traverse.Create(__instance).Property("m_captionPanel").GetValue();

        if (Plugin.selected_item != null) {
            int num_exchanges = Plugin.selected_item.num_exchanges;
            if (PadManager.IsTrigger(PadManager.BUTTON.bSquare))
                num_exchanges = Plugin.selected_item.item_count / 5;
            if (PadManager.IsRepeat(PadManager.BUTTON.dLeft)) 
                num_exchanges--;
            if (PadManager.IsRepeat(PadManager.BUTTON.dRight))
                num_exchanges++;
            if (num_exchanges > Plugin.selected_item.item_count / 5)
                num_exchanges = Plugin.selected_item.item_count / 5;
            if (num_exchanges < 1)
                num_exchanges = 1;
            if (num_exchanges != Plugin.selected_item.num_exchanges)
                CriSoundManager.PlayCommonSe("S_005");
            SetCaptionText(captionPanel, "OK", $"Exchange x{num_exchanges}");
            Plugin.selected_item.num_exchanges = num_exchanges;
        }

        uItemBase panel_item = (uItemBase)Traverse.Create(__instance).Property("m_itemPanel").GetValue();

        int selected_option = (int)Traverse.Create(panel_item).Property("m_selectNo").GetValue();
        // Weird special-case during Sunday trades, the window_list has an extra element in the middle of the list throwing off the index
        // Temp-solution: +1 to "selected_option" so we can pull the correct item
        if (Plugin.sunday_trade_window_types.Contains(window_type) && selected_option > 2)
            selected_option += 1;

        Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow> window_list = (Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow>)Traverse.Create(__instance).Property("m_paramCommonSelectWindowList").GetValue();
        ParameterCommonSelectWindow window = window_list[selected_option];
        string blockID = (string)Traverse.Create(window).Property("m_scriptCommand").GetValue();
        Plugin.Logger.LogInfo($"blockID {blockID}");
        
        if (Plugin.player_inventory_window_types.Contains(window_type))
            return;

        dynamic item_id = Traverse.Create(window).Property("m_select_item1").GetValue();
        if (Plugin.selected_item != null && Plugin.selected_item.item_id == item_id)
            return;

        SelectedItem.ItemType item_type = Plugin.town_material_window_types.Contains(window_type) ? SelectedItem.ItemType.Material : SelectedItem.ItemType.Item;
        SelectedItem selected_item = new SelectedItem(item_type, item_id);
        Plugin.selected_item = selected_item;
        SetCaptionText(captionPanel, "OK", $"Exchange x{Plugin.selected_item.num_exchanges}");
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
        for (int i = 0; i < Plugin.selected_item.num_exchanges - 1; i++) {
            Plugin.original_CallCmdBlockCommonSelectWindow.Invoke(__instance, new object[] { __instance, _param });
        }
    }
}

[HarmonyPatch]
public static class Patch_CScenarioScriptBase_CallCsvbBlock
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony instance) {
        return Plugin.GetOriginalMethod("CScenarioScriptBase", "CallCsvbBlock");
    }

    public static void Postfix(string _csvbId, string _blockId, dynamic __instance) {
        Plugin.Logger.LogInfo($"Patch_CScenarioScriptBase_CallCsvbBlock");
        Plugin.Logger.LogInfo($"__instance {__instance}");
        Plugin.Logger.LogInfo($"_csvbId {_csvbId}");
        Plugin.Logger.LogInfo($"_blockId {_blockId}");
    }
}