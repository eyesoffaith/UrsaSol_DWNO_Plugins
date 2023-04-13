using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
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
    public static MethodInfo unpatched_PickItem;
    
    public static ConfigEntry<float> resourceRarity;
    public static ConfigEntry<float> resourceMultiplier;
    public static ConfigEntry<int> cardProbability;
    public static ConfigEntry<bool> breakNodeFullInventory;
    public static ConfigEntry<bool> oneHitNode;

    public override void Load()
    {
        Plugin.Logger = base.Log;
        HarmonyFileLog.Enabled = true;
        
        Plugin.oneHitNode = base.Config.Bind<bool>("One Hit Resource Node", "enable", true, "Whether the mod should gather all materials from a resource node at once.");
        Plugin.resourceMultiplier = base.Config.Bind<float>("Resource Multiplier", "multiplier", 1, "Multiplies the number of materials pulled from resource nodes.");
        Plugin.resourceRarity = base.Config.Bind<float>("Resource Rarity Multiplier", "multiplier", 1, "Multiplies the chance of pulling rare materials from resource nodes.");
        Plugin.cardProbability = base.Config.Bind<int>("Card Drop Chance", "probability", 10, "Percent chance of a card dropping from resource node per material pull.");
        Plugin.breakNodeFullInventory = base.Config.Bind<bool>("Break Material Node Toggle", "breakNodeFullInventory", false, "Run card chance and break material node even when inventory is full.");

        // Plugin startup logic
        Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        this.Awake();
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("OneHitResource");
        var _original = AccessTools.Method(typeof(ItemPickPointTimeRebirth), "PickItem");
        unpatched_PickItem = harmony.Patch(_original);
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ItemPickPointTimeRebirth), "PickItem")]
public static class Patch_PickItem
{
    public static bool Prefix() {
        return !Plugin.oneHitNode.Value;
    }

    public static void Postfix(int requestPickCount, float rarityRevision, ref dynamic __result, ItemPickPointTimeRebirth __instance) {
        if (Plugin.oneHitNode.Value) {
            List<UInt32> materials = new List<UInt32>();
            int remainderPickCount = __instance.remainderPickCount;
            for (int i = 0; i < remainderPickCount; i++) {
                dynamic part = Plugin.unpatched_PickItem.Invoke(__instance, new object[] { __instance, (int)(Plugin.resourceMultiplier.Value * requestPickCount), (int)(Plugin.resourceRarity.Value * rarityRevision) });
                foreach (var item in part) {
                    materials.Add(item);
                }
            }
            __result = new Il2CppStructArray<UInt32>(materials.ToArray());
        }
    }
}

// Custom card acquisition
[HarmonyPatch(typeof(uItemPickPanel), "PickMaterial")]
public static class Patch_PickMaterial
{
    public static List<int> acquiredDigimonCards = new List<int>();
    public static bool inventoryFull = false;
    public static bool Prefix()
    {
        dynamic itemStorageData = Traverse.Create(new StorageData()).Property("m_ItemStorageData").GetValue();
        Patch_PickMaterial.inventoryFull = itemStorageData.GetEmptyNum(ItemStorageData.StorageType.MATERIAL) <= 0;

        return true;
    }
    public static void Postfix(uItemPickPanel __instance)
    {
        ItemPickPointTimeRebirth materialPickPoint = (ItemPickPointTimeRebirth)ItemPickPointManager.Ref.GetMaterialPickPoint(ItemPickPointManager.Ref.PickingPoint.id);
        int remainderPickCount = Plugin.oneHitNode.Value ? materialPickPoint.remainderPickCount : 1;

        if (Plugin.breakNodeFullInventory.Value | !Patch_PickMaterial.inventoryFull) {
            dynamic params2 = Traverse.Create(AppMainScript.parameterManager.digimonCardData).Property("m_params").GetValue();
            List<int> list = new List<int>();
            foreach (ParameterDigimonCardData parameterDigimonCardData in params2)
            {
                int cardNum = (int)Traverse.Create(parameterDigimonCardData).Property("m_number").GetValue();
                if (!StorageData.m_digimonCardFlag.IsGetFlag((uint)cardNum))
                {
                    list.Add(cardNum);
                }
            }

            if (!StorageData.m_digimonCardFlag.IsAllGetFlag()) {
                for (int i = 0; i < remainderPickCount; i++) {
                    if (UnityEngine.Random.Range(0, 100) <= Plugin.cardProbability.Value) {
                        if (list.Count > 0)
                        {
                            int index = UnityEngine.Random.Range(0, list.Count);
                            int cardNum = list[index];
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
            acquiredDigimonCards.Sort();
        }

        if (Plugin.breakNodeFullInventory.Value) {
            for (int i = 0; i < remainderPickCount; i++) {
                Plugin.unpatched_PickItem.Invoke(materialPickPoint, new object[] { materialPickPoint, 0, 0 });
            }
        }
    }
}

// Check message to see if card was added by vanilla code, remove if present and undo card acquisition
// Write custom message for our cards
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