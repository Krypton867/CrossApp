
using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _path = path;

        string? directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public void Notify(CatalogChangedEventArgs notification)
    {
        string line = ConsoleSink.Format(notification);

        File.AppendAllText(
            _path,
            line + Environment.NewLine);
    }
}