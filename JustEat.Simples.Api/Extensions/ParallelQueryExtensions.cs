using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JustEat.Simples.Api.Extensions
{
    public static class ParallelQueryExtensions
    {
        public static ParallelQuery<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer,
            ParallelQuery<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            TInner defaultInner)
        {


            return outer
                .GroupJoin(
                    inner,
                    outerKeySelector,
                    innerKeySelector,
                    Tuple.Create)
                .SelectMany(
                    z => z.Item2.DefaultIfEmpty(defaultInner),
                    (a, b) => resultSelector(a.Item1, b));
        }

        public static ParallelQuery<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ParallelQuery<TOuter> outer,
            ParallelQuery<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {


            return outer
                .GroupJoin(
                    inner,
                    outerKeySelector,
                    innerKeySelector,
                    Tuple.Create)
                .SelectMany(
                    z => z.Item2.DefaultIfEmpty(),
                    (a, b) => resultSelector(a.Item1, b));
        }
    }
}
