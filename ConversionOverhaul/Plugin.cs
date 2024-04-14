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

namespace ConversionOverhaul;

// TODO: Check ParameterCommonSelectWindowMode. There is a WindowType parameter that looks like a clue
// * MaterialChange#
// * LaboratoryItemChange##


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
        
        // Plugin.oneHitNode = base.Config.Bind<bool>("One Hit Resource Node", "enable", true, "Whether the mod should gather all materials from a resource node at once.");
        // Plugin.resourceMultiplier = base.Config.Bind<float>("Resource Multiplier", "multiplier", 1, "Multiplies the number of materials pulled from resource nodes.");
        // Plugin.resourceRarity = base.Config.Bind<float>("Resource Rarity Multiplier", "multiplier", 1, "Multiplies the chance of pulling rare materials from resource nodes.");
        // Plugin.cardProbability = base.Config.Bind<int>("Card Drop Chance", "probability", 10, "Percent chance of a card dropping from resource node per material pull.");
        // Plugin.breakNodeFullInventory = base.Config.Bind<bool>("Break Material Node Toggle", "breakNodeFullInventory", false, "Run card chance and break material node even when inventory is full.");

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
