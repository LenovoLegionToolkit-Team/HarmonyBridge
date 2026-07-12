using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Station.Core;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugin.HarmonyBridge;

public class HarmonyBridgeProvider : IExtensionProvider
{
    public void Initialize(IExtensionContext context)
    {
        HarmonyBridge.Initialize();
        Log.Instance.Trace($"[HarmonyBridge] Initialized.");
    }

    public Task ExecuteAsync(string action, params object[] args) => Task.CompletedTask;

    public object? GetData(string key) => key switch
    {
        nameof(ExtensionDataKey.Capability) => "HarmonyBridge",
        nameof(ExtensionDataKey.Version) => "1.0.0",
        _ => null
    };

    public void SetData(string key, object? value) { }

    public async ValueTask DisposeAsync()
    {
        HarmonyBridge.UnpatchAll();
        await ValueTask.CompletedTask;
    }
}
