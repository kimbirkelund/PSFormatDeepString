using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace PSFormatDeepString
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> sequence, params T[] items)
        {
            return sequence.Concat(items.AsEnumerable());
        }

        public static IEnumerable<KeyValuePair<TKey, TValue>> Concat<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> sequence, TKey key, TValue value)
        {
            return sequence.Concat(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Gets the input instance or a the empty read only collection if <c>null</c>.
        /// </summary>
        public static IReadOnlyList<T> Execute<T>(this IReadOnlyList<T> sequence)
        {
            return Execute((IEnumerable<T>)sequence);
        }

        /// <summary>
        /// Converts the input sequence to an read-only collection, if it isn't one already.
        /// Returns the empty read-only collection if <c>null</c>.
        /// </summary>
        public static IReadOnlyList<T> Execute<T>(this IEnumerable<T> sequence)
        {
            return (IReadOnlyList<T>)(sequence as IImmutableList<T>)
                   ?? sequence?.ToList()
                              .AsReadOnly()
                   ?? EmptyReadOnlyList<T>.Instance;
        }

        public static IList<TResult> FullOuterJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer,
                                                                                  IEnumerable<TInner> inner,
                                                                                  Func<TOuter, TKey> outerKeySelector,
                                                                                  Func<TInner, TKey> innerKeySelector,
                                                                                  Func<TOuter, TInner, TResult> resultSelector,
                                                                                  TOuter defaultOuter = default(TOuter),
                                                                                  TInner defaultInner = default(TInner),
                                                                                  IEqualityComparer<TKey> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<TKey>.Default;

            var left = outer.LeftJoin(inner, outerKeySelector, innerKeySelector, resultSelector, comparer)
                            .ToList();
            var right = outer.RightJoin(inner, outerKeySelector, innerKeySelector, resultSelector, comparer)
                             .ToList();

            return left.Union(right)
                       .ToList();
        }

        public static string Join<T>(this IEnumerable<T> sequece, string separator)
        {
            if (sequece == null)
                throw new ArgumentNullException(nameof(sequece));

            return string.Join(separator, sequece);
        }

        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer,
                                                                                   IEnumerable<TInner> inner,
                                                                                   Func<TOuter, TKey> outerKeySelector,
                                                                                   Func<TInner, TKey> innerKeySelector,
                                                                                   Func<TOuter, TInner, TResult> resultSelector,
                                                                                   IEqualityComparer<TKey> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<TKey>.Default;

            var results = outer.GroupJoin(inner,
                                          outerKeySelector,
                                          innerKeySelector,
                                          (oV, iVs) => new
                                                       {
                                                           oV,
                                                           iVs
                                                       },
                                          comparer)
                               .SelectMany(p => p.iVs.DefaultIfEmpty(),
                                           (p, left) => resultSelector(p.oV, left));

            return results;
        }

        public static IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer,
                                                                                    IEnumerable<TInner> inner,
                                                                                    Func<TOuter, TKey> outerKeySelector,
                                                                                    Func<TInner, TKey> innerKeySelector,
                                                                                    Func<TOuter, TInner, TResult> resultSelector,
                                                                                    IEqualityComparer<TKey> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<TKey>.Default;

            var results = inner.GroupJoin(outer,
                                          innerKeySelector,
                                          outerKeySelector,
                                          (iV, oVs) => new
                                                       {
                                                           iV,
                                                           oVs
                                                       },
                                          comparer)
                               .SelectMany(p => p.oVs.DefaultIfEmpty(),
                                           (p, right) => resultSelector(right, p.iV));

            return results;
        }

        public static IEnumerable<TResult> SelectIf<TInput, TResult>(this IEnumerable<TInput> sequence,
                                                                     Func<TInput, bool> predicate,
                                                                     Func<TInput, TResult> trueCase,
                                                                     Func<TInput, TResult> falseCase)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (trueCase == null)
                throw new ArgumentNullException(nameof(trueCase));
            if (falseCase == null)
                throw new ArgumentNullException(nameof(falseCase));

            foreach (var item in sequence)
            {
                if (predicate(item))
                    yield return trueCase(item);
                else
                    yield return falseCase(item);
            }
        }

        public static IEnumerable<T> SelectIf<T>(this IEnumerable<T> sequence,
                                                 Func<T, bool> predicate,
                                                 Func<T, T> trueCase,
                                                 Func<T, T> falseCase = null)
        {
            return sequence.SelectIf<T, T>(predicate, trueCase, falseCase ?? (i => i));
        }

        /// <summary>
        ///     Takes elements while predicate returns <c>true</c>. The predicate gets the first and the current element.
        /// </summary>
        public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> sequence, FirstAndCurrentPredicate<T> predicate)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            var enumerator = sequence.GetEnumerator();
            if (!enumerator.MoveNext())
                yield break;
            var firstElement = enumerator.Current;

            do
            {
                if (!predicate(firstElement, enumerator.Current))
                    yield break;

                yield return enumerator.Current;
            } while (enumerator.MoveNext());
        }

        public static dynamic ToDynamic(this IEnumerable<KeyValuePair<string, object>> sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            var eo = new ExpandoObject();
            var eoColl = (ICollection<KeyValuePair<string, object>>)eo;

            foreach (var kvp in sequence)
                eoColl.Add(kvp);

            return eo;
        }

        public static dynamic ToDynamic<TValue>(this IEnumerable<KeyValuePair<string, TValue>> sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            return sequence.Select(p => new KeyValuePair<string, object>(p.Key, p.Value))
                           .ToDynamic();
        }

        public static dynamic ToDynamic<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> sequence, Func<TKey, string> keySelector = null)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            keySelector = keySelector ?? (k => k + "");

            return sequence.Select(p => new KeyValuePair<string, object>(keySelector(p.Key), p.Value))
                           .ToDynamic();
        }

        // ReSharper disable once ConsiderUsingAsyncSuffix
        public static Task<IEnumerable<T>> WhenAll<T>(this IEnumerable<Task<T>> tasks)
        {
            // Using ContinueWith approach instead of simply awaiting
            // maintains the full exception.
            return Task.WhenAll(tasks)
                       .ContinueWith(t => t.Result.AsEnumerable());
        }

        // ReSharper disable once ConsiderUsingAsyncSuffix
        public static Task WhenAll(this IEnumerable<Task> tasks)
        {
            return Task.WhenAll(tasks);
        }

        public delegate bool FirstAndCurrentPredicate<in T>(T firstElement, T currentElement);

        internal static class EmptyReadOnlyList<T>
        {
            public static readonly ReadOnlyCollection<T> Instance = new List<T>().AsReadOnly();
        }
    }
}
