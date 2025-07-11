using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Telegram.Models;
using System.Text;
using System.Text.Json;
using FlowSynx.Plugins.Telegram.Services;
using FlowSynx.Plugins.Telegram.Extensions;

namespace FlowSynx.Plugins.Telegram;

public class TelegramPlugin : IPlugin
{
    private IPluginLogger? _logger;
    private TelegramPluginSpecifications _telegramSpecifications = null!;
    private bool _isInitialized;

    public HttpClient HttpClient { get; set; } = new HttpClient();
    public IReflectionGuard ReflectionGuard { get; set; } = new DefaultReflectionGuard();

    public PluginMetadata Metadata => new PluginMetadata
    {
        Id = Guid.Parse("4e9e2b55-935b-4c1c-9d0d-360aeaebf68a"),
        Name = "Telegram",
        CompanyName = "FlowSynx",
        Description = Resources.PluginDescription,
        Version = new Version(1, 1, 1),
        Category = PluginCategory.Communication,
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Icon = "flowsynx.png",
        ReadMe = "README.md",
        RepositoryUrl = "https://github.com/flowsynx/Plugin-telegram",
        ProjectUrl = "https://flowsynx.io",
        Tags = new List<string>() { "flowSynx", "telegram", "communication", "collaboration" },
        MinimumFlowSynxVersion = new Version(1, 1, 1)
    };

    public PluginSpecifications? Specifications { get; set; }

    public Type SpecificationsType => typeof(TelegramPluginSpecifications);

    public IReadOnlyCollection<string> SupportedOperations => new[] { "sendmessage", "sendfile" };

    public Task Initialize(IPluginLogger logger)
    {
        EnsureNotCalledViaReflection();

        ArgumentNullException.ThrowIfNull(logger);
        _telegramSpecifications = Specifications.ToObject<TelegramPluginSpecifications>();
        _logger = logger;
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureNotCalledViaReflection();
        EnsureInitialized();

        var input = parameters.ToObject<InputParameters>();
        ValidateChatId(input.ChatId);

        return input.Operation.ToLowerInvariant() switch
        {
            "sendmessage" => await ExecuteSendMessageAsync(input, cancellationToken),
            "sendfile" => await ExecuteSendFileAsync(input, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported operation: {input.Operation}")
        };
    }

    private async Task<object?> ExecuteSendMessageAsync(InputParameters input, CancellationToken cancellationToken)
    {
        var contexts = ParseDataToContexts(input.Data, isFile: false);

        foreach (var context in contexts)
        {
            var message = context.Content;
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Invalid or empty message content.");

            var payload = new
            {
                chat_id = input.ChatId,
                text = message,
                parse_mode = "Markdown"
            };

            var url = BuildTelegramApiUrl("sendMessage");
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync(url, content, cancellationToken);
            await LogAndThrowIfFailed(response, "sendMessage", cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task<object?> ExecuteSendFileAsync(InputParameters input, CancellationToken cancellationToken)
    {
        var contexts = ParseDataToContexts(input.Data, isFile: true);

        foreach (var context in contexts)
        {
            var data = context.RawData ?? Encoding.UTF8.GetBytes(context.Content ?? string.Empty);
            await SendFileAsync(input.ChatId, context.Id, data, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private List<PluginContext> ParseDataToContexts(object? data, bool isFile)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data), "Input data cannot be null.");

        var contexts = new List<PluginContext>();

        switch (data)
        {
            case PluginContext singleContext:
                contexts.Add(singleContext);
                break;

            case IEnumerable<PluginContext> contextList:
                contexts.AddRange(contextList);
                break;

            case string strData:
                if (isFile)
                {
                    var bytes = strData.IsBase64String()
                        ? strData.Base64ToByteArray()
                        : Encoding.UTF8.GetBytes(strData);

                    contexts.Add(new PluginContext(Guid.NewGuid().ToString(), "Data") { RawData = bytes });
                }
                else
                {
                    contexts.Add(new PluginContext(Guid.NewGuid().ToString(), "Data") { Content = strData });
                }
                break;

            default:
                throw new NotSupportedException("Unsupported input data format for Telegram plugin.");
        }

        return contexts;
    }

    private async Task<string> SendFileAsync(string chatId, string fileName, byte[] data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (apiMethod, fieldName) = GetTelegramFileEndpoint(fileName);
        var url = BuildTelegramApiUrl(apiMethod);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(chatId), "chat_id" },
            { new ByteArrayContent(data), fieldName, fileName }
        };

        var response = await HttpClient.PostAsync(url, form, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger?.LogInfo($"{apiMethod} response: {result}");

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Telegram API error: {response.StatusCode} - {result}");

        return result;
    }

    private static (string Method, string Field) GetTelegramFileEndpoint(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".bmp" => ("sendPhoto", "photo"),
            ".mp4" => ("sendVideo", "video"),
            ".mp3" or ".ogg" => ("sendAudio", "audio"),
            _ => ("sendDocument", "document")
        };
    }

    private string BuildTelegramApiUrl(string method) =>
        $"https://api.telegram.org/bot{_telegramSpecifications.Token}/{method}";

    private async Task LogAndThrowIfFailed(HttpResponseMessage response, string method, CancellationToken cancellationToken)
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger?.LogInfo($"{method} response: {responseContent}");

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Telegram API error: {response.StatusCode} - {responseContent}");
    }

    private void EnsureNotCalledViaReflection()
    {
        if (ReflectionGuard.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");
    }

    private static void ValidateChatId(string? chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("Missing or invalid 'chatId' input.");
    }
}