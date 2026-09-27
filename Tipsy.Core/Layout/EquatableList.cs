using System.Collections;
using System.Runtime.CompilerServices;

namespace Tipsy.Core.Layout;

/// <summary>A read-only list compared by its items in order, so records that hold one keep value equality.</summary>
[CollectionBuilder(typeof(EquatableList), nameof(EquatableList.Create))]
public sealed class EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
{
    private readonly T[] items;

    internal EquatableList(T[] items)
    {
        this.items = items;
    }

    public int Count => items.Length;

    public T this[int index] => items[index];

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(EquatableList<T>? other) =>
        other is not null && items.AsSpan().SequenceEqual(other.items, EqualityComparer<T>.Default);

    public override bool Equals(object? obj) => Equals(obj as EquatableList<T>);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}

public static class EquatableList
{
    public static EquatableList<T> Create<T>(ReadOnlySpan<T> items) => new(items.ToArray());
}
