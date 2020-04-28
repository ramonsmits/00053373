using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class UpdateOrderHandler : IHandleMessages<MyMessage>
{
    static readonly int ProcessId = Process.GetCurrentProcess().Id;
    readonly ILog _logger = LogManager.GetLogger<UpdateOrderHandler>();

    public Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        // Added read of header info for display in Graylog
        var numberDelayedRetries = string.Empty;
        var messageId = string.Empty;
        context?.MessageHeaders.TryGetValue(Headers.DelayedRetries, out numberDelayedRetries);
        context?.MessageHeaders.TryGetValue(Headers.MessageId, out messageId);
        _logger.Warn($"Process '{ProcessId}', Machine '{Environment.MachineName}', delayed retry count '{numberDelayedRetries ?? "0"}', message id '{messageId ?? string.Empty}");
        throw new Exception("I intentionally fail all attempts");
    }
}