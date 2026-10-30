using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingCatalogStore : ICatalogStore
{
	private readonly ICatalogStore _inner;
	private readonly Dictionary<string, Product?> _cache =
		new(StringComparer.OrdinalIgnoreCase);

	private List<Product>? _listCache;

	public CachingCatalogStore(ICatalogStore inner)
	{
		_inner = inner
			?? throw new ArgumentNullException(nameof(inner));
	}

	public IReadOnlyList<Product> List()
	{
		if (_listCache is null)
			_listCache = _inner.List().ToList();

		return _listCache.ToList();
	}

	public Product? GetById(string id)
	{
		if (_cache.TryGetValue(id, out Product? product))
			return product;

		product = _inner.GetById(id);
		_cache[id] = product;

		return product;
	}

	public void Add(Product item)
	{
		_inner.Add(item);
		Invalidate();
	}

	public void Update(Product item)
	{
		_inner.Update(item);
		Invalidate();
	}

	public bool Remove(string id)
	{
		bool removed = _inner.Remove(id);

		if (removed)
			Invalidate();

		return removed;
	}

	private void Invalidate()
	{
		_cache.Clear();
		_listCache = null;
	}
}