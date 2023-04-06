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

// ACCOMPLISHMENTS:
//  - Found some success by adding ItemData directly to ItemStorageData.m_itemDataListTbl. Would be nice to not have to bypass the original implementation though

// TODO: All item collection, card collection, and item storage is handled by a delegate stored in uItemPickPanel.m_itemPickCallback
//  - Create PickMaterial Postfix that overwrites uItemPickPanel.m_itemPickCallback with an equivalent implementation we can tweak OR
//  - Learn how to write a Transpiler that can successfully cut out the StorageData.m_ItemStorageData.SaveItemList in the current delegate and implement it ourselves
        
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static StorageData StorageData;
    public static Type ListType;
    
    public override void Load()
    {
        Plugin.Logger = base.Log;
        Plugin.StorageData = new StorageData();


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
        }
    }
}

[HarmonyPatch(typeof(ItemStorageData), "GetTypeMaxNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Ref })]
public static class Patch_GetTypeMaxNumRef
{
    public static void Postfix(ItemStorageData.StorageType type, ref int __result, ItemStorageData __instance) 
    {
        Plugin.Logger.LogMessage("[ItemStorageData::Patch_GetTypeMaxNumRef::Postfix]");
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