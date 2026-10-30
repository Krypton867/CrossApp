using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class CompositeNotificationSink : INotificationSink
{
    private readonly IReadOnlyList<INotificationSink> _sinks;

    public CompositeNotificationSink(
        params INotificationSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);

        _sinks = sinks;
    }

    public void Notify(CatalogChangedEventArgs notification)
    {
        foreach (INotificationSink sink in _sinks)
        {
            sink.Notify(notification);
        }
    }
}