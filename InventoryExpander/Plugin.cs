using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Reflection;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace InventoryExpander;

// TODO: Attempt manually patching ItemStorageData constructor
//     var ctor = (MethodBase)((typeof(ItemStorageData)).GetMember(".ctor", AccessTools.all))[0];
//     harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(Patch_ItemStorageData), "Postfix"));
        
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
        Plugin.Logger.LogInfo($"ItemStorageData Methods");
        foreach (var method in typeof(ItemStorageData).GetMethods()) {
            Plugin.Logger.LogInfo(method);
        }
        Plugin.Logger.LogInfo($"ItemStorageData Properties");
        foreach (var property in typeof(ItemStorageData).GetProperties()) {
            Plugin.Logger.LogInfo(property);
        }
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

// [HarmonyPatch(typeof(ItemStorageData), "CreateInitItemList")]
// [HarmonyPatch(new Type[] { typeof(List<ItemData>), typeof(int) }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Normal })]
// class Patch_CreateInitItemList
// {
//     static void Postfix(ref List<ItemData> list, int max_capacity, ItemStorageData __instance)
//     {
//         Plugin.Logger.LogMessage("[ItemStorageData::Patch_CreateInitItemList::Postfix]");
//         Plugin.Logger.LogMessage($"max_capacity = {max_capacity}");
//         List<ItemData>[] m_itemDataListTbl = (List<ItemData>[])Traverse.Create(__instance).Property("m_itemDataListTbl").GetValue();

//         Plugin.Logger.LogMessage($"list.Capacity = {list.Capacity}");
//         foreach(var _list in m_itemDataListTbl) {
//             Plugin.Logger.LogMessage($"list.Capacity = {_list.Capacity}");
//         }
//     }
// }

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