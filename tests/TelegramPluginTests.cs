using FlowSynx.PluginCore;
using FlowSynx.Plugins.Telegram.Models;
using FlowSynx.Plugins.Telegram.Services;
using Moq;
using Moq.Protected;
using System.Buffers.Text;
using System.Net;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlowSynx.Plugins.Telegram.UnitTests;

public class TelegramPluginTests
{
    private readonly Mock<IPluginLogger> _loggerMock = new();
    private readonly Mock<IReflectionGuard> _guardMock = new();
    private readonly TelegramPlugin _plugin;

    public TelegramPluginTests()
    {
        _plugin = new TelegramPlugin
        {
            ReflectionGuard = _guardMock.Object,
            HttpClient = CreateMockHttpClient(),
        };

        _plugin.Specifications = new TelegramPluginSpecifications
        {
            Token = "TEST_TOKEN"
        };
    }

    private static HttpClient CreateMockHttpClient()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"ok\": true }", Encoding.UTF8, "application/json")
            });

        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task ExecuteSendMessageAsync_ShouldSendTextMessage()
    {
        await _plugin.Initialize(_loggerMock.Object);

        var input = new PluginParameters
        {
            { "Operation", "sendmessage" },
            { "ChatId", "12345" },
            { "Data", new PluginContext("123", "message") { Content = "Hello, World!" } },
        };

        await _plugin.ExecuteAsync(input, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteSendFileAsync_ShouldSendPngFile()
    {
        await _plugin.Initialize(_loggerMock.Object);

        var data = Encoding.UTF8.GetBytes("fakeimagecontent");
        var input = new PluginParameters
        {
            { "Operation", "sendfile" },
            { "ChatId", "12345" },
            { "Data", new PluginContext("image.png", "file") { RawData = data } },
        };

        await _plugin.ExecuteAsync(input, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteSendFileAsync_ShouldSendMp3File()
    {
        await _plugin.Initialize(_loggerMock.Object);

        var data = Encoding.UTF8.GetBytes("audio");
        var input = new PluginParameters
        {
            { "Operation", "sendfile" },
            { "ChatId", "12345" },
            { "Data", new PluginContext("track.mp3", "audio") { RawData = data } },
        };

        await _plugin.ExecuteAsync(input, CancellationToken.None);
    }

    [Theory]
    [InlineData("sendmessage")]
    [InlineData("sendfile")]
    public async Task ExecuteAsync_InvalidChatId_ShouldThrow(string operation)
    {
        await _plugin.Initialize(_loggerMock.Object);
        var input = new PluginParameters
        {
            { "Operation", operation },
            { "ChatId", null },
            { "Data", new PluginContext("x", "x") { Content = "Test" } },
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _plugin.ExecuteAsync(input, CancellationToken.None));

        Assert.Contains("chatId", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedOperation_ShouldThrow()
    {
        await _plugin.Initialize(_loggerMock.Object);
        var input = new PluginParameters
        {
            { "Operation", "something-else" },
            { "ChatId", "123" },
            { "Data", new PluginContext("123", "msg") { Content = "Hey" } },
        };

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            _plugin.ExecuteAsync(input, CancellationToken.None));

        Assert.Contains("Unsupported operation", ex.Message);
    }

    [Fact]
    public async Task ExecuteSendMessageAsync_MissingMessage_ShouldThrow()
    {
        await _plugin.Initialize(_loggerMock.Object);

        var context = new PluginContext("123", "message");
        var input = new PluginParameters
        {
            { "Operation", "sendmessage" },
            { "ChatId", "1" },
            { "Data", context },
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _plugin.ExecuteAsync(input, CancellationToken.None));
    }
}