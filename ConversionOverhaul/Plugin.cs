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
//   - Figure out Gostumon and Tyranmon window_type for Sundays
//   - Tweak Guardrmon exchange so that it works like the Gutsumon and Tyranmon (change panel item text to list ingredients)

namespace ConversionOverhaul;

public class SelectedItem
{
    public SelectedItem(uint id, ItemType type) {
        this.item_id = id;
        switch(type) {
            case ItemType.Material: case ItemType.Item:
                this.item_type = type;
                break;
            default:
                throw new Exception($"Unknown type for SelectedItem (type = {type})");
        }
    }

    public uint item_id;
    public ItemType item_type;
    public int num_exchanges = 1;
    public string item_name { get { return Language.GetString(this.item_id); } }
    public int item_count { get { 
        if (this.item_type == ItemType.Material)
            return Plugin.town_materials[item_name];
        if (this.item_type == ItemType.Item)
            return Plugin.player_items[item_name];

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
    public static SelectedItem selected_item;
    public static MethodInfo original_CallCmdBlockCommonSelectWindow;

    public static MethodInfo Get_CallCmdBlockCommonSelectWindow_MethodInfo()
    {
        Plugin.CScenarioScript = AccessTools.TypeByName("CScenarioScript");

        return AccessTools.Method(Plugin.CScenarioScript, "CallCmdBlockCommonSelectWindow");
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
        Plugin.original_CallCmdBlockCommonSelectWindow = harmony.Patch(Get_CallCmdBlockCommonSelectWindow_MethodInfo());
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

// [HarmonyPatch(typeof(ParameterCommonSelectWindowMode), "GetParam")]
// [HarmonyPatch(new Type[] { typeof(ParameterCommonSelectWindowMode.WindowType) })]
// public static class Patch_ParameterCommonSelectWindowMode_GetParam
// {
//     public static void Postfix(ParameterCommonSelectWindowMode.WindowType window_type, ref dynamic __result) {
//         Plugin.Logger.LogInfo($"ParameterCommonSelectWindowMode::GetParam");
//     }
// }

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
            int amount = 1;
            if (PadManager.IsTrigger(PadManager.BUTTON.bSquare)) {
                Plugin.selected_item.num_exchanges = Plugin.selected_item.item_count / 5;
            }
            if (PadManager.IsRepeat(PadManager.BUTTON.dLeft)) {
            Plugin.selected_item.num_exchanges -= amount;
            }
            if (PadManager.IsRepeat(PadManager.BUTTON.dRight)) {
                Plugin.selected_item.num_exchanges += amount;
            }
            if (Plugin.selected_item.num_exchanges > Plugin.selected_item.item_count / 5)
                Plugin.selected_item.num_exchanges = Plugin.selected_item.item_count / 5;
            if (Plugin.selected_item.num_exchanges < 1)
                Plugin.selected_item.num_exchanges = 1;
            SetCaptionText(captionPanel, "OK", $"Exchange x{Plugin.selected_item.num_exchanges}");
        }

        uItemBase panel_item = (uItemBase)Traverse.Create(__instance).Property("m_itemPanel").GetValue();
        int selected_option = (int)Traverse.Create(panel_item).Property("m_selectNo").GetValue();
        Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow> window_list = (Il2CppSystem.Collections.Generic.List<ParameterCommonSelectWindow>)Traverse.Create(__instance).Property("m_paramCommonSelectWindowList").GetValue();
        ParameterCommonSelectWindow window = window_list[selected_option];
        dynamic item_id = Traverse.Create(window).Property("m_select_item1").GetValue();

        if (Plugin.selected_item != null && Plugin.selected_item.item_id == item_id)
            return;

        SelectedItem.ItemType item_type = Plugin.town_material_window_types.Contains(window_type) ? SelectedItem.ItemType.Material : SelectedItem.ItemType.Item;
        SelectedItem selected_item = new SelectedItem(item_id, item_type);
        Plugin.selected_item = selected_item;

        SetCaptionText(captionPanel, "OK", $"Exchange x{Plugin.selected_item.num_exchanges}");
    }
}

[HarmonyPatch]
public static class Test
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod(Harmony instance) {
        return Plugin.Get_CallCmdBlockCommonSelectWindow_MethodInfo();
    }

    public static void Postfix(ParameterCommonSelectWindow _param, dynamic __instance) {
        for (int i = 0; i < Plugin.selected_item.num_exchanges - 1; i++) {
            Plugin.original_CallCmdBlockCommonSelectWindow.Invoke(__instance, new object[] { __instance, _param });
        }
    }
}