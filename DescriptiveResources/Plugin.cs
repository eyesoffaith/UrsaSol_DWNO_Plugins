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
        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("DescriptiveResources");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(uItemPickPanelCommand), "OpenMessageWindow")]
public static class Patch_OpenMessageWindow
{
    static readonly uint[] MATERIAL_KIND_MSG_LANG = new uint[4]
	{
		Language.makeHash("material_liquid"),
		Language.makeHash("material_metal"),
		Language.makeHash("material_stone"),
		Language.makeHash("material_wood")
	};
    
    public static void Postfix(Action callback, uItemPickPanelCommand __instance) {
        uCommonMessageWindow center = MainGameManager.Ref.MessageManager.GetCenter();
        ItemPickPointTimeRebirth itemPickPointTimeRebirth = ItemPickPointManager.Ref.GetMaterialPickPoint(ItemPickPointManager.Ref.TargetPoint.id);
        ParameterMaterialPickPointData parameterMaterialPickPointData = (ParameterMaterialPickPointData)Traverse.Create(itemPickPointTimeRebirth).Property("m_parameterMaterialPickPointData").GetValue();

        if (parameterMaterialPickPointData is not null) {
            var parameterMaterialPickPointData_id = Traverse.Create(parameterMaterialPickPointData).Property("m_id").GetValue();
            var itemLength = parameterMaterialPickPointData.GetItemDataLength();

            String message = $"{Language.GetString("item_pick_message_1")}\n";
            for (int i = itemLength-1; i >= 0; i--) {
                var itemData = parameterMaterialPickPointData.GetItemData(i);
                ParameterItemData paramItemData = ParameterItemData.GetParam(itemData.id);
                message += $"[{itemData.probability}%] {paramItemData.GetName()}\n";
            }
            message += $"{Language.GetString("item_pick_message_2")}\n";

            ParameterItemDataMaterial param2 = HashIdSearchClass<ParameterItemDataMaterial>.GetParam(AppMainScript.parameterManager.itemDataMaterial, itemPickPointTimeRebirth.itemId);
            int m_id = (int)Traverse.Create(param2).Property("m_kind").GetValue();

            message = String.Format(
                message, 
                Language.GetString(MATERIAL_KIND_MSG_LANG[m_id]), 
                itemPickPointTimeRebirth.remainderPickCount
            );

            center.SetMessage(message, uCommonMessageWindow.Pos.Center);
        }
    }
}