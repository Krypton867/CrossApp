using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs notification)
    {
        Console.WriteLine(Format(notification));
    }

    internal static string Format(
        CatalogChangedEventArgs notification)
    {
        return $"{notification.At:HH:mm:ss} " +
               $"{notification.Kind,-8} " +
               $"{notification.Id} " +
               $"{notification.Delta,4:+0;-0;0} " +
               $"→ залишок {notification.QuantityAfter}";
    }
}