using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Telegram.Models;

public class TelegramPluginSpecifications: PluginSpecifications
{
    [RequiredMember]
    public string Token { get; set; } = string.Empty;
}