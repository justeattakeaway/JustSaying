using System;

namespace JustSaying.Messaging.Policies
{
    public static class SqsPolicyBuilder
    {
        public static SqsPolicyAsync<T> BuildAsync<T>(params Func<SqsPolicyAsync<T>, SqsPolicyAsync<T>>[] policies)
        {
            SqsPolicyAsync<T> policy = new InnerSqsPolicyAsync<T>();
            return policy.WithAsync(policies);
        }

        public static SqsPolicyAsync<T> WithAsync<T>(this SqsPolicyAsync<T> inner, params Func<SqsPolicyAsync<T>, SqsPolicyAsync<T>>[] policies)
        {
            var policy = inner;

            foreach (var p in policies)
            {
                policy = p(policy);
            }

            return policy;
        }
    }
}
