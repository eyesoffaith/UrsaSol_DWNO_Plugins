using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace InventoryExpander;

// TODO: Attempt to call ItemStorageData::SaveItemListBase from Patch_PickMaterial::Postfix
        
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static StorageData StorageData;
    
    public override void Load()
    {
        Plugin.Logger = base.Log;
        Plugin.StorageData = new StorageData();

        HarmonyFileLog.Enabled = true;
        Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // // Plugin startup logic
        // Plugin.Logger.LogInfo($"ItemStorageData Methods");
        // foreach (var method in typeof(ItemStorageData).GetMethods()) {
        //     Plugin.Logger.LogInfo(method);
        // }
        // Plugin.Logger.LogInfo($"ItemStorageData Properties");
        // foreach (var property in typeof(ItemStorageData).GetProperties()) {
        //     Plugin.Logger.LogInfo(property);
        // }

        // Plugin.Logger.LogInfo($"StorageData Methods");
        // foreach (var method in typeof(StorageData).GetMethods()) {
        //     Plugin.Logger.LogInfo(method);
        // }
        // Plugin.Logger.LogInfo($"StorageData Properties");
        // foreach (var property in typeof(StorageData).GetProperties()) {
        //     Plugin.Logger.LogInfo(property);
        // }

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
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_InitializeItemList::Prefix]");
        int[] newTypeMax = {60, 500, 600, 20};
        Traverse.Create(__instance).Property("m_itemTypeMaxTbl").SetValue(new Il2CppStructArray<int>(newTypeMax));

        dynamic m_itemDataListTbl = Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();
        for (int i = 0; i < m_itemDataListTbl.Count; i++) {
            int oldCap = m_itemDataListTbl[i].Capacity;
            m_itemDataListTbl[i].Capacity = newTypeMax[i];

            if (newTypeMax[i] > oldCap) {
                for (i = 0; i < newTypeMax[i] - oldCap; i++) {
                    m_itemDataListTbl[i].Add(new ItemData());
                }
            }

            Plugin.Logger.LogMessage($"m_itemDataListTbl[i].Capacity = {m_itemDataListTbl[i].Capacity}");
        }
    }
}
[HarmonyPatch(typeof(ItemStorageData), "AddItemPlayer")]
public static class Patch_AddItemPlayer
{
    public static bool Prefix() 
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_AddItemPlayer::Prefix]");

        return true;
    }

    public static void Postfix() 
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_AddItemPlayer::Postfix]");
    }
}

[HarmonyPatch(typeof(uItemPickPanel), "PickMaterial")]
public static class Patch_PickMaterial
{
    public static bool Prefix() 
    {
        Plugin.Logger.LogMessage("[uItemPickPanel::Patch_PickMaterial::Prefix]");

        return true;
    }

    public static void Postfix() 
    {
        Plugin.Logger.LogMessage("[uItemPickPanel::Patch_PickMaterial::Postfix]");

        dynamic m_itemPickPoint = ItemPickPointManager.Ref.PickingPoint;
        dynamic m_itemIds = m_itemPickPoint.PickItem(10, 1.2f);
        dynamic m_ItemStorageData = Traverse.Create(Plugin.StorageData).Property("m_ItemStorageData").GetValue();
        dynamic m_itemList = m_ItemStorageData.GetItemListKindBit(ItemStorageData.StorageType.MATERIAL, ParameterItemData.KindIndexBit.KindMaterialBit);
        
        Plugin.Logger.LogMessage($"m_itemList.Count = {m_itemList.Count}");

        for (int k = 0; k < m_itemIds.Length; k++)
        {
            ItemData item = new ItemData(m_itemIds[k], 1);
            m_itemList.Add(item);
        }
        Plugin.Logger.LogMessage($"m_itemList.Count = {m_itemList.Count}");

        Plugin.Logger.LogMessage($"{ItemStorageData.StorageType.MATERIAL}");
        Plugin.Logger.LogMessage($"m_itemList.GetType() = {m_itemList.GetType()}");

        m_ItemStorageData.SaveItemList(ItemStorageData.StorageType.MATERIAL, ref m_itemList);

        Plugin.Logger.LogMessage($"m_itemList.Count = {m_itemList.Count}");
    }
}

[HarmonyPatch(typeof(ItemStorageData))]
[HarmonyPatch("GetTypeMaxNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Ref })]
public static class Patch_GetTypeMaxNumRef
{
    public static void Postfix(ItemStorageData.StorageType type, ref int __result, ItemStorageData __instance) 
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_GetTypeMaxNumRef::Postfix]");
        Plugin.Logger.LogMessage($"type = {type}");

        if (type == ItemStorageData.StorageType.MATERIAL) {
            System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            for (int i = 0; i < t.FrameCount; i++) {
                StackFrame sf = t.GetFrame(i);
                Plugin.Logger.LogMessage($"{sf.GetMethod()}@{sf.GetFileLineNumber()}");
            }
        }

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