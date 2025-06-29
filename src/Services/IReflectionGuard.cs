namespace FlowSynx.Plugins.Telegram.Services;

public interface IReflectionGuard
{
    bool IsCalledViaReflection();
}