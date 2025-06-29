namespace FlowSynx.Plugins.Telegram.Models;

internal class InputParameters
{
    public string Operation {  get; set; } = "sendmessage";
    public string ChatId { get; set; } = string.Empty;
    public object? Data { get; set; }
}