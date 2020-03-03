using System;
using System.Collections.Generic;
using System.Text;

namespace JustSaying.Messaging.Channels
{
    public abstract class SqsPolicy<T>
    {
        private readonly SqsPolicy<T> _next;

        public SqsPolicy(SqsPolicy<T> next)
        {
            _next = next;
        }

        public T Run(Func<T> func)
        {
            return RunInner(() =>
            {
                if (_next == null)
                    return func();

                return _next.Run(func);
            });
        }

        protected abstract T RunInner(Func<T> func);
    }

    public class InnerSqsPolicy<T> : SqsPolicy<T>
    {
        public InnerSqsPolicy() : base(null)
        {

        }

        protected override T RunInner(Func<T> func)
        {
            return func();
        }
    }

    public class ErrorHandlingSqsPolicy<T> : SqsPolicy<T>
    {
        public ErrorHandlingSqsPolicy(SqsPolicy<T> next) : base(next)
        {

        }

        protected override T RunInner(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (InvalidOperationException)
            {
                return default(T);
            }
        }
    }

    public static class SqsPolicyBuilder
    {
        public static SqsPolicy<T> Build<T>(params Func<SqsPolicy<T>, SqsPolicy<T>>[] policies)
        {
            SqsPolicy<T> policy = new InnerSqsPolicy<T>();

            foreach(var p in policies)
            {
                policy = p(policy);
            }

            return policy;
        }
    }
}
