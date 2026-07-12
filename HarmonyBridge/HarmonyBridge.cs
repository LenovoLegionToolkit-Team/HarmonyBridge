using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugin.HarmonyBridge;

public static class HarmonyBridge
{
    private static readonly Dictionary<string, object> Instances = [];
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        using var stream = typeof(HarmonyBridge).Assembly.GetManifestResourceStream("0Harmony.dll");
        if (stream is not null)
        {
            var buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);
            Assembly.Load(buffer);
            Log.Instance.Trace($"[HarmonyBridge] Pre-loaded 0Harmony from embedded resource ({buffer.Length} bytes).");
        }
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        if (new AssemblyName(args.Name).Name != "0Harmony")
        {
            return null;
        }

        using var stream = typeof(HarmonyBridge).Assembly.GetManifestResourceStream("0Harmony.dll");
        if (stream is null)
        {
            return null;
        }

        var buffer = new byte[stream.Length];
        stream.ReadExactly(buffer);

        Log.Instance.Trace($"[HarmonyBridge] Loaded 0Harmony from embedded resource ({buffer.Length} bytes).");

        return Assembly.Load(buffer);
    }

    public static void Patch(string ownerId, Type targetType, Type processorType)
    {
        if (!Instances.TryGetValue(ownerId, out var obj))
        {
            var harmonyType = typeof(HarmonyBridge).Assembly.GetType("HarmonyLib.Harmony")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "0Harmony")
                    ?.GetType("HarmonyLib.Harmony");

            if (harmonyType is null)
            {
                Log.Instance.Trace($"[HarmonyBridge] HarmonyLib.Harmony type not found.");
                return;
            }

            obj = Activator.CreateInstance(harmonyType, $"com.llt.harmony.{ownerId}");
            Instances[ownerId] = obj!;
        }

        var processorClassType = typeof(HarmonyBridge).Assembly.GetType("HarmonyLib.PatchClassProcessor")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "0Harmony")
                ?.GetType("HarmonyLib.PatchClassProcessor");

        if (processorClassType is null)
        {
            Log.Instance.Trace($"[HarmonyBridge] HarmonyLib.PatchClassProcessor type not found.");
            return;
        }

        var processor = Activator.CreateInstance(processorClassType, obj, processorType);
        var patchMethod = processorClassType.GetMethod("Patch");
        patchMethod!.Invoke(processor, null);

        Log.Instance.Trace($"[HarmonyBridge] Owner '{ownerId}': patched {targetType.FullName} with {processorType.FullName}.");
    }

    public static void Unpatch(string ownerId)
    {
        if (Instances.Remove(ownerId, out var obj))
        {
            var unpatchMethod = obj.GetType().GetMethod("UnpatchAll");
            unpatchMethod!.Invoke(obj, [obj.GetType().GetProperty("Id")!.GetValue(obj)]);
            Log.Instance.Trace($"[HarmonyBridge] Owner '{ownerId}': unpatched.");
        }
    }

    public static void UnpatchAll()
    {
        foreach (var (ownerId, obj) in Instances)
        {
            var unpatchMethod = obj.GetType().GetMethod("UnpatchAll");
            unpatchMethod!.Invoke(obj, [obj.GetType().GetProperty("Id")!.GetValue(obj)]);
            Log.Instance.Trace($"[HarmonyBridge] Owner '{ownerId}': unpatched.");
        }

        Instances.Clear();
    }
}