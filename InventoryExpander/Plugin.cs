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

namespace InventoryExpander;

// TODO: Fix materials over 200 not saving to save file
//  - Not sure why but in ItemStorageData::WriteSaveData_ItemData the function iterates over the inventory ItemData lists and only for index 2 (i.e. MATERIAL) does it impose a limit to the loop besides the list.Count
//  - As crazy of an idea as it would be, try temporarily inserting an empty list into position 2 of m_itemDataListTbl to bypass this limit. The function should skip the empty list and save the materials at position 3
//      using list.Count
//  - Having trouble padding with nulls since the data type is a variant of an array containing a variant of List. If I can rangle the object types, I think the process has a chance of working.

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

        dynamic method = AccessTools.Method(typeof(ItemStorageData), "WriteSaveData");
        dynamic prefix = AccessTools.Method(typeof(Patch_WriteSaveData_ItemData), "Prefix");
        dynamic postfix = AccessTools.Method(typeof(Patch_WriteSaveData_ItemData), "Postfix");
        Plugin.Logger.LogMessage($"method = {method}");
        Plugin.Logger.LogMessage($"prefix = {prefix}");
        Plugin.Logger.LogMessage($"postfix = {postfix}");

        // harmony.Patch(method, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
        harmony.Patch(method, prefix: new HarmonyMethod(prefix));

        harmony.PatchAll();
    }
}

class Patch_WriteSaveData_ItemData
{
    static bool Prefix(ItemStorageData __instance)
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_WriteSaveData_ItemData::Prefix]");
        
        dynamic itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();

        var a = new List<ItemData>[7];
        a[0] = null;
        a[1] = null;
        a[2] = null;
        a[3] = itemDataListTbl[0];
        a[4] = itemDataListTbl[1];
        a[5] = itemDataListTbl[2];
        a[6] = itemDataListTbl[3];

        var b = new Il2CppReferenceArray<List<ItemData>>(a);

        Type arrayType = itemDataListTbl.GetType();
        Type listType = itemDataListTbl[0].GetType();
        Plugin.Logger.LogMessage($"arrayType = {arrayType}");
        foreach (var method in arrayType.GetMethods()) {
            Plugin.Logger.LogMessage($"{method}");
        }
        Plugin.Logger.LogMessage($"listType = {listType}");
        foreach (var method in listType.GetMethods()) {
            Plugin.Logger.LogMessage($"{method}");
        }

        for(int i = 0; i < 3; i++) {
            itemDataListTbl.Insert(0, null);
        }
        return true;
    }

    static void Postfix(ref dynamic _writer, ref uint __result, ItemStorageData __instance)
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_WriteSaveData_ItemData::Postfix]");
        __result = 1;
        long position = 0;

        if (_writer != null)
        {
            dynamic baseStream = Traverse.Create(_writer).Property("BaseStream").GetValue();
            position = baseStream.Position;
            Plugin.Logger.LogMessage($"position = {position}");
        }
        else 
        {
            __result = 0U;
        }
        
        // dynamic itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        // if (__result != 0U && itemDataListTbl != null)
        // {
        //     for (int i = 0; i < itemDataListTbl.Count; i++)
        //     {
        //         if (itemDataListTbl[i] != null)
        //         {
        //             for (int j = 0; j < itemDataListTbl[i].Count; j++)
        //             {
        //                 dynamic itemData = itemDataListTbl[i][j];
        //                 if (itemData != null)
        //                 {
        //                     itemData.WriteSaveData(_writer);
        //                 }
        //             }
        //         }
        //     }
        // }
        // else
        // {
        //     __result = 0U;
        // }

        dynamic shopItemData = Traverse.Create(__instance).Property("m_shopItemData").GetValue();
        Plugin.Logger.LogMessage($"shopItemData.GetType() = {shopItemData.GetType()}");

        if (__result != 0U && shopItemData != null && shopItemData.WriteSaveData(__instance, ref _writer))
        {
            Plugin.Logger.LogMessage($"Attempting to save shop item data");
            dynamic baseStream = Traverse.Create(_writer).Property("BaseStream").GetValue();
            __result = (uint)(baseStream.Position - position);
        }
        else
        {
            __result = 0U;
        }

        Plugin.Logger.LogMessage($"__result = {__result}");

        // // if (__instance.m_itemDataListTbl == null) 
        // // {
		// // 	__result = false;
		// // }
        // // else 
        // // {
        // //     int num = __instance.m_itemDataListTbl.Length;
        // //     for (int i = 0; i < num; i++)
        // //     {
        // //         dynamic list = __instance.m_itemDataListTbl[i];
        // //         if (list != null)
        // //         {
        // //             // int num2 = (i == 2) ? 100 : list.Count;
        // //             for (int j = 0; j < list.Count; j++)
        // //             {
        // //                 ItemData itemData = list[j];
        // //                 if (itemData != null)
        // //                 {
        // //                     itemData.WriteSaveData(_writer);
        // //                 }
        // //             }
        // //         }
        // //     }
        // //     __result = true;
        // // }
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