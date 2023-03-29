using System;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace StackingResourceStorage;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Digimon World Next Order.exe")]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public void Awake()
    {
        Harmony harmony = new Harmony("StackingResourceStorage");
        harmony.PatchAll();
    }
}

// TODO: See if you can call PickMaterial in a for loop via the patch and return false 
// to stop the original from being called
// TODO: See if PickMaterial patch can get a self reference to "this.m_targetItemPickPoint"
// This could gice you the remainder attempts on the resource node effectively allowing the process to 
// be put in a for loop
[HarmonyPatch(typeof(ItemStorageData), nameof(ItemStorageData.MAX_MATERIAL_ITEM_NUM))]
public static class Patch
{
    [HarmonyPrefix]
    public static void Postfix(ref int __result)
    {
        __result = 999;
    }
}