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

namespace DescriptiveResources;

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
        Harmony harmony = new Harmony("DescriptiveResources");
        var _original = AccessTools.Method(typeof(ItemPickPointTimeRebirth), "PickItem");
        original = harmony.Patch(_original);
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ItemPickPointTimeRebirth), "PickItem")]
public static class Patch
{
    [HarmonyPrefix]
    public static bool GrabReference(ItemPickPointTimeRebirth __instance, int requestPickCount, float rarityRevision) {
        return false;
    }

    [HarmonyPostfix]
    public static void Repeat(int requestPickCount, float rarityRevision, ItemPickPointTimeRebirth __instance, ref dynamic __result) {
        var pulls = __instance.remainderPickCount;
        List<UInt32> materials = new List<UInt32>();
        for (int i = 0; i < pulls; i++) {
            dynamic part = Plugin.original.Invoke(__instance, new object[] { __instance, requestPickCount, rarityRevision });
            foreach (var item in part) {
                materials.Add(item);
            }
        }
        __result = new Il2CppStructArray<UInt32>(materials.ToArray());
    }
}
