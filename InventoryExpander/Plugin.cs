using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace InventoryExpander;

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

        Plugin.Logger.LogMessage($"ItemStorageData Methods");
        foreach (var method in typeof(ItemStorageData).GetMethods()) {
            Plugin.Logger.LogMessage(method);
        }
        Plugin.Logger.LogMessage($"ItemStorageData Properties");
        foreach (var property in typeof(ItemStorageData).GetProperties()) {
            Plugin.Logger.LogMessage(property);
        }

        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("InventoryExpander");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ItemStorageData), "SetMaterialMaxHaveNumAllDigimonPartner")]
class Patch_SetMaterialMaxHaveNumAllDigimonPartner
{
    static bool Prefix(ItemStorageData __instance)
    {
        Traverse.Create(__instance).Property("m_maxMaterialHaveNum").SetValue(600);
        return false;
    }
}

[HarmonyPatch(typeof(ItemStorageData))]
[HarmonyPatch("GetTypeMaxNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Ref })]
public static class Patch_GetTypeMaxNumRef
{
    public static void Postfix(ItemStorageData.StorageType type, ref int __result) {
        switch(type)
        {
            case ItemStorageData.StorageType.KEY_ITEM:
                break;
            case ItemStorageData.StorageType.MATERIAL:
                __result = (int)600;
                break;
            case ItemStorageData.StorageType.PLAYER:
                break;
            case ItemStorageData.StorageType.SHOP:
                break;
        }
    }
}