using System;
using System.Globalization;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class UpdateOrderHandler : IHandleMessages<MyMessage>
{
    private readonly ILog _logger = LogManager.GetLogger<UpdateOrderHandler>();

    public Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        // Added read of header info for display in Graylog
        var numberDelayedRetries = string.Empty;
        var messageId = string.Empty;
        context?.MessageHeaders.TryGetValue(Headers.DelayedRetries, out numberDelayedRetries);
        context?.MessageHeaders.TryGetValue(Headers.MessageId, out messageId);
        return UpdateOrderAsync(message, $"delayed retry count '{numberDelayedRetries ?? "0"}', message id '{messageId ?? string.Empty}, Timestamp: {DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}'");
    }

    private Task UpdateOrderAsync(MyMessage message, string nsbMessageInfo)
    {
        try
        {
            throw new Exception("I intentionally fail all attempts");
        }
        catch
        {
            _logger.Warn(nsbMessageInfo);
            throw;
        }
    }
}