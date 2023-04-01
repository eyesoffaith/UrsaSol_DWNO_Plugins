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
using UnityEngine.UI;

namespace OneHitResource;

// TODO: Current implementation returns a max of one digimon card since digimon card acquisition is handled in a different function
//   - uItemPickPanel::PickMaterial checks percent chance (10%) and sets the card ID the player gets
//   - test if a Postfix can set multiple digimon cards as "acquired" and if that gets properly added to player inventory
//   - try a Postfix on uItemPickPanel::PickMaterial to re-run card chance reflecting the 10% over the requestItemPickCount
//      - i.e. 1-(1-[CARD_CHANCE])^REQUEST_ITEM_PICK_COUNT

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
        Harmony harmony = new Harmony("OneHitResource");
        var _original = AccessTools.Method(typeof(ItemPickPointTimeRebirth), "PickItem");
        original = harmony.Patch(_original);
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ItemPickPointTimeRebirth), "PickItem")]
public static class Patch_PickItem
{
    public static int remainderPickCount = 0;
    public static bool Prefix(ItemPickPointTimeRebirth __instance, int requestPickCount, float rarityRevision) {
        remainderPickCount = (int)__instance.remainderPickCount;
        return false;
    }

    public static void Postfix(int requestPickCount, float rarityRevision, ref dynamic __result, ItemPickPointTimeRebirth __instance) {
        List<UInt32> materials = new List<UInt32>();
        for (int i = 0; i < Patch_PickItem.remainderPickCount; i++) {
            dynamic part = Plugin.original.Invoke(__instance, new object[] { __instance, requestPickCount, rarityRevision });
            foreach (var item in part) {
                materials.Add(item);
            }
        }
        __result = new Il2CppStructArray<UInt32>(materials.ToArray());
    }
}

// Undo card acquisition and do it ourselves
[HarmonyPatch(typeof(uItemPickPanel), "PickMaterial")]
public static class Patch_PickMaterial
{
    public static List<int> acquiredDigimonCards = new List<int>();
    public static int cardProbability = 100;
    public static void Postfix(uItemPickPanel __instance)
    {
        int m_digimonCardNumber = (int)Traverse.Create(__instance).Property("m_digimonCardNumber").GetValue();
        if (m_digimonCardNumber > 0) {
            StorageData.m_digimonCardFlag.SetFlag((uint)m_digimonCardNumber, false);
        }

        dynamic params2 = Traverse.Create(AppMainScript.parameterManager.digimonCardData).Property("m_params").GetValue();
        List<int> list = new List<int>();
        foreach (ParameterDigimonCardData parameterDigimonCardData in params2)
        {
            int cardNum = (int)Traverse.Create(parameterDigimonCardData).Property("m_number").GetValue();
            if (!StorageData.m_digimonCardFlag.IsGetFlag((uint)cardNum))
            {
                // Plugin.Logger.LogMessage($"Card #{cardNum} is available");
                list.Add(cardNum);
            }
        }

        if (!StorageData.m_digimonCardFlag.IsAllGetFlag()) {
            for (int i = 0; i < Patch_PickItem.remainderPickCount; i++) {
                if (UnityEngine.Random.Range(0, 100) <= cardProbability) {
                    if (list.Count > 0)
                    {
                        int index = UnityEngine.Random.Range(0, list.Count);
                        int cardNum = list[index];
                        if (StorageData.m_digimonCardFlag.SetFlag((uint)cardNum, true)) {
                            // Plugin.Logger.LogMessage($"Adding card #{cardNum}");
                            acquiredDigimonCards.Add(cardNum);
                            list.RemoveAt(index);
                        }
                        else {
                            Traverse.Create(__instance).Property("m_digimonCardNumber").SetValue(0);
                        }
                    }
                }
            }
        }
    }
}

// Add custom card message to result screen
[HarmonyPatch(typeof(uItemPickPanelResult), "enablePanel")]
public static class Patch_enablePanel
{
    public static void Postfix(int digimonCardNumber, uItemPickPanelResult __instance)
    {
        var messageWindow = MainGameManager.Ref.MessageManager.GetCenter();
        string message = ((Text)Traverse.Create(messageWindow).Property("m_label").GetValue()).text;

        foreach (var cardNum in Patch_PickMaterial.acquiredDigimonCards) {
            // ParameterDigimonCardData param2 = ParameterDigimonCardData.GetParam((int)cardNum);
            // int m_number = (int)Traverse.Create(param2).Property("m_number").GetValue();
            message += string.Format("\n" + Language.GetString("item_pick_message_6"), cardNum.ToString("D3"));
        }
        Patch_PickMaterial.acquiredDigimonCards.Clear();
        messageWindow.SetMessage(message, uCommonMessageWindow.Pos.Center);
    }
}