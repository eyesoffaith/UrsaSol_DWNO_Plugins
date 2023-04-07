using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace InventoryExpander;

// TODO: Fix materials over 200 not saving to save file
//  - Not sure why but in ItemStorageData::WriteSaveData_ItemData the function iterates over the inventory ItemData lists and only for index 2 (i.e. MATERIAL) does it impose a limit to the loop besides the list.Count
//  - As crazy of an idea as it would be, try temporarily inserting an empty list into position 2 of m_itemDataListTbl to bypass this limit. The function should skip the empty list and save the materials at position 3
//      using list.Count

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

        harmony.Patch(method, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));

        harmony.PatchAll();
    }
}

class Patch_WriteSaveData_ItemData
{
    static bool Prefix()
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_WriteSaveData_ItemData::Prefix]");
        // return true;
        return false;
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