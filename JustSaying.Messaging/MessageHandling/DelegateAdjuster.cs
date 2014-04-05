using System;
using System.Linq.Expressions;

namespace JustSaying.Messaging.MessageHandling
{
    public class DelegateAdjuster
    {
        public static Func<TBase, bool> CastArgument<TBase, TDerived>(Expression<Func<TDerived, bool>> source) where TDerived : TBase
        {
            if (typeof(TDerived) == typeof(TBase))
            {
                return (Func<TBase, bool>)((Delegate)source.Compile());
            }
            
            var sourceParameter = Expression.Parameter(typeof(TBase), "source");

            var result = Expression.Lambda<Func<TBase, bool>>(
                Expression.Invoke(
                    source, 
                    Expression.Convert(sourceParameter, typeof(TDerived))
                ),
                sourceParameter);

            return result.Compile();
        }
    }
}
