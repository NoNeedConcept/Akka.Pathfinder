﻿using System.Collections.Concurrent;

namespace Akka.Pathfinder.Core;

public static class ConcurrentDictionaryExtensions
{
    public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer) where TKey : notnull
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

        ConcurrentDictionary<TKey, TElement> d = new(comparer ?? EqualityComparer<TKey>.Default);
        foreach (TSource element in source)
            d.TryAdd(keySelector(element), elementSelector(element));

        return d;
    }

    public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull => ToConcurrentDictionary(source, keySelector, IdentityFunction<TSource>.Instance, null!);

    public static ConcurrentDictionary<TKey, TSource> ToConcurrentDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer) where TKey : notnull => ToConcurrentDictionary(source, keySelector, IdentityFunction<TSource>.Instance, comparer);

    public static ConcurrentDictionary<TKey, TElement> ToConcurrentDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull => ToConcurrentDictionary(source, keySelector, elementSelector, null!);

    internal class IdentityFunction<TElement>
    {
        public static Func<TElement, TElement> Instance
        {
            get { return x => x; }
        }
    }

}