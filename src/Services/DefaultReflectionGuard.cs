using FlowSynx.PluginCore.Helpers;

namespace FlowSynx.Plugins.Telegram.Services;

public class DefaultReflectionGuard : IReflectionGuard
{
    public bool IsCalledViaReflection() => ReflectionHelper.IsCalledViaReflection();
}