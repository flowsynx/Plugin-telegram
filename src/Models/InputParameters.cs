namespace FlowSynx.Plugins.Telegram.Models;

internal class InputParameters
{
    public string ChatId { get; set; } = string.Empty;
    public string? Message { get; set; }
}