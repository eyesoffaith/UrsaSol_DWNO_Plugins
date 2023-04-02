using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyLib.Tools;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.UI;

namespace OneHitResource;

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
    public static bool Prefix() {
        return false;
    }

    public static void Postfix(int requestPickCount, float rarityRevision, ref dynamic __result, ItemPickPointTimeRebirth __instance) {
        List<UInt32> materials = new List<UInt32>();
        int remainderPickCount = __instance.remainderPickCount;
        for (int i = 0; i < remainderPickCount; i++) {
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
    public static int cardProbability = 10;
    public static List<int> acquiredDigimonCards = new List<int>();
    public static void Postfix(uItemPickPanel __instance)
    {
        int cardNum = (int)Traverse.Create(__instance).Property("m_digimonCardNumber").GetValue();
        if (cardNum > 0) {
            var isGetFlag = StorageData.m_digimonCardFlag.IsGetFlag((uint)cardNum);
            StorageData.m_digimonCardFlag.SetFlag((uint)cardNum, false);
            isGetFlag = StorageData.m_digimonCardFlag.IsGetFlag((uint)cardNum);
        }

        dynamic params2 = Traverse.Create(AppMainScript.parameterManager.digimonCardData).Property("m_params").GetValue();
        List<int> list = new List<int>();
        foreach (ParameterDigimonCardData parameterDigimonCardData in params2)
        {
            cardNum = (int)Traverse.Create(parameterDigimonCardData).Property("m_number").GetValue();
            if (!StorageData.m_digimonCardFlag.IsGetFlag((uint)cardNum))
            {
                list.Add(cardNum);
            }
        }

        int remainderPickCount = ItemPickPointManager.Ref.GetMaterialPickPoint(ItemPickPointManager.Ref.PickingPoint.id).remainderPickCount;
        if (!StorageData.m_digimonCardFlag.IsAllGetFlag()) {
            for (int i = 0; i < remainderPickCount; i++) {
                if (UnityEngine.Random.Range(0, 100) <= cardProbability) {
                    if (list.Count > 0)
                    {
                        int index = UnityEngine.Random.Range(0, list.Count);
                        cardNum = list[index];
                        if (StorageData.m_digimonCardFlag.SetFlag((uint)cardNum, true)) {
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

        Match m = Regex.Match(message, @"(?<=#)\d{3}");
        if (m.Success) {
            Plugin.Logger.LogInfo($"Removing card detected from base code {m.Value}");
            StorageData.m_digimonCardFlag.SetFlag((uint)int.Parse(m.Value), false);
            var lines = message.Split('\n').ToList();
            lines.RemoveAt(lines.Count - 1);
            message = String.Join('\n', lines);
        }
        foreach (var cardNum in Patch_PickMaterial.acquiredDigimonCards) {
            message += string.Format("\n" + Language.GetString("item_pick_message_6"), cardNum.ToString("D3"));
        }
        Patch_PickMaterial.acquiredDigimonCards.Clear();
        messageWindow.SetMessage(message, uCommonMessageWindow.Pos.Center);
    }
}