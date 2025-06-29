using FlowSynx.PluginCore;
using FlowSynx.Plugins.Telegram.Models;
using FlowSynx.Plugins.Telegram.Services;
using Moq;
using Moq.Protected;
using System.Net;

namespace FlowSynx.Plugins.Telegram.UnitTests;

public class TelegramPluginTests
{
    [Fact]
    public async Task ExecuteAsync_SendsTelegramMessage_WhenValid()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Arrange
        var plugin = new TelegramPlugin
        {
            HttpClient = new HttpClient(handler.Object),
            ReflectionGuard = Mock.Of<IReflectionGuard>(x => x.IsCalledViaReflection() == false),
            Specifications = new TelegramPluginSpecifications { Token = "123" }
        };

        var loggerMock = new Mock<IPluginLogger>();
        await plugin.Initialize(loggerMock.Object);

        var parameters = new PluginParameters
        {
            {"ChatId", "1234"},
            { "Message", "Hello"}
        };

        // Act
        await plugin.ExecuteAsync(parameters, CancellationToken.None);

        // Assert
        loggerMock.Verify(x => x.Log(PluginLoggerLevel.Information, It.Is<string>(msg => msg.Contains("Telegram message sent"))), Times.Once);
    }
}