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

        foreach (var method in typeof(ItemStorageData).GetMethods()) {
            Plugin.Logger.LogInfo(method);
        }

        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("InventoryExpander");
        harmony.PatchAll();
    }
}

// [HarmonyPatch(typeof(ItemStorageData), "GetMaxItemNum")]
// public static class GetMaxItemNum_Patch
// {
//     [HarmonyPostfix]
//     public static void PostFix(ItemStorageData.StorageType type, ref int __result) {
//         Plugin.Logger.LogMessage($"");
//         Plugin.Logger.LogMessage($"GetMaxItemNum::PostFix");
//         Plugin.Logger.LogMessage($"storage type = {type}");
//         Plugin.Logger.LogMessage($"__result Class = {__result.GetType()}");
//         Plugin.Logger.LogMessage($"__result = {__result}");
//     }
// }

[HarmonyPatch(typeof(ItemStorageData))]
[HarmonyPatch("GetTypeMaxNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Ref })]
public static class GetTypeMaxNumRef_Patch
{
    // [HarmonyPrefix]
    // public static bool PreFix(ItemStorageData.StorageType type, ref int __result) {
    //     Plugin.Logger.LogMessage("");
    //     Plugin.Logger.LogMessage($"GetTypeMaxNumRef::PreFix");
    //     return false;
    // }

    [HarmonyPostfix]
    public static void PostFix(ItemStorageData.StorageType type, ref int __result) {
        Plugin.Logger.LogMessage("");
        Plugin.Logger.LogMessage($"GetTypeMaxNumRef::PostFix");
        Plugin.Logger.LogMessage($"storage type = {type}");
        Plugin.Logger.LogMessage($"__result = {__result}");

        switch(type)
        {
            case ItemStorageData.StorageType.KEY_ITEM:
                break;
            case ItemStorageData.StorageType.MATERIAL:
                __result = (int)500;
                break;
            case ItemStorageData.StorageType.PLAYER:
                break;
            case ItemStorageData.StorageType.SHOP:
                break;
        }
    }
}

// [HarmonyPatch(typeof(ItemStorageData))]
// [HarmonyPatch("GetMaterialStorageExNum")]
// public static class SetMaxMaterialItemNum_Patch
// {
//     [HarmonyPostfix]
//     public static void PostFix(ref int __result) {
//         Plugin.Logger.LogMessage("");
//         Plugin.Logger.LogMessage($"GetMaterialStorageExNum::PostFix");
//         Plugin.Logger.LogMessage($"__result = {__result}");

//         __result = 500;
//     }
// }

// [HarmonyPatch(typeof(ItemStorageData))]
// [HarmonyPatch("GetTypeMaxNum")]
// [HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Normal })]
// public static class GetTypeMaxNum_Patch
// {
//     [HarmonyPostfix]
//     public static void PostFix(ItemStorageData.StorageType type, ref int __result) {
//         Plugin.Logger.LogMessage("");
//         Plugin.Logger.LogMessage($"GetTypeMaxNum::PostFix");
//         Plugin.Logger.LogMessage($"storage type = {type}");
//         Plugin.Logger.LogMessage($"__result = {__result}");

//         switch(type)
//         {
//             case ItemStorageData.StorageType.KEY_ITEM:
//                 break;
//             case ItemStorageData.StorageType.MATERIAL:
//                 __result = (int)500;
//                 break;
//             case ItemStorageData.StorageType.PLAYER:
//                 break;
//             case ItemStorageData.StorageType.SHOP:
//                 break;
//         }
//     }
// }

// [HarmonyPatch(typeof(ItemStorageData))]
// [HarmonyPatch("IsHaveAddFull")]
// [HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType), typeof(ItemData), typeof(ItemData) }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal })]
// public static class IsHaveAddFull_Patch
// {
//     [HarmonyPostfix]
//     public static void PostFix(ItemStorageData.StorageType type, ItemData add_item_data, ItemData check_storage_item_data, ref dynamic __result)
//     {
//         Plugin.Logger.LogMessage("");
//         Plugin.Logger.LogMessage($"IsHaveAddFull::PostFix");
//         Plugin.Logger.LogMessage($"storage type = {type}");
//         Plugin.Logger.LogMessage($"__result = {__result}");
//     }
// }

// [HarmonyPatch(typeof(ItemStorageData))]
// [HarmonyPatch("IsHaveFullRef")]
// [HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType), typeof(int) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
// public static class IsHaveFullRef_Patch
// {
//     [HarmonyPostfix]
//     public static void PostFix(ItemStorageData.StorageType type, ref int item_id, ref dynamic __result)
//     {
//         Plugin.Logger.LogMessage("");
//         Plugin.Logger.LogMessage($"IsHaveFullRef::PostFix");
//         Plugin.Logger.LogMessage($"storage type = {type}");
//         Plugin.Logger.LogMessage($"storage item_id = {item_id}");
//         Plugin.Logger.LogMessage($"__result = {__result}");
//     }
// }

// [Message:InventoryExpander] NativeMethodInfoPtr_GetTypeMaxNum_Public_Int32_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetMaterialStorageExNum_Public_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveAddFull_Public_Boolean_byref_StorageType_byref_ItemData_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SetMaterialMaxHaveNumAllDigimonPartner_Public_Void_0
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_PLAYER_ITEM_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_STORAGE_ITEM_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_MATERIAL_ITEM_NUM_GAME_DATA_VER_LOWER_THAN_2
// [Message:InventoryExpander] NativeFieldInfoPtr_EX_MATERIAL_ITEM_NUM_GAME_DATA_VER_2_OR_ABOVE
// [Message:InventoryExpander] NativeFieldInfoPtr_EX_MATERIAL_ITEM_NUM_GAME_DATA_VER
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_MATERIAL_ITEM_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_KEY_ITEM_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_m_itemTypeMaxTbl
// [Message:InventoryExpander] NativeFieldInfoPtr_STORAGE_LEVEL_PLAYER_INIT_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_STORAGE_LEVEL_PLAYER_EX_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_STORAGE_LEVEL_SHOP_INIT_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_STORAGE_LEVEL_SHOP_EX_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_STORAGE_LEVEL_MATERIAL_INIT_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_PLAYER_ONE_ITEM_HAVE_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_SHOP_ONE_ITEM_HAVE_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MATERIAL_UNIT_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_MATERIAL_ONE_ITEM_HAVE_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MAX_KEY_ITEM_ONE_ITEM_HAVE_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MATERIAL_ITEM_TYPE_HAVE_NUM
// [Message:InventoryExpander] NativeFieldInfoPtr_MATERIAL_STORAGE_EX_ROBUSTNESS_RATE
// [Message:InventoryExpander] NativeFieldInfoPtr_MATRIAL_ITEM_LIST_INVALED_INDEX
// [Message:InventoryExpander] NativeFieldInfoPtr_m_itemDataListTbl
// [Message:InventoryExpander] NativeFieldInfoPtr_m_storageLevelInitNumTbl
// [Message:InventoryExpander] NativeFieldInfoPtr_m_storageLevelExNumTbl
// [Message:InventoryExpander] NativeFieldInfoPtr_m_maxItemNumTbl
// [Message:InventoryExpander] NativeFieldInfoPtr_m_maxMaterialHaveNum
// [Message:InventoryExpander] NativeFieldInfoPtr_m_shopItemData
// [Message:InventoryExpander] NativeFieldInfoPtr_m_paramItemDataSort
// [Message:InventoryExpander] NativeMethodInfoPtr__ctor_Public_Void_0
// [Message:InventoryExpander] NativeMethodInfoPtr_CreateInitItemList_Private_Void_byref_List_1_ItemData_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetShopItemData_Public_ShopItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_InitializeItemList_Public_Void_0
// [Message:InventoryExpander] NativeMethodInfoPtr_InitializeItemListForRetryNewGame_Public_Void_0
// [Message:InventoryExpander] NativeMethodInfoPtr_InitializeItemList_Private_Void_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_InitItemDataList_Public_Void_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_InitItemDataList_Public_Void_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetActiveItemListNum_Public_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetActiveItemListNum_Public_Int32_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetEmptyNum_Public_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetEmptyNum_Public_Int32_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBit_Public_List_1_ItemData_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBitRef_Public_List_1_ItemData_byref_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBit_Public_List_1_ItemData_StorageType_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBit_Public_List_1_ItemData_StorageType_byref_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBit_Public_List_1_ItemData_byref_StorageType_byref_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBitRef_Public_List_1_ItemData_StorageType_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBitRef_Public_List_1_ItemData_StorageType_byref_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBitRef_Public_List_1_ItemData_byref_StorageType_byref_ClassIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBit_Public_List_1_ItemData_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindRef_Public_List_1_ItemData_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBit_Public_List_1_ItemData_StorageType_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBit_Public_List_1_ItemData_StorageType_byref_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBit_Public_List_1_ItemData_byref_StorageType_byref_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBitRef_Public_List_1_ItemData_StorageType_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBitRef_Public_List_1_ItemData_StorageType_byref_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBitRef_Public_List_1_ItemData_byref_StorageType_byref_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemData_Public_Void_ItemData_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemData_Public_Void_byref_ItemData_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemData_Public_Void_StorageType_ItemData_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemData_Public_Void_StorageType_byref_ItemData_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemData_Public_Void_byref_StorageType_byref_ItemData_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemList_Public_Void_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemList_Public_Void_StorageType_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetStorageItemListKindBit_Public_List_1_ItemData_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetStorageItemListKindBitRef_Public_List_1_ItemData_byref_KindIndexBit_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveStorageItemListData_Public_Void_byref_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveStorageItemList_Public_Void_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemDataToParamData_Public_ParameterItemData_byref_ItemData_Boolean_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetStorageLevel_Public_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetTypeMaxNum_Public_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetDijimonPartnerAllTotalRobustness_Public_Single_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetMaxItemNum_Public_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveEmpty_Public_Boolean_StorageType_byref_UInt32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveEmpty_Public_Boolean_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveFullStorage_Public_Boolean_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveFullStorage_Public_Boolean_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveFull_Public_Boolean_StorageType_byref_UInt32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveFull_Public_Boolean_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemData_Public_ItemData_StorageType_byref_UInt32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemDataRef_Public_ItemData_StorageType_byref_UInt32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsGetItem_Public_Boolean_StorageType_ItemData_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsGetItem_Public_Boolean_StorageType_byref_ItemData_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsGetItem_Public_Boolean_byref_StorageType_byref_ItemData_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsAllGetItemList_Public_Boolean_StorageType_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsAllGetItemList_Public_Boolean_byref_StorageType_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_InitItemDataListBase_Private_Void_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsActiveItemDataList_Private_Boolean_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsActiveItemDataList_Private_Boolean_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetActiveItemDataList_Private_List_1_ItemData_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetActiveItemDataList_Private_List_1_ItemData_byref_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListClassBitBase_Private_List_1_ItemData_byref_StorageType_byref_List_1_ItemData_byref_ClassIndexBit_Boolean_0
// [Message:InventoryExpander] NativeMethodInfoPtr_ListAdd_Private_Void_byref_List_1_ItemData_byref_ItemData_byref_Boolean_0
// [Message:InventoryExpander] NativeMethodInfoPtr_LisAddBase_Private_Void_StorageType_byref_List_1_ItemData_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_LisAddBase_Private_Void_StorageType_byref_List_1_ItemData_byref_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_LisAddBase_Private_Void_byref_StorageType_byref_List_1_ItemData_byref_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemListKindBitBase_Private_List_1_ItemData_byref_StorageType_byref_List_1_ItemData_byref_KindIndexBit_Boolean_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetSortKeyItemList_Private_List_1_ItemData_byref_List_1_ParameterItemData_byref_List_1_ItemData_byref_List_1_ItemData_byref_Boolean_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemDataBase_Private_Void_byref_StorageType_byref_List_1_ItemData_byref_ItemData_byref_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_SaveItemListBase_Private_Void_byref_StorageType_byref_List_1_ItemData_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveEmptyBase_Private_Boolean_byref_StorageType_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveFullBase_Private_Boolean_byref_StorageType_byref_UInt32_0
// [Message:InventoryExpander] NativeMethodInfoPtr_IsHaveFullBase_Private_Boolean_byref_StorageType_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_GetItemDataBase_Private_ItemData_byref_StorageType_byref_List_1_ItemData_byref_UInt32_Boolean_0
// [Message:InventoryExpander] NativeMethodInfoPtr_AllMaterialConvergent_Public_Void_0
// [Message:InventoryExpander] NativeMethodInfoPtr_MaterialConvergent_Private_Boolean_byref_ItemData_byref_List_1_ItemData_0
// [Message:InventoryExpander] NativeMethodInfoPtr_MaterialExpansion_Public_Void_0
// [Message:InventoryExpander] NativeMethodInfoPtr_AddItemPlayer_Public_Void_UInt32_Int32_0
// [Message:InventoryExpander] NativeMethodInfoPtr__AddMaterialOrKeyItem_Private_Void_ItemData_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr__RemoveMaterialOrKeyItem_Private_Void_ItemData_Int32_StorageType_0
// [Message:InventoryExpander] NativeMethodInfoPtr_ReadSaveData_Public_Virtual_Final_New_Boolean_BinaryReader_0
// [Message:InventoryExpander] NativeMethodInfoPtr_ReadSaveDataUpdate002_Public_Virtual_Final_New_Boolean_BinaryReader_0
// [Message:InventoryExpander] NativeMethodInfoPtr_WriteSaveData_Public_Virtual_Final_New_UInt32_BinaryWriter_0
// [Message:InventoryExpander] NativeMethodInfoPtr_WriteSaveDataUpdate002_Public_Virtual_Final_New_UInt32_BinaryWriter_0
// [Message:InventoryExpander] NativeMethodInfoPtr_ReadSaveData_ItemData_Private_Boolean_byref_BinaryReader_0
// [Message:InventoryExpander] NativeMethodInfoPtr_ReadSaveData_ExMaterialItem_Private_Boolean_BinaryReader_0
// [Message:InventoryExpander] NativeMethodInfoPtr_WriteSaveData_ItemData_Private_Boolean_byref_BinaryWriter_0
// [Message:InventoryExpander] NativeMethodInfoPtr_WriteSaveData_ExMaterialItem_Private_Boolean_BinaryWriter_0
// [Message:InventoryExpander] NativeMethodInfoPtr_ReadSaveData_ShopItemData_Private_Boolean_byref_BinaryReader_0
// [Message:InventoryExpander] NativeMethodInfoPtr_WriteSaveData_ShopItemData_Private_Boolean_byref_BinaryWriter_0

// [HarmonyPatch(typeof(ItemStorageData))]
// [HarmonyPatch("SetMaterialMaxHaveNumAllDigimonPartner")]
// public static class SetMaterialMaxHaveNumAllDigimonPartner_Patch
// {
//     [HarmonyPrefix]
//     public static bool PreFix(ItemStorageData __instance)
//     {
//         Plugin.Logger.LogMessage("");
//         Plugin.Logger.LogMessage($"SetMaterialMaxHaveNumAllDigimonPartner::PostFix");
//         Plugin.Logger.LogMessage($"__instance = {__instance}");

//         var x = Traverse.Create(__instance).Field("NativeFieldInfoPtr_m_maxMaterialHaveNum").GetValue();
//         Plugin.Logger.LogMessage($"x = {x}");

//         unsafe
//         {
//             int m_maxMaterialHaveNum = *x;
//             Plugin.Logger.LogMessage($"m_maxMaterialHaveNum = {m_maxMaterialHaveNum}");
//         }



//         // foreach (var field in Traverse.Create(__instance).Fields()) {
//         //     Plugin.Logger.LogMessage(field);
//         // }

//         return false;
//     }
// }

[HarmonyPatch(typeof(ItemStorageData))]
[HarmonyPatch("GetEmptyNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Normal })]
public static class GetEmptyNum_Patch
{
    [HarmonyPostfix]
    public static void PostFix(ItemStorageData.StorageType type, ref dynamic __result)
    {
        Plugin.Logger.LogMessage("");
        Plugin.Logger.LogMessage($"GetEmptyNum::PostFix");
        Plugin.Logger.LogMessage($"storage type = {type}");
        Plugin.Logger.LogMessage($"__result = {__result}");
    }
}

[HarmonyPatch(typeof(ItemStorageData))]
[HarmonyPatch("GetEmptyNum")]
[HarmonyPatch(new Type[] { typeof(ItemStorageData.StorageType) }, new ArgumentType[] { ArgumentType.Ref })]
public static class GetEmptyNumRef_Patch
{
    [HarmonyPostfix]
    public static void PostFix(ItemStorageData.StorageType type, ref dynamic __result)
    {
        Plugin.Logger.LogMessage("");
        Plugin.Logger.LogMessage($"GetEmptyNumRef::PostFix");
        Plugin.Logger.LogMessage($"storage type = {type}");
        Plugin.Logger.LogMessage($"__result = {__result}");
    }
}