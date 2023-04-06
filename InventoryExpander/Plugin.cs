using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace InventoryExpander;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static ConfigEntry<int> materialInventorySize;
    
    public override void Load()
    {
        Plugin.Logger = base.Log;
        Plugin.materialInventorySize = base.Config.Bind<int>("Material Inventory Size", "size", 200, "The size of the player's inventory for material items.");

        HarmonyFileLog.Enabled = true;
        Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("InventoryExpander");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ItemStorageData), "InitializeItemList")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) })]
class Patch_InitializeItemList
{
    static void Postfix(ItemStorageData __instance)
    {
        // Plugin.Logger.LogMessage("[ItemStorageData::Patch_InitializeItemList::Prefix]");
        int type = (int)ItemStorageData.StorageType.MATERIAL;
        int newCapacity = Plugin.materialInventorySize.Value;

        dynamic itemTypeMaxTbl = Traverse.Create(__instance).Property("m_itemTypeMaxTbl").GetValue();
        itemTypeMaxTbl[type] = newCapacity;
        Traverse.Create(__instance).Property("m_itemTypeMaxTbl").SetValue(new Il2CppStructArray<int>(itemTypeMaxTbl));

        dynamic m_itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        int oldCapacity = m_itemDataListTbl[type].Capacity;
        m_itemDataListTbl[type].Capacity = newCapacity;

        if (newCapacity > oldCapacity) {
            for (int i = 0; i < newCapacity - oldCapacity; i++) {
                m_itemDataListTbl[type].Add(new ItemData());
            }
        }
    }
}

[HarmonyPatch(typeof(ItemStorageData), "GetTypeMaxNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Ref })]
public static class Patch_GetTypeMaxNumRef
{
    public static void Postfix(ItemStorageData.StorageType type, ref int __result, ItemStorageData __instance) 
    {
        // Plugin.Logger.LogMessage("[ItemStorageData::Patch_GetTypeMaxNumRef::Postfix]");
        switch(type)
        {
            case ItemStorageData.StorageType.KEY_ITEM:
                break;
            case ItemStorageData.StorageType.MATERIAL:
                __result = (int)Plugin.materialInventorySize.Value;
                break;
            case ItemStorageData.StorageType.PLAYER:
                break;
            case ItemStorageData.StorageType.SHOP:
                break;
        }
    }
}