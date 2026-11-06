namespace Core.Reports;

public static class EnumerableExtensions
{
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, int> selector,
        int count,
        Func<T, string>? secondarySelector = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (secondarySelector is null)
        {
            return source
                .OrderByDescending(selector)
                .Take(count);
        }

        return source
            .OrderByDescending(selector)
            .ThenBy(secondarySelector, StringComparer.Ordinal)
            .Take(count);
    }
}