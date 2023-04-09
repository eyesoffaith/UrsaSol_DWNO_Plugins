using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2 = Il2CppSystem.Collections.Generic;

namespace InventoryExpander;

// TODO: Fix materials over 200 not saving to save file
//  - Not sure why but in ItemStorageData::WriteSaveData_ItemData the function iterates over the inventory ItemData lists and only for index 2 (i.e. MATERIAL) does it impose a limit to the loop besides the list.Count
//  - As crazy of an idea as it would be, try temporarily inserting an empty list into position 2 of m_itemDataListTbl to bypass this limit. The function should skip the empty list and save the materials at position 3
//      using list.Count
//  - Having trouble padding with nulls since the data type is a variant of an array containing a variant of List. If I can rangle the object types, I think the process has a chance of working.
//  - Managed to rangle object types and null pad the list to hopefully avoid the loop limit defined. However, it seems the problem still persists. Saving and Loading doesn't error out but
//      it looks like the game is confused on how to properly store and reload the data, resulting in a loaded game but inventory, digimon info, and digimail all blank

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

[HarmonyPatch(typeof(ItemStorageData), "ReadSaveData")]
class Patch_ReadSaveData
{
    public static bool Prefix(ItemStorageData __instance)
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_ReadSaveData::Prefix]");

        dynamic itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }

        var a = new Il2.List<ItemData>[] {null, null, null, null, null, null, null, null, null, null, itemDataListTbl[0], itemDataListTbl[1], itemDataListTbl[2], itemDataListTbl[3]};
        var b = new Il2CppReferenceArray<Il2.List<ItemData>>(a);
        Traverse.Create(__instance).Property("m_itemDataListTbl").SetValue(b);

        itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }

        return true;
    }

    public static void Postfix(ItemStorageData __instance)
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_ReadSaveData::Postfix]");
        dynamic itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }

        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        var a = new Il2.List<ItemData>[4] {itemDataListTbl[10], itemDataListTbl[11], itemDataListTbl[12], itemDataListTbl[13]};
        var b = new Il2CppReferenceArray<Il2.List<ItemData>>(a);
        Traverse.Create(__instance).Property("m_itemDataListTbl").SetValue(b);

        itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }
    }
}

[HarmonyPatch(typeof(ItemStorageData), "WriteSaveData")]
class Patch_WriteSaveData
{
    public static bool Prefix(ItemStorageData __instance)
    {
        Plugin.Logger.LogMessage("[ItemStorageData::WriteSaveData::Prefix]");
        
        dynamic itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }

        var a = new Il2.List<ItemData>[] {null, null, null, null, null, null, null, null, null, null, itemDataListTbl[0], itemDataListTbl[1], itemDataListTbl[2], itemDataListTbl[3]};
        var b = new Il2CppReferenceArray<Il2.List<ItemData>>(a);
        Traverse.Create(__instance).Property("m_itemDataListTbl").SetValue(b);

        itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }

        return true;
    }

    public static void Postfix(ItemStorageData __instance)
    {
        Plugin.Logger.LogMessage("[ItemStorageData::WriteSaveData::Postfix]");

        dynamic itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }

        var a = new Il2.List<ItemData>[4] {itemDataListTbl[10], itemDataListTbl[11], itemDataListTbl[12], itemDataListTbl[13]};
        var b = new Il2CppReferenceArray<Il2.List<ItemData>>(a);
        Traverse.Create(__instance).Property("m_itemDataListTbl").SetValue(b);

        itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        Plugin.Logger.LogMessage($"itemDataListTbl.Count = {itemDataListTbl.Count}");
        foreach (Il2.List<ItemData> list in itemDataListTbl) {
            if (list == null) {
                Plugin.Logger.LogMessage($"list is null");
            } else {
                Plugin.Logger.LogMessage($"list.Count = {list.Count}");
            }
        }
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