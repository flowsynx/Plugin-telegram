using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Telegram.Models;
using System.Text;
using System.Text.Json;
using FlowSynx.Plugins.Telegram.Services;

namespace FlowSynx.Plugins.Telegram;

public class TelegramPlugin : IPlugin
{
    private IPluginLogger? _logger;
    private TelegramPluginSpecifications _telegramSpecifications = null!;
    private bool _isInitialized;

    public HttpClient HttpClient { get; set; } = new HttpClient();
    public IReflectionGuard ReflectionGuard { get; set; } = new DefaultReflectionGuard();

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("4e9e2b55-935b-4c1c-9d0d-360aeaebf68a"),
                Name = "Telegram",
                CompanyName = "FlowSynx",
                Description = Resources.PluginDescription,
                Version = new PluginVersion(1, 0, 0),
                Category = PluginCategory.Communication,
                Authors = new List<string> { "FlowSynx" },
                Copyright = "© FlowSynx. All rights reserved.",
                Icon = "flowsynx.png",
                ReadMe = "README.md",
                RepositoryUrl = "https://github.com/flowsynx/Plugin-telegram",
                ProjectUrl = "https://flowsynx.io",
                Tags = new List<string>() { "flowSynx", "telegram", "communication", "collaboration" },
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }

    public Type SpecificationsType => typeof(TelegramPluginSpecifications);

    public IReadOnlyCollection<string> SupportedOperations => new List<string>();

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
        EnsureNotCalledViaReflection();
        EnsureInitialized();

        var input = parameters.ToObject<InputParameters>();
        ValidateInput(input);

        _logger?.LogInfo($"Sending message to Telegram: '{input.Message}'");

        var url = $"https://api.telegram.org/bot{_telegramSpecifications.Token}/sendMessage";
        var payload = new
        {
            chat_id = input.ChatId,
            text = input.Message
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error: {response.StatusCode} - {errorMessage}");
        }

        _logger?.LogInfo("Telegram message sent");
        return Task.CompletedTask;
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

    private static void ValidateInput(InputParameters input)
    {
        if (string.IsNullOrWhiteSpace(input.ChatId))
            throw new ArgumentException("Missing or invalid 'chatid' input.");

        if (string.IsNullOrWhiteSpace(input.Message))
            throw new ArgumentException("Missing or invalid 'message' input.");
    }
}